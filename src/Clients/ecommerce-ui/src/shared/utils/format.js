// Görüntüleme biçimleri. Birden fazla feature kullandığı için shared altında.

export const formatPrice = (value) =>
  Number(value ?? 0).toLocaleString("tr-TR", { style: "currency", currency: "TRY" });

// API tarihleri UTC olarak saklar ama JSON'da saat dilimi eki (Z) olmadan döner; tarayıcı bunu yerel saat sanıp
// 3 saat kaydırır. Eki olmayan değer UTC kabul edilir. (Kalıcı çözüm backend'de: roadmap 5.1)
const HAS_TIMEZONE = /(Z|[+-]\d{2}:?\d{2})$/;

export const formatDate = (value) => {
  if (!value) return "-";
  const date = new Date(HAS_TIMEZONE.test(value) ? value : `${value}Z`);
  return date.toLocaleString("tr-TR", { dateStyle: "medium", timeStyle: "short" });
};
