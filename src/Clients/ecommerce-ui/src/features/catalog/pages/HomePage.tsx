import { Link } from "react-router-dom";
import { ArrowRight, ArrowUpRight, PackageCheck, Radio, ShieldCheck, Truck } from "lucide-react";
import { brand } from "../../../config/brand";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Button, { ButtonLink } from "../../../shared/components/Button";
import Picture from "../../../shared/components/Picture";
import { formatPrice } from "../../../shared/utils/format";
import { getProductsWithStock } from "../api";
import { discountPercent, listCategories, sortProducts, UNCATEGORIZED } from "../catalogView";
import ProductGrid from "../components/ProductGrid";
import styles from "./HomePage.module.css";

const PROMISES = [
  { icon: Truck, title: "Ücretsiz kargo", text: "Tüm siparişlerde, alt limit olmadan." },
  { icon: PackageCheck, title: "Stok senin için ayrılır", text: "Siparişi verdiğin an ürün rezerve edilir." },
  { icon: Radio, title: "Canlı sipariş takibi", text: "Her adım anında ekranına düşer." },
  { icon: ShieldCheck, title: "Güvenli ödeme", text: "Kart bilgin sunucularımızda saklanmaz." },
];

const categoryLink = (name: string) => `/products?category=${encodeURIComponent(name)}`;

export default function HomePage() {
  const { data: products = [], error, isLoading, reload } = useAsync(getProductsWithStock, []);

  const featured = sortProducts(products, "featured");
  const inStock = featured.filter((p) => p.availableStock > 0);
  const heroProducts = inStock.filter((p) => p.imageUrl).slice(0, 2);
  const onSale = inStock.filter((p) => discountPercent(p) !== null);
  const categories = listCategories(products).filter((c) => c.name !== UNCATEGORIZED);

  return (
    <>
      <section className={styles.hero}>
        <div className={styles.heroText}>
          <span className={styles.eyebrow}>{brand.tagline}</span>
          <h1 className={styles.heroTitle}>
            Masanı kur, <em>işine odaklan.</em>
          </h1>
          <p className={styles.heroLead}>{brand.description}</p>
          <div className={styles.heroActions}>
            <ButtonLink to="/products" size="lg">
              Koleksiyonu keşfet <ArrowRight size={18} aria-hidden="true" />
            </ButtonLink>
            {onSale.length > 0 && (
              <ButtonLink to="/products?sale=1" size="lg" variant="secondary">
                İndirimdekiler
              </ButtonLink>
            )}
          </div>
        </div>

        <div className={styles.heroVisual}>
          {heroProducts.map((product, index) => (
            <Link
              key={product.id}
              to={`/product/${product.id}`}
              className={`${styles.heroCard} ${index === 0 ? styles.heroCardMain : styles.heroCardSide}`}
            >
              <Picture
                src={product.imageUrl}
                alt={product.name}
                width={index === 0 ? 640 : 360}
                loading="eager"
                className={styles.heroPicture}
              />
              <span className={styles.heroChip}>
                <span className={styles.heroChipName}>{product.name}</span>
                <span className={styles.heroChipPrice}>{formatPrice(product.price)}</span>
              </span>
            </Link>
          ))}
        </div>
      </section>

      <section className={styles.promises} aria-label="Neden düzen">
        {PROMISES.map(({ icon: Icon, title, text }) => (
          <div key={title} className={styles.promise}>
            <Icon size={22} className={styles.promiseIcon} aria-hidden="true" />
            <div>
              <h2 className={styles.promiseTitle}>{title}</h2>
              <p className={styles.promiseText}>{text}</p>
            </div>
          </div>
        ))}
      </section>

      {error && (
        <Alert className={styles.error}>
          Ürünler şu anda yüklenemiyor.{" "}
          <Button variant="ghost" size="sm" onClick={reload}>
            Tekrar dene
          </Button>
        </Alert>
      )}

      {categories.length > 0 && (
        <section className={styles.section}>
          <div className={styles.sectionHead}>
            <h2 className={styles.sectionTitle}>Kategoriler</h2>
            <Link to="/products" className={styles.sectionLink}>
              Tüm ürünler <ArrowRight size={16} aria-hidden="true" />
            </Link>
          </div>
          <div className={styles.categories}>
            {categories.map((category) => (
              <Link key={category.name} to={categoryLink(category.name)} className={styles.category}>
                <Picture src={category.imageUrl} alt="" width={420} className={styles.categoryPicture} />
                <span className={styles.categoryLabel}>
                  <span>
                    <span className={styles.categoryName}>{category.name}</span>
                    <span className={styles.categoryCount}>{category.count} ürün</span>
                  </span>
                  <ArrowUpRight size={18} aria-hidden="true" />
                </span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {!error && (
        <section className={styles.section}>
          <div className={styles.sectionHead}>
            <h2 className={styles.sectionTitle}>Öne çıkanlar</h2>
            <Link to="/products" className={styles.sectionLink}>
              Hepsini gör <ArrowRight size={16} aria-hidden="true" />
            </Link>
          </div>
          <ProductGrid products={inStock.slice(0, 8)} isLoading={isLoading} columns="wide" />
        </section>
      )}

      <section className={styles.banner}>
        <div>
          <span className={styles.bannerEyebrow}>Çalışma alanı rehberi</span>
          <h2 className={styles.bannerTitle}>Sessiz bir klavye, doğru ışık, toplu bir masa.</h2>
          <p className={styles.bannerText}>
            İyi bir çalışma günü çoğu zaman küçük ayrıntılarda saklıdır. Seçkimizi buna göre hazırladık.
          </p>
        </div>
        <ButtonLink to="/products?sort=price-asc" variant="secondary" size="lg">
          Uygun fiyatlılardan başla
        </ButtonLink>
      </section>

      {onSale.length > 0 && (
        <section className={styles.section}>
          <div className={styles.sectionHead}>
            <h2 className={styles.sectionTitle}>İndirimdekiler</h2>
            <Link to="/products?sale=1" className={styles.sectionLink}>
              Tüm indirimler <ArrowRight size={16} aria-hidden="true" />
            </Link>
          </div>
          <ProductGrid products={sortProducts(onSale, "discount").slice(0, 4)} columns="wide" />
        </section>
      )}
    </>
  );
}
