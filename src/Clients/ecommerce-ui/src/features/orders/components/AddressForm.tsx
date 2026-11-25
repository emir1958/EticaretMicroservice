import FormField from "../../../shared/components/FormField";
import type { Address } from "../types";
import styles from "./AddressForm.module.css";

interface AddressFormProps {
  value: Address;
  onChange: (address: Address) => void;
}

// maxLength yerine: "0532 123 45 67" yapıştırılınca tarayıcı boşluklarla birlikte keser, rakamlar eksik kalır
const digits = (text: string, max: number) => text.replace(/\D/g, "").slice(0, max);

// Sınırlar backend'deki CreateOrderCommandValidator ile aynı
export default function AddressForm({ value, onChange }: AddressFormProps) {
  const set = (field: keyof Address, fieldValue: string) => onChange({ ...value, [field]: fieldValue });

  return (
    <div className={styles.grid}>
      <FormField
        className={styles.full}
        label="Alıcı adı soyadı"
        required
        maxLength={100}
        value={value.contactName}
        onChange={(e) => set("contactName", e.target.value)}
        autoComplete="name"
      />
      <FormField
        label="Telefon"
        type="tel"
        inputMode="numeric"
        pattern="0?5[0-9]{9}"
        placeholder="05XXXXXXXXX"
        hint="İsteğe bağlı, kargo bildirimi için"
        value={value.phone}
        onChange={(e) => set("phone", digits(e.target.value, 11))}
        autoComplete="tel-national"
      />
      <FormField
        label="Posta kodu"
        inputMode="numeric"
        pattern="[0-9]{5}"
        placeholder="34710"
        hint="İsteğe bağlı"
        value={value.zipCode}
        onChange={(e) => set("zipCode", digits(e.target.value, 5))}
        autoComplete="postal-code"
      />
      <FormField
        label="İl"
        required
        maxLength={50}
        value={value.city}
        onChange={(e) => set("city", e.target.value)}
        autoComplete="address-level1"
      />
      <FormField
        label="İlçe"
        required
        maxLength={50}
        value={value.district}
        onChange={(e) => set("district", e.target.value)}
        autoComplete="address-level2"
      />
      <FormField
        className={styles.full}
        label="Mahalle, cadde / sokak"
        required
        maxLength={100}
        value={value.street}
        onChange={(e) => set("street", e.target.value)}
        autoComplete="address-line1"
      />
      <FormField
        className={styles.full}
        label="Bina no, daire"
        required
        maxLength={250}
        value={value.line}
        onChange={(e) => set("line", e.target.value)}
        autoComplete="address-line2"
      />
    </div>
  );
}
