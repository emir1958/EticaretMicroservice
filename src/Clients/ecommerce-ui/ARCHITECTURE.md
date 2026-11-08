# ecommerce-ui Mimarisi

React + Vite ile yazılmış müşteri ve yönetim arayüzü. Tüm istekler API Gateway'e (YARP) gider.

## Klasör yapısı

```
src/
  main.jsx              Giriş noktası: global CSS ve <App />
  app/                  Uygulamanın iskeleti
    App.jsx             Global provider'lar (Router, Auth, Cart)
    AppRoutes.jsx       Tüm adresler tek yerde
    layouts/            Sayfaları saran iskeletler: CustomerLayout (header, footer, sepet), AdminLayout (kenar çubuğu)
  config/env.js         Ortam ayarları (VITE_API_URL). import.meta.env yalnızca burada okunur
  lib/apiClient.js      Tek HTTP istemcisi (axios + interceptor'lar) ve getErrorMessage
  shared/               Hiçbir feature'a ait olmayan, herkesin kullandığı parçalar
    components/         Button, FormField, Alert, EmptyState (iş mantığı içermez)
    hooks/              useAsync (veri çekme kalıbı)
    utils/              formatPrice, formatDate ...
  styles/               tokens.css (tasarım token'ları), global.css (temel stiller)
  features/             İş alanlarına göre bölünmüş kod
    auth/               Giriş, kayıt, token, korumalı route'lar
    cart/               Sepet (misafir + Redis), sepet çekmecesi
    catalog/            Ürün listesi ve detay, stok birleştirme
    orders/             Ödeme, siparişlerim, canlı sipariş durumu (SignalR)
    admin/              Yönetim paneli sayfaları (+ admin.module.css: admin sayfalarının ortak başlık/panel/tablo stilleri)
```

Bir feature klasörünün içi:

```
features/<ad>/
  api.js                Backend ile konuşan tek yer
  <Ad>Context.jsx       (gerekirse) feature'ın global state'i ve use<Ad>() hook'u
  components/           Bu feature'a ait bileşenler
  pages/                Route'a bağlanan sayfalar (<Ad>Page.jsx + <Ad>Page.module.css)
```

## Kurallar

### 1. Bileşenler HTTP isteği yapmaz; `api.js` yapar
Sayfa `getProductsWithStock()` çağırır; URL'yi, cevabın şeklini veya stokun başka servisten geldiğini bilmez.
Backend sözleşmesi değişirse yalnızca ilgili `api.js` değişir.

```
Sayfa ──> features/catalog/api.js ──> lib/apiClient.js ──> Gateway ──> Catalog.Api / Stock.Api
```

- `apiClient`'ı yalnızca `api.js` dosyaları import eder.
- `api.js` fonksiyonları **veriyi** döner (`response` nesnesini değil): `return data;`
- Backend camelCase JSON döner ve cevapları zarflamaz (`{ data: ... }` yok). Tahmine dayalı
  fallback (`res.data?.data || res.data`, `item.ProductId || item.productId`) yazılmaz.
- Hata mesajı her zaman `getErrorMessage(err, "varsayılan mesaj")` ile okunur.

### 2. Sayfa verisi `useAsync` ile çekilir
Loading, hata ve race condition (eski isteğin geç gelen cevabı) tek yerde çözülür; sayfalar bunu elle yazmaz.

```jsx
const { data: order, error, isLoading, reload } = useAsync(() => getMyOrder(id), [id]);
```

- İkinci parametre `useEffect`'teki gibi bağımlılık listesidir; ESLint eksik bağımlılığı uyarır.
- Değişiklikten sonra listeyi yenilemek için `reload()` (eski veri yenilenirken ekranda kalır).
- Hata mesajı: `getErrorMessage(error, "...")`. Özel durumlar `error.response?.status` ile ayrılır (örn. 404).
- Birden fazla isteğin birleşimi gerekiyorsa sayfa modülünde küçük bir `load...()` fonksiyonu yazılır
  (bkz. `ProductDetailPage` → `loadProductDetail`).
- `useEffect` doğrudan yalnızca dış bir sistemle senkronizasyon için kullanılır: SignalR aboneliği
  (`useOrderHub`), girişte sepet birleştirme (`CartContext`). "Sayfa açılınca veri getir" bunlardan biri değildir.

### 3. Layout ve yetki kontrolü route ağacında, sayfada değil
Sayfa kendini `<AdminLayout>` ile sarmaz, `<ProtectedRoute>` içine almaz. Bunlar `AppRoutes.jsx`'te
path'siz "layout route" olarak bir kez yazılır; içlerindeki `<Outlet />` eşleşen sayfanın yerini belirler.

```jsx
<Route path="/admin" element={<AdminRoute />}>      {/* yetki: admin değilse sayfaya hiç inilmez */}
  <Route element={<AdminLayout />}>                 {/* iskelet: kenar çubuğu */}
    <Route path="orders" element={<AdminOrdersPage />} />
  </Route>
</Route>
```

