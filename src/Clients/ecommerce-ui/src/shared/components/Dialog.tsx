import { useEffect, useId, useRef, type ReactNode } from "react";
import { X } from "lucide-react";
import IconButton from "./IconButton";
import styles from "./Dialog.module.css";

interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  // right/left: kenardan açılan çekmece, center: ortada modal
  placement?: "right" | "left" | "center";
  size?: "sm" | "md" | "lg";
  // Başlığın yanında gösterilen ek içerik (örn. ürün sayısı)
  titleExtra?: ReactNode;
  footer?: ReactNode;
  // Görünümü değiştirmek için (örn. koyu zeminli menü çekmecesi)
  className?: string;
  children: ReactNode;
}

// Tarayıcının <dialog> öğesi üzerine kurulu. showModal() odak tuzağını, Esc ile kapanmayı ve
// arkadaki sayfanın erişilemez olmasını kendisi sağlar; bunlar için ayrıca kod yazılmaz.
export default function Dialog({
  open,
  onClose,
  title,
  placement = "right",
  size = "md",
  titleExtra,
  footer,
  className,
  children,
}: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  // React state'i ile tarayıcının dialog durumu eşitlenir (dış sistemle senkronizasyon)
  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      className={[styles.dialog, styles[placement], styles[size], className].filter(Boolean).join(" ")}
      aria-labelledby={titleId}
      onClose={onClose}
      // Panelin dışına (arka plana) tıklanınca hedef dialog'un kendisidir
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      {open && (
        <div className={styles.panel}>
          <header className={styles.header}>
            <h2 id={titleId} className={styles.title}>
              {title}
              {titleExtra}
            </h2>
            <IconButton icon={X} label="Kapat" onClick={onClose} />
          </header>
          <div className={styles.body}>{children}</div>
          {footer && <footer className={styles.footer}>{footer}</footer>}
        </div>
      )}
    </dialog>
  );
}
