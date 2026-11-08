// Sipariş durumunun ekranda nasıl görüneceği.
// API enum adını döner (Beklemede, Tamamlandı, IptalEdildi), SignalR ise Completed / Canceled gönderir;
// ikisi de aynı etikete eşlenir.
// tone, OrderStatusBadge'deki CSS sınıfını seçer (renkler CSS'te)
const STATUS_VIEW = {
  pending: { label: "Ödeme Bekleniyor", tone: "pending" },
  completed: { label: "Tamamlandı", tone: "completed" },
  canceled: { label: "İptal Edildi", tone: "canceled" },
};

const STATUS_KEYS = {
  Beklemede: "pending",
  Pending: "pending",
  Tamamlandı: "completed",
  Completed: "completed",
  IptalEdildi: "canceled",
  Canceled: "canceled",
};

export function getOrderStatusView(status) {
  return STATUS_VIEW[STATUS_KEYS[status]] ?? { label: status || "Bilinmiyor", tone: "unknown" };
}

export const formatPrice = (value) =>
  Number(value ?? 0).toLocaleString("tr-TR", { style: "currency", currency: "TRY" });

// API tarihleri UTC olarak saklar ama JSON'da saat dilimi eki (Z) olmadan döner; tarayıcı bunu yerel saat sanıp
// 3 saat kaydırır. Eki olmayan değer UTC kabul edilir.
const HAS_TIMEZONE = /(Z|[+-]\d{2}:?\d{2})$/;

export const formatDate = (value) => {
  if (!value) return "-";
  const date = new Date(HAS_TIMEZONE.test(value) ? value : `${value}Z`);
  return date.toLocaleString("tr-TR", { dateStyle: "medium", timeStyle: "short" });
};
