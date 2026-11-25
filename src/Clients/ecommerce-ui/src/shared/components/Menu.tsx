import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import styles from "./Menu.module.css";

interface MenuProps {
  // Menüyü açan butonun içeriği
  trigger: ReactNode;
  triggerLabel: string;
  triggerClassName?: string;
  align?: "start" | "end";
  children: ReactNode;
}

// Küçük açılır menü (hesap menüsü gibi). İçindeki bir öğeye tıklanınca, dışarı tıklanınca veya Esc ile kapanır.
export default function Menu({ trigger, triggerLabel, triggerClassName, align = "end", children }: MenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const menuId = useId();

  // Menü açıkken belge dinlenir: dışarı tıklama ve Esc
  useEffect(() => {
    if (!isOpen) return;

    const onPointerDown = (e: PointerEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setIsOpen(false);
    };
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") setIsOpen(false);
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [isOpen]);

  return (
    <div ref={rootRef} className={styles.root}>
      <button
        type="button"
        className={triggerClassName}
        aria-label={triggerLabel}
        aria-expanded={isOpen}
        aria-controls={menuId}
        onClick={() => setIsOpen((open) => !open)}
      >
        {trigger}
      </button>
      {isOpen && (
        <div id={menuId} className={`${styles.menu} ${styles[align]}`} onClick={() => setIsOpen(false)}>
          {children}
        </div>
      )}
    </div>
  );
}
