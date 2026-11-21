import styles from "./Skeleton.module.css";

// Yüklenen içeriğin yer tutucusu. Boyutu çağıranın verdiği className belirler (inline style kullanılmaz).
export default function Skeleton({ className }: { className?: string }) {
  return <span className={[styles.skeleton, className].filter(Boolean).join(" ")} aria-hidden="true" />;
}
