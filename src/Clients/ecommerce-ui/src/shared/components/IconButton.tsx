import type { ComponentProps } from "react";
import type { LucideIcon } from "lucide-react";
import styles from "./IconButton.module.css";

interface IconButtonProps extends Omit<ComponentProps<"button">, "children" | "aria-label" | "title"> {
  icon: LucideIcon;
  // Yalnızca ikon içeren butonun adı: ekran okuyucu ve fareyle üzerine gelince ipucu
  label: string;
  variant?: "ghost" | "outline";
  size?: "sm" | "md";
  // Sepet adedi gibi sayaç
  badge?: number;
}

export default function IconButton({
  icon: Icon,
  label,
  variant = "ghost",
  size = "md",
  badge,
  className,
  ...props
}: IconButtonProps) {
  return (
    <button
      type="button"
      aria-label={badge ? `${label}, ${badge}` : label}
      title={label}
      className={[styles.button, styles[variant], styles[size], className].filter(Boolean).join(" ")}
      {...props}
    >
      <Icon size={size === "sm" ? 16 : 20} aria-hidden="true" />
      {badge ? <span className={styles.badge}>{badge > 99 ? "99+" : badge}</span> : null}
    </button>
  );
}
