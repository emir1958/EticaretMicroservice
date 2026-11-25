import type { OrderHistoryType, PaymentStatus } from "./types";

export const PAYMENT_STATUS_LABEL: Record<PaymentStatus, string> = {
  Alinmadi: "Alınmadı",
  Alindi: "Alındı",
  Basarisiz: "Başarısız",
  IadeEdildi: "İade edildi",
};

export const HISTORY_VIEW: Record<OrderHistoryType, { label: string; tone: "positive" | "negative" | "neutral" }> = {
  SiparisAlindi: { label: "Sipariş alındı", tone: "neutral" },
  StokAyrildi: { label: "Stok ayrıldı", tone: "positive" },
  StokYetersiz: { label: "Stok yetersiz", tone: "negative" },
  OdemeAlindi: { label: "Ödeme alındı", tone: "positive" },
  OdemeBasarisiz: { label: "Ödeme başarısız", tone: "negative" },
  Tamamlandi: { label: "Sipariş tamamlandı", tone: "positive" },
  IptalEdildi: { label: "Sipariş iptal edildi", tone: "negative" },
  IadeEdildi: { label: "Ödeme iade edildi", tone: "neutral" },
};

export type StatusTone = "pending" | "completed" | "canceled" | "unknown";

export interface StatusView {
  label: string;
  tone: StatusTone;
}

const STATUS_VIEW: Record<Exclude<StatusTone, "unknown">, StatusView> = {
  pending: { label: "Ödeme Bekleniyor", tone: "pending" },
  completed: { label: "Tamamlandı", tone: "completed" },
  canceled: { label: "İptal Edildi", tone: "canceled" },
};

// API enum adını (Beklemede, Tamamlandı, IptalEdildi), SignalR ise saga durumunu (Completed, Canceled) gönderir
const STATUS_KEYS: Partial<Record<string, keyof typeof STATUS_VIEW>> = {
  Beklemede: "pending",
  Pending: "pending",
  Tamamlandı: "completed",
  Completed: "completed",
  IptalEdildi: "canceled",
  Canceled: "canceled",
};

export function getOrderStatusView(status: string | null | undefined): StatusView {
  const key = status ? STATUS_KEYS[status] : undefined;
  return key ? STATUS_VIEW[key] : { label: status || "Bilinmiyor", tone: "unknown" };
}
