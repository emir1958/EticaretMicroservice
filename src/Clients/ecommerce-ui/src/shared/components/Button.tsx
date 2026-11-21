import type { ComponentProps } from "react";
import { Link, type LinkProps } from "react-router-dom";
import styles from "./Button.module.css";

interface ButtonStyleProps {
  variant?: "primary" | "accent" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md" | "lg";
  fullWidth?: boolean;
  className?: string;
}

type ButtonProps = ButtonStyleProps & ComponentProps<"button"> & { loading?: boolean };
type ButtonLinkProps = ButtonStyleProps & LinkProps;

function buttonClasses({ variant = "primary", size = "md", fullWidth = false, className }: ButtonStyleProps) {
  return [styles.button, styles[variant], styles[size], fullWidth && styles.fullWidth, className]
    .filter(Boolean)
    .join(" ");
}

// loading: buton devre dışı kalır ve dönen bir gösterge çıkar; metni çağıran belirler ("Kaydediliyor...")
export default function Button({
  variant,
  size,
  fullWidth,
  className,
  loading = false,
  disabled,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      type="button"
      className={buttonClasses({ variant, size, fullWidth, className })}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {loading && <span className={styles.spinner} aria-hidden="true" />}
      {children}
    </button>
  );
}

export function ButtonLink({ variant, size, fullWidth, className, ...props }: ButtonLinkProps) {
  return <Link className={buttonClasses({ variant, size, fullWidth, className })} {...props} />;
}
