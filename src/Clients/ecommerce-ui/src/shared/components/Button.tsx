import type { ComponentProps } from "react";
import { Link, type LinkProps } from "react-router-dom";
import styles from "./Button.module.css";

interface ButtonStyleProps {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md" | "lg";
  fullWidth?: boolean;
  className?: string;
}

type ButtonProps = ButtonStyleProps & ComponentProps<"button">;
type ButtonLinkProps = ButtonStyleProps & LinkProps;

function buttonClasses({ variant = "primary", size = "md", fullWidth = false, className }: ButtonStyleProps) {
  return [styles.button, styles[variant], styles[size], fullWidth && styles.fullWidth, className]
    .filter(Boolean)
    .join(" ");
}

export default function Button({ variant, size, fullWidth, className, ...props }: ButtonProps) {
  return <button type="button" className={buttonClasses({ variant, size, fullWidth, className })} {...props} />;
}

export function ButtonLink({ variant, size, fullWidth, className, ...props }: ButtonLinkProps) {
  return <Link className={buttonClasses({ variant, size, fullWidth, className })} {...props} />;
}