- Yeni korumalı sayfa = doğru `<Route>` grubunun içine bir satır.
- Guard'lar (`ProtectedRoute`, `AdminRoute`) yalnızca arayüzü yönetir; asıl yetki kontrolü backend'de
  (`[Authorize(Roles = "Admin")]`).
- Render saf kalır: `alert()`, `confirm()` gibi tarayıcı diyalogları kullanılmaz; durum ekranda gösterilir
  (örn. silme için satır içi onay, yetkisiz erişim için `EmptyState`).

### 4. Bağımlılıklar tek yönde akar
```
shared, lib, config   <──   features   <──   app
```
- `shared/`, `lib/`, `config/` hiçbir feature'ı import etmez.
- `app/` her şeyi import edebilir (route'lar, layout'lar).
- Feature'lar arası bağımlılıklar (döngü yasak; yeni bir ok eklemeden önce bu listeye bak):

| Feature   | Kullanabildikleri         |
|-----------|---------------------------|
| `auth`    | — (hiçbir feature)        |
| `cart`    | `auth`                    |
| `catalog` | `auth`, `cart`            |
| `orders`  | `auth`, `cart`            |
| `admin`   | `auth`, `catalog`, `orders` |

  Örnek: `catalog` sepete ürün eklemek için `cart`'ı kullanır; `cart` asla `catalog`'u import etmez.
  Döngü (A → B → A) olursa iki feature aslında birbirinden ayrılamıyor demektir: ortak parça `shared/`'a alınır.

### 5. Bir şeyin yeri, onu kimin kullandığına göre seçilir
- Tek feature kullanıyorsa → o feature'ın içinde.
- Birden fazla feature kullanıyor ve iş mantığı içermiyorsa → `shared/`.
- Örnek: `formatPrice` her yerde kullanılıyor → `shared/utils`. `getOrderStatusView` yalnızca
  sipariş durumu bilir → `features/orders`.

### 6. State
- Türetilebilen değer state'e konmaz, render sırasında hesaplanır
  (örn. `AuthContext`'te `user` token'dan, `CartContext`'te `cart` misafir/kullanıcı sepetinden).
- Global state yalnızca gerçekten global olan için: oturum (`AuthContext`) ve sepet (`CartContext`).
  Sayfaya ait veri sayfanın kendi state'inde kalır.
- `setState` updater fonksiyonları saftır; HTTP isteği veya localStorage yazımı event handler'da yapılır.

### 7. Stil
- Her bileşen/sayfa kendi `.module.css` dosyasını kullanır; sabit renk/boşluk yerine `tokens.css` değişkenleri.
  Yeni bir renk gerekiyorsa önce anlamlı bir token eklenir (örn. `--color-sidebar-bg`).
- Inline `style={{}}` kullanılmaz.
- Form alanı her zaman `FormField`, hata/başarı mesajı her zaman `Alert`: aynı input stili üç yerde kopyalanmaz.
- Bir feature'ın birden çok sayfasında ortak olan stiller feature kökünde durur
  (örn. `features/admin/admin.module.css` → `import ui from "../admin.module.css"`).
- Dikkat: CSS Modules'te tanımsız sınıf (`styles.yanlisAd`) hata vermez, sessizce `undefined` olur.
  Sınıf adını değiştirince kullanıldığı yerleri de ara.

### 8. Ortam ayarları
- Ayarlar `config/env.js` üzerinden okunur. Vite `VITE_` değişkenlerini bundle'a gömer: secret yazılmaz.
- Production build'de `VITE_API_URL` zorunludur (`vite.config.js` build'i durdurur). Bkz. `.env.example`.

## Yeni bir özellik eklemek (örnek: "Favoriler")

1. `features/favorites/api.js` → `getFavorites()`, `addFavorite(productId)` ...
2. `features/favorites/pages/FavoritesPage.jsx` + `FavoritesPage.module.css`
3. Gerekirse `features/favorites/components/` altında feature'a özel bileşenler
4. `app/AppRoutes.jsx` → giriş gerektirdiği için `<Route element={<ProtectedRoute />}>` grubunun içine:
   `<Route path="/favorites" element={<FavoritesPage />} />`
5. Başka bir feature'ın favorilere ihtiyacı varsa (örn. `catalog` kartta kalp ikonu) bağımlılık yönünü kontrol et:
   `catalog → favorites` olur, `favorites → catalog` olmaz.

## Komutlar

```bash
npm run dev      # Geliştirme sunucusu, her zaman :5173 (backend CORS bu origin'e izin verir; port doluysa hata verir)
                 # VITE_API_URL yoksa http://localhost:5000
npm run lint     # ESLint
npm run build    # Production build (VITE_API_URL zorunlu)
```
