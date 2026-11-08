import { Link } from "react-router-dom";
import styles from "./Button.module.css";

// variant: primary | secondary | ghost | danger, size: sm | md | lg
// `to` verilirse router Link'i olarak render edilir; link ve butonlar aynı görünür.
export default function Button({ variant = "primary", size = "md", fullWidth = false, to, className, children, ...props }) {
  const classes = [styles.button, styles[variant], styles[size], fullWidth && styles.fullWidth, className]
    .filter(Boolean)
    .join(" ");

  if (to) {
    return (
      <Link to={to} className={classes} {...props}>
        {children}
      </Link>
    );
  }

  return (
    <button type="button" className={classes} {...props}>
      {children}
    </button>
  );
}
