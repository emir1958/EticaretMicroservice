export const formatPrice = (value: number | null | undefined) =>
  Number(value ?? 0).toLocaleString("tr-TR", { style: "currency", currency: "TRY" });

// Özet kartları için: kuruşsuz ve büyük tutarlarda kısaltılmış (₺128,4 B)
export const formatCompactPrice = (value: number) =>
  value.toLocaleString("tr-TR", {
    style: "currency",
    currency: "TRY",
    notation: value >= 100_000 ? "compact" : "standard",
    maximumFractionDigits: value >= 100_000 ? 1 : 0,
  });

export const formatNumber = (value: number) => value.toLocaleString("tr-TR");

const HAS_TIMEZONE = /(Z|[+-]\d{2}:?\d{2})$/;

// Backend saatleri UTC yazar ama bazı alanlarda "Z" eki olmadan döner; eksikse UTC kabul edilir
export const parseApiDate = (value: string) => new Date(HAS_TIMEZONE.test(value) ? value : `${value}Z`);

export const formatDate = (value: string | null | undefined) => {
  if (!value) return "-";
  return parseApiDate(value).toLocaleString("tr-TR", { dateStyle: "medium", timeStyle: "short" });
};

export const formatDay = (value: string | null | undefined) => {
  if (!value) return "-";
  return parseApiDate(value).toLocaleDateString("tr-TR", { dateStyle: "medium" });
};
