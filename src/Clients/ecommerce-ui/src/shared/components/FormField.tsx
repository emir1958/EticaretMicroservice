import { useId } from "react";
import styles from "./FormField.module.css";

// Etiket + giriş alanı. Kalan tüm prop'lar (type, value, onChange, required, placeholder...)
// doğrudan <input>'a (veya as="textarea" ise <textarea>'ya) geçer.
//
//   <FormField label="E-Posta" icon={Mail} type="email" value={email} onChange={...} required />
//
// useId: label'ı input'a bağlayan benzersiz id (label'a tıklayınca odak, ekran okuyucu desteği).
// Aynı form sayfada iki kez çizilse bile id'ler çakışmaz.
export default function FormField({
  label,
  icon: Icon,
  hint,
  as: Control = "input",
  className,
  inputClassName,
  ...controlProps
}) {
  const id = useId();
  const hintId = hint ? `${id}-hint` : undefined;

  const controlClasses = [
    styles.control,
    Icon && styles.withIcon,
    Control === "textarea" && styles.textarea,
    inputClassName,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={className}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <div className={styles.wrap}>
        {Icon && <Icon size={18} className={styles.icon} aria-hidden="true" />}
        <Control id={id} aria-describedby={hintId} className={controlClasses} {...controlProps} />
      </div>
      {hint && (
        <small id={hintId} className={styles.hint}>
          {hint}
        </small>
      )}
    </div>
  );
}
