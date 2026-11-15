import { useState, useRef, type SubmitEvent } from "react";
import { Link } from "react-router-dom";
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Loader2,
  CreditCard,
  ShieldCheck,
  ShoppingBag,
  MapPin,
} from "lucide-react";
import { useCart } from "../../cart/CartContext";
import { useAuth } from "../../auth/AuthContext";
import { useOrderHub } from "../useOrderHub";
import { getErrorMessage, getErrorStatus } from "../../../lib/apiClient";
import { createOrder } from "../api";
import AddressForm from "../components/AddressForm";
import type { Address } from "../types";
import Alert from "../../../shared/components/Alert";
import Button, { ButtonLink } from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import FormField from "../../../shared/components/FormField";
import { formatPrice } from "../../../shared/utils/format";
import styles from "./CheckoutPage.module.css";

const EMPTY_ADDRESS: Address = {
  contactName: "",
  phone: "",
  city: "",
  district: "",
  street: "",
  zipCode: "",
  line: "",
};

const trimAddress = (address: Address): Address => ({
  contactName: address.contactName.trim(),
  phone: address.phone,
  city: address.city.trim(),
  district: address.district.trim(),
  street: address.street.trim(),
  zipCode: address.zipCode,
  line: address.line.trim(),
});

export default function CheckoutPage() {
  const { cart, totalAmount, clearCart } = useCart();
  const { isAdmin } = useAuth();
  const { orderStatus } = useOrderHub();

  // Kart Form State'leri
  const [cardHolder, setCardHolder] = useState("");
  const [cardNumber, setCardNumber] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvv, setCvv] = useState("");
  const [address, setAddress] = useState(EMPTY_ADDRESS);

  const idempotencyKeyRef = useRef(crypto.randomUUID());
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdOrderId, setCreatedOrderId] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const formatCardNumber = (val: string) => {
    const raw = val.replace(/\D/g, "").slice(0, 16);
    return raw.match(/.{1,4}/g)?.join(" ") || raw;
  };

  const formatExpiry = (val: string) => {
    const raw = val.replace(/\D/g, "").slice(0, 4);
    if (raw.length >= 3) return `${raw.slice(0, 2)}/${raw.slice(2)}`;
    return raw;
  };

  // 🟢 2. YÖNETİCİ BLOKAJI: Admin hesabı bu sayfada sipariş veremez
  if (isAdmin) {
    return (
      <EmptyState
        icon={ShieldCheck}
        title="Yönetici hesabı ile sipariş verilemez"
        text="Şu anda Admin yetkisine sahip bir hesapla oturum açtınız. Yönetici hesapları üzerinden sipariş oluşturulamaz."
        action={
          <div className={styles.resultActions}>
            <ButtonLink to="/admin/orders">Yönetici Paneline Git</ButtonLink>
            <ButtonLink to="/" variant="secondary">
              Mağazaya Dön
            </ButtonLink>
          </div>
        }
      />
    );
  }

  // 🟢 3. SEPET BOŞ VE SİPARİŞ HENÜZ VERİLMEDİYSE UYARI
  if (cart.length === 0 && !createdOrderId) {
    return (
      <EmptyState
        icon={ShoppingBag}
        title="Sepetiniz boş"
        text="Ödeme yapabilmek için sepetinize en az bir ürün eklemelisiniz."
        action={<ButtonLink to="/">Alışverişe Başla</ButtonLink>}
      />
    );
  }

  const handleCreateOrder = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (cart.length === 0) return;

    const rawCard = cardNumber.replace(/\s/g, "");
    if (rawCard.length !== 16 || cvv.length < 3 || expiry.length !== 5) {
      setErrorMessage("Lütfen kart bilgilerinizi eksiksiz ve doğru girin.");
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    // Demo ödeme: karttan yalnızca son 4 hane kullanılır (sağlayıcı entegrasyonu roadmap Faz 2).
    // Alıcı gönderilmez: backend kullanıcıyı token'dan alır, istemcinin beyanına güvenmez.
    const paymentToken = `tok_card_${rawCard.slice(-4)}`;

    try {
      const orderId = await createOrder(
        { address: trimAddress(address), paymentToken, items: cart },
        idempotencyKeyRef.current,
      );
      setCreatedOrderId(orderId);
      clearCart();
    } catch (err) {
      console.error("Sipariş oluşturma hatası:", err);

      // 409: aynı anahtarla bir istek hâlâ işleniyor ([Idempotent] filtresi). Anahtar korunur ki
      // tekrar denemede ikinci sipariş oluşmasın. Diğer hatalarda bir sonraki deneme yeni bir istektir.
      if (getErrorStatus(err) !== 409) {
        idempotencyKeyRef.current = crypto.randomUUID();
      }

      setErrorMessage(getErrorMessage(err, "Sipariş oluşturulurken bir hata meydana geldi."));
      setIsSubmitting(false);
    }
  };

  // Sipariş verildi: SignalR'dan gelen canlı duruma göre sonuç ekranı
  if (createdOrderId) {
    const isLive = orderStatus && orderStatus.orderId === createdOrderId;
    const isCompleted = isLive && orderStatus.status === "Completed";
    const isCanceled = isLive && !isCompleted;

    return (
      <div className={styles.result}>
        <div
          className={`${styles.resultIcon} ${
            isCompleted ? styles.iconSuccess : isCanceled ? styles.iconDanger : styles.iconPending
          }`}
        >
          {isCompleted ? <CheckCircle size={32} /> : isCanceled ? <XCircle size={32} /> : <Loader2 size={32} />}
        </div>
        <h1 className={styles.resultTitle}>
          {isCompleted ? "Siparişiniz onaylandı!" : isCanceled ? "Sipariş iptal edildi" : "Siparişiniz alındı"}
        </h1>
        <p className={styles.resultText}>
          {isCompleted
            ? "Stok ayrıldı ve ödemeniz başarıyla onaylandı."
            : isCanceled
              ? orderStatus.message || "Siparişiniz tamamlanamadı."
              : "Stok ve ödeme adımları işleniyor, sonuç birkaç saniye içinde burada görünecek."}
        </p>
        <span className={styles.orderNo}>Sipariş No #{createdOrderId}</span>
        <div className={styles.resultActions}>
          <ButtonLink to={`/orders/${createdOrderId}`}>Sipariş detayına git</ButtonLink>
          <ButtonLink to="/" variant="secondary">
            Alışverişe devam et
          </ButtonLink>
        </div>
      </div>
    );
  }

  return (
    <>
      <Link to="/" className={styles.back}>
        <ArrowLeft size={16} /> Mağazaya dön
      </Link>
      <h1 className={styles.title}>Ödeme</h1>

      <form onSubmit={handleCreateOrder} className={styles.layout}>
        <div>
          <section className={styles.panel}>
            <h2 className={styles.panelTitle}>
              <CreditCard size={20} /> Kart bilgileri
            </h2>

            {errorMessage && <Alert className={styles.field}>{errorMessage}</Alert>}

            <FormField
              className={styles.field}
              label="Kart üzerindeki isim"
              type="text"
              required
              placeholder="Ad Soyad"
              value={cardHolder}
              onChange={(e) => setCardHolder(e.target.value)}
              autoComplete="cc-name"
            />

            <FormField
              className={styles.field}
              inputClassName={styles.cardNumber}
              label="Kart numarası"
              type="text"
              inputMode="numeric"
              required
              placeholder="0000 0000 0000 0000"
              value={cardNumber}
              onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
              autoComplete="cc-number"
              hint={
                <>
                  Test: sonu <strong>0000</strong> (limit yetersiz) veya <strong>9999</strong> (internete kapalı) olan
                  kart ödeme hatası senaryosunu çalıştırır.
                </>
              }
            />

            <div className={styles.row}>
              <FormField
                label="Son kullanma (AA/YY)"
                type="text"
                inputMode="numeric"
                required
                placeholder="AA/YY"
                value={expiry}
                onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                autoComplete="cc-exp"
              />
              <FormField
                label="CVV"
                type="password"
                inputMode="numeric"
                required
                maxLength={3}
                placeholder="•••"
                value={cvv}
                onChange={(e) => setCvv(e.target.value.replace(/\D/g, ""))}
                autoComplete="cc-csc"
              />
            </div>
          </section>

          <section className={styles.panel}>
            <h2 className={styles.panelTitle}>
              <MapPin size={20} /> Teslimat adresi
            </h2>
            <AddressForm value={address} onChange={setAddress} />
          </section>
        </div>

        <aside className={`${styles.panel} ${styles.summary}`}>
          <h2 className={styles.panelTitle}>
            <ShoppingBag size={20} /> Sipariş özeti
          </h2>
          {cart.map((item) => (
            <div key={item.id} className={styles.summaryItem}>
              {item.imageUrl ? (
                <img src={item.imageUrl} alt={item.name} className={styles.thumb} />
              ) : (
                <div className={styles.thumb} />
              )}
              <div className={styles.itemInfo}>
                <div className={styles.itemName}>{item.name}</div>
                <div className={styles.itemMeta}>
                  {item.quantity} × {formatPrice(item.price)}
                </div>
              </div>
              <span className={styles.itemTotal}>{formatPrice(item.price * item.quantity)}</span>
            </div>
          ))}

          <div className={styles.totalRow}>
            <span>Ödenecek tutar</span>
            <span className={styles.totalAmount}>{formatPrice(totalAmount)}</span>
          </div>

          <Button type="submit" size="lg" fullWidth disabled={isSubmitting || cart.length === 0}>
            <ShieldCheck size={18} />
            {isSubmitting ? "Sipariş işleniyor..." : "Ödemeyi tamamla"}
          </Button>
          <p className={styles.secure}>
            <ShieldCheck size={14} /> Demo ödeme: karttan yalnızca son 4 hane kullanılır
          </p>
        </aside>
      </form>
    </>
  );
}
