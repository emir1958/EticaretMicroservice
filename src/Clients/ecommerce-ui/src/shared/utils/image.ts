const RESIZABLE_HOST = "images.unsplash.com";

// Görsel, gösterileceği genişlikte istenir: 64px'lik küçük resim için 900px'lik dosya indirilmez.
// Yalnızca boyut parametresini bildiğimiz CDN'de uygulanır; diğer adresler olduğu gibi kalır.
export function sizedImageUrl(src: string, width: number) {
  let url: URL;
  try {
    url = new URL(src);
  } catch {
    return src;
  }

  if (url.hostname !== RESIZABLE_HOST) return src;

  url.searchParams.set("w", String(width));
  url.searchParams.set("auto", "format");
  url.searchParams.set("fit", "crop");
  url.searchParams.set("q", "75");
  url.searchParams.delete("h");
  return url.toString();
}
