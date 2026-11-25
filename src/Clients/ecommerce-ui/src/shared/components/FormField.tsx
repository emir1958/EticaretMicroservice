import { useId, type ComponentProps, type ReactNode } from "react";
import { ChevronDown, type LucideIcon } from "lucide-react";
import styles from "./FormField.module.css";

interface FieldProps {
  label: string;
  icon?: LucideIcon;
  hint?: ReactNode;
  // Alanın altında gösterilen doğrulama hatası; ekran okuyucuya aria-invalid ve aria-describedby ile bildirilir
  error?: string | null;
  // Alanın sağ ucuna yerleşen kontrol (örn. şifreyi göster butonu)
  trailing?: ReactNode;
  className?: string;
  inputClassName?: string;
}

type FormFieldProps = FieldProps & Omit<ComponentProps<"input">, "id" | "className">;
type TextAreaFieldProps = Omit<FieldProps, "trailing"> & Omit<ComponentProps<"textarea">, "id" | "className">;
type SelectFieldProps = Omit<FieldProps, "trailing" | "icon"> & Omit<ComponentProps<"select">, "id" | "className">;

interface FieldShellProps extends Omit<FieldProps, "inputClassName"> {
  id: string;
  describedBy: string | undefined;
  children: ReactNode;
}

function FieldShell({ id, describedBy, label, icon: Icon, hint, error, trailing, className, children }: FieldShellProps) {
  return (
    <div className={className}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <div className={styles.wrap}>
        {Icon && <Icon size={18} className={styles.icon} aria-hidden="true" />}
        {children}
        {trailing && <div className={styles.trailing}>{trailing}</div>}
      </div>
      {(error || hint) && (
        <small id={describedBy} className={error ? styles.error : styles.hint}>
          {error || hint}
        </small>
      )}
    </div>
  );
}

function useFieldIds(hasDescription: boolean) {
  const id = useId();
  return { id, describedBy: hasDescription ? `${id}-desc` : undefined };
}

function controlClasses(...extra: (string | false | undefined)[]) {
  return [styles.control, ...extra].filter(Boolean).join(" ");
}

export default function FormField({
  label,
  icon,
  hint,
  error,
  trailing,
  className,
  inputClassName,
  ...inputProps
}: FormFieldProps) {
  const { id, describedBy } = useFieldIds(Boolean(error || hint));
  return (
    <FieldShell
      id={id}
      describedBy={describedBy}
      label={label}
      icon={icon}
      hint={hint}
      error={error}
      trailing={trailing}
      className={className}
    >
      <input
        id={id}
        aria-describedby={describedBy}
        aria-invalid={error ? true : undefined}
        className={controlClasses(icon && styles.withIcon, Boolean(trailing) && styles.withTrailing, inputClassName)}
        {...inputProps}
      />
    </FieldShell>
  );
}

export function TextAreaField({ label, icon, hint, error, className, inputClassName, ...textareaProps }: TextAreaFieldProps) {
  const { id, describedBy } = useFieldIds(Boolean(error || hint));
  return (
    <FieldShell id={id} describedBy={describedBy} label={label} icon={icon} hint={hint} error={error} className={className}>
      <textarea
        id={id}
        aria-describedby={describedBy}
        aria-invalid={error ? true : undefined}
        className={controlClasses(icon && styles.withIcon, styles.textarea, inputClassName)}
        {...textareaProps}
      />
    </FieldShell>
  );
}

export function SelectField({ label, hint, error, className, inputClassName, children, ...selectProps }: SelectFieldProps) {
  const { id, describedBy } = useFieldIds(Boolean(error || hint));
  return (
    <FieldShell
      id={id}
      describedBy={describedBy}
      label={label}
      hint={hint}
      error={error}
      className={className}
      trailing={<ChevronDown size={16} className={styles.chevron} aria-hidden="true" />}
    >
      <select
        id={id}
        aria-describedby={describedBy}
        aria-invalid={error ? true : undefined}
        className={controlClasses(styles.select, inputClassName)}
        {...selectProps}
      >
        {children}
      </select>
    </FieldShell>
  );
}
