import { Minus, Plus } from "lucide-react";
import styles from "./QuantityStepper.module.css";

interface QuantityStepperProps {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
  size?: "md" | "sm";
  label?: string;
}

export default function QuantityStepper({ value, onChange, min = 1, max = Infinity, size = "md", label }: QuantityStepperProps) {
  const prefix = label ? `${label} ` : "";
  const iconSize = size === "sm" ? 14 : 16;

  return (
    <div className={`${styles.stepper} ${styles[size]}`}>
      <button type="button" onClick={() => onChange(value - 1)} disabled={value <= min} aria-label={`${prefix}adedini azalt`}>
        <Minus size={iconSize} />
      </button>
      <span className={styles.value}>{value}</span>
      <button type="button" onClick={() => onChange(value + 1)} disabled={value >= max} aria-label={`${prefix}adedini artır`}>
        <Plus size={iconSize} />
      </button>
    </div>
  );
}
