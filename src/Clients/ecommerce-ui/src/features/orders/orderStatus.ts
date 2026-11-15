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
