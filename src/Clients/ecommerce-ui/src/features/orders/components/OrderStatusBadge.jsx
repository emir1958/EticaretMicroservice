import { getOrderStatusView } from "../orderStatus";
import styles from "./OrderStatusBadge.module.css";

export default function OrderStatusBadge({ status }) {
  const view = getOrderStatusView(status);
  return <span className={`${styles.badge} ${styles[view.tone]}`}>{view.label}</span>;
}
