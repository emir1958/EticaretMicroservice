import { useId, type ComponentProps, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import styles from "./FormField.module.css";

interface FieldProps {
  label: string;
  icon?: LucideIcon;
  hint?: ReactNode;
  className?: string;
  inputClassName?: string;
}

type FormFieldProps = FieldProps & Omit<ComponentProps<"input">, "id" | "className">;
type TextAreaFieldProps = FieldProps & Omit<ComponentProps<"textarea">, "id" | "className">;

interface FieldShellProps extends Omit<FieldProps, "inputClassName"> {
  id: string;
  hintId: string | undefined;
  children: ReactNode;
}

function FieldShell({ id, hintId, label, icon: Icon, hint, className, children }: FieldShellProps) {
  return (
    <div className={className}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <div className={styles.wrap}>
        {Icon && <Icon size={18} className={styles.icon} aria-hidden="true" />}
        {children}
      </div>
      {hint && (
        <small id={hintId} className={styles.hint}>
          {hint}
        </small>
      )}
    </div>
  );
}

function useFieldIds(hint: ReactNode) {
  const id = useId();
  return { id, hintId: hint ? `${id}-hint` : undefined };
}

function controlClasses(icon: LucideIcon | undefined, ...extra: (string | undefined)[]) {
  return [styles.control, icon && styles.withIcon, ...extra].filter(Boolean).join(" ");
}

export default function FormField({ label, icon, hint, className, inputClassName, ...inputProps }: FormFieldProps) {
  const { id, hintId } = useFieldIds(hint);
  return (
    <FieldShell id={id} hintId={hintId} label={label} icon={icon} hint={hint} className={className}>
      <input id={id} aria-describedby={hintId} className={controlClasses(icon, inputClassName)} {...inputProps} />
    </FieldShell>
  );
}

export function TextAreaField({ label, icon, hint, className, inputClassName, ...textareaProps }: TextAreaFieldProps) {
  const { id, hintId } = useFieldIds(hint);
  return (
    <FieldShell id={id} hintId={hintId} label={label} icon={icon} hint={hint} className={className}>
      <textarea
        id={id}
        aria-describedby={hintId}
        className={controlClasses(icon, styles.textarea, inputClassName)}
        {...textareaProps}
      />
    </FieldShell>
  );
}
