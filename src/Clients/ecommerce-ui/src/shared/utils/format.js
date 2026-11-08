

export const formatPrice = (value) =>
  Number(value ?? 0).toLocaleString("tr-TR", { style: "currency", currency: "TRY" });


const HAS_TIMEZONE = /(Z|[+-]\d{2}:?\d{2})$/;

export const formatDate = (value) => {
  if (!value) return "-";
  const date = new Date(HAS_TIMEZONE.test(value) ? value : `${value}Z`);
  return date.toLocaleString("tr-TR", { dateStyle: "medium", timeStyle: "short" });
};
