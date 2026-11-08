// Ortama göre değişen ayarlar tek yerde okunur. Kodun geri kalanı import.meta.env'e doğrudan dokunmaz.
//
// Vite yalnızca VITE_ ön ekli değişkenleri tarayıcı koduna gömer ve bunu build anında yapar:
// değer bundle'ın içine yazılır, yani burada asla secret tutulmaz (herkes görebilir).
const DEV_GATEWAY_URL = "http://localhost:5000";

// Varsayılan yalnızca geliştirmede devreye girer: production build'de değer yoksa
// vite.config.js build'i durdurur (hata tarayıcıda değil, build anında alınır).
const gatewayUrl = (import.meta.env.VITE_API_URL || DEV_GATEWAY_URL).replace(/\/$/, "");

export const env = {
  apiBaseUrl: gatewayUrl,
  orderHubUrl: `${gatewayUrl}/orderhub`,
};
