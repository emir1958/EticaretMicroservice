import Badge, { type BadgeTone } from "../../../shared/components/Badge";
import { getOrderStatusView, PAYMENT_STATUS_LABEL, type StatusTone } from "../orderStatus";
import type { PaymentStatus } from "../types";

const STATUS_TONE: Record<StatusTone, BadgeTone> = {
  pending: "warning",
  completed: "success",
  canceled: "danger",
  unknown: "neutral",
};

const PAYMENT_TONE: Record<PaymentStatus, BadgeTone> = {
  Alinmadi: "neutral",
  Alindi: "success",
  Basarisiz: "danger",
  IadeEdildi: "info",
};

export default function OrderStatusBadge({ status }: { status: string | undefined }) {
  const view = getOrderStatusView(status);
  return (
    <Badge tone={STATUS_TONE[view.tone]} dot>
      {view.label}
    </Badge>
  );
}

export function PaymentStatusBadge({ status }: { status: PaymentStatus }) {
  return <Badge tone={PAYMENT_TONE[status]}>{PAYMENT_STATUS_LABEL[status]}</Badge>;
}
