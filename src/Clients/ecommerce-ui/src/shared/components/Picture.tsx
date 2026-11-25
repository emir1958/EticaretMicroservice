import { useState } from "react";
import { ImageOff } from "lucide-react";
import { sizedImageUrl } from "../utils/image";
import styles from "./Picture.module.css";

interface PictureProps {
  src: string | null | undefined;
  alt: string;
  // Ekranda kaplayacağı yaklaşık genişlik (px): doğru boyutta dosya istemek için
  width: number;
  // Kutunun boyutunu ve oranını çağıran belirler; görsel kutuyu doldurur
  className?: string;
  loading?: "lazy" | "eager";
}

// Ürün görseli. Adres boşsa veya yüklenemezse kırık resim ikonu yerine sade bir yer tutucu gösterir.
export default function Picture({ src, alt, width, className, loading = "lazy" }: PictureProps) {
  // Hangi adresin başarısız olduğu saklanır: src değişince yeni görsel kendiliğinden yeniden denenir
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const showImage = Boolean(src) && failedSrc !== src;

  return (
    <div className={[styles.picture, className].filter(Boolean).join(" ")}>
      {showImage && src ? (
        <img
          src={sizedImageUrl(src, width)}
          srcSet={`${sizedImageUrl(src, width)} 1x, ${sizedImageUrl(src, width * 2)} 2x`}
          alt={alt}
          loading={loading}
          decoding="async"
          onError={() => setFailedSrc(src)}
        />
      ) : (
        <ImageOff className={styles.placeholder} role="img" aria-label={alt ? `${alt} (görsel yok)` : "Görsel yok"} />
      )}
    </div>
  );
}
