import { useState, useRef } from "react";
import { Link } from "react-router-dom";
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Loader2,
  CreditCard,
  ShieldCheck,
  AlertTriangle,
  ShoppingBag,
  MapPin,
} from "lucide-react";
import { useCart } from "../../context/CartContext";
import { useAuth } from "../../context/AuthContext";
import { useOrderHub } from "../../hooks/useOrderHub";
import api from "../../services/api";
import Button from "../../components/ui/Button";
import EmptyState from "../../components/ui/EmptyState";
import { formatPrice } from "../../utils/orderStatus";
import styles from "./Checkout.module.css";

// Adres formu henüz yok: her sipariş bu demo adresle gönderilir. Ekranda da aynısı gösterilir.
const DEMO_ADDRESS = {
  city: "İstanbul",
  district: "Kadıköy",
  street: "Moda Caddesi",
  zipCode: "34710",
  line: "No:12 Daire:4",
};

export default function Checkout() {
  const { cart, totalAmount, clearCart } = useCart();
  // 🟢 1. isAdmin bilgisini AuthContext'ten alıyoruz
  const { user, isAdmin } = useAuth();
  const { orderStatus } = useOrderHub();

  // Kart Form State'leri
  const [cardHolder, setCardHolder] = useState("");
  const [cardNumber, setCardNumber] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvv, setCvv] = useState("");

  const idempotencyKeyRef = useRef(crypto.randomUUID());
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdOrderId, setCreatedOrderId] = useState(null);
  const [errorMessage, setErrorMessage] = useState(null);

  const formatCardNumber = (val) => {
    const raw = val.replace(/\D/g, "").slice(0, 16);
    return raw.match(/.{1,4}/g)?.join(" ") || raw;
  };

  const formatExpiry = (val) => {
    let raw = val.replace(/\D/g, "").slice(0, 4);
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
            <Button to="/admin/orders">Yönetici Paneline Git</Button>
            <Button to="/" variant="secondary">
              Mağazaya Dön
            </Button>
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
        action={<Button to="/">Alışverişe Başla</Button>}
      />
    );
  }

  const handleCreateOrder = async (e) => {
    e.preventDefault();
    if (cart.length === 0) return;

    const rawCard = cardNumber.replace(/\s/g, "");
    if (rawCard.length !== 16 || cvv.length < 3 || expiry.length !== 5) {
      setErrorMessage("Lütfen kart bilgilerinizi eksiksiz ve doğru girin.");
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    // 🟢 DÜZELTİLDİ: cleanCard yerine tanımladığınız rawCard kullanıldı
    const last4 = rawCard.slice(-4);
    const paymentToken = `tok_card_${last4}`;

    const payload = {
      buyerId: user?.id,
      address: DEMO_ADDRESS,
      payment: {
        paymentToken: paymentToken,
      },
      orderItems: cart.map((item) => ({
        productId: item.id,
        productName: item.name,
        price: item.price,
        quantity: item.quantity,
      })),
    };

    try {
      const response = await api.post("/api/orders", payload, {
        headers: {
          "Idempotency-Key": idempotencyKeyRef.current,
        },
      });
      setCreatedOrderId(response.data?.orderId || response.data);
      clearCart();
    } catch (err) {
      console.error("Sipariş oluşturma hatası:", err);

      // 🟢 Backend'deki [Idempotent] filter 409 Conflict dönerse:
      if (err.response?.status === 409) {
        setErrorMessage(
          "Siparişiniz şu anda işleniyor, lütfen art arda tıklamayınız.",
        );
        setIsSubmitting(false);
        return;
      }
      // Kullanıcı hatalı bir işlem yaptıysa (400 Bad Request vb.) bir sonraki deneme için yeni anahtar üret
      idempotencyKeyRef.current = crypto.randomUUID();

      const backendErrors = err.response?.data?.errors;
      let detailMsg = err.response?.data?.detail || err.response?.data?.message;

      if (backendErrors) {
        detailMsg = Object.values(backendErrors).flat().join(" | ");
      }

      setErrorMessage(
        detailMsg || "Sipariş oluşturulurken bir hata meydana geldi.",
      );
      setIsSubmitting(false);
    }
  };

  // Sipariş verildi: SignalR'dan gelen canlı duruma göre sonuç ekranı
  if (createdOrderId) {
    const isLive = orderStatus && orderStatus.orderId === createdOrderId;
    const isCompleted = isLive && (orderStatus.status === "Completed" || orderStatus.status === "Tamamlandı");
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
          <Button to={`/orders/${createdOrderId}`}>Sipariş detayına git</Button>
          <Button to="/" variant="secondary">
            Alışverişe devam et
          </Button>
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

            {errorMessage && (
              <div className={styles.error}>
                <AlertTriangle size={18} /> {errorMessage}
              </div>
            )}

            <div className={styles.field}>
              <label className={styles.label}>Kart üzerindeki isim</label>
              <input
                className={styles.input}
                type="text"
                required
                placeholder="Ad Soyad"
                value={cardHolder}
                onChange={(e) => setCardHolder(e.target.value)}
                autoComplete="cc-name"
              />
            </div>

            <div className={styles.field}>
              <label className={styles.label}>Kart numarası</label>
              <input
                className={`${styles.input} ${styles.cardNumber}`}
                type="text"
                inputMode="numeric"
                required
                placeholder="0000 0000 0000 0000"
                value={cardNumber}
                onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
                autoComplete="cc-number"
              />
              <small className={styles.hint}>
                Test: sonu <strong>0000</strong> (limit yetersiz) veya <strong>9999</strong> (internete kapalı) olan kart
                ödeme hatası senaryosunu çalıştırır.
              </small>
            </div>

            <div className={styles.row}>
              <div>
                <label className={styles.label}>Son kullanma (AA/YY)</label>
                <input
                  className={styles.input}
                  type="text"
                  inputMode="numeric"
                  required
                  placeholder="AA/YY"
                  value={expiry}
                  onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                  autoComplete="cc-exp"
                />
              </div>
              <div>
                <label className={styles.label}>CVV</label>
                <input
                  className={styles.input}
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
            </div>
          </section>

          <section className={styles.panel}>
            <h2 className={styles.panelTitle}>
              <MapPin size={20} /> Teslimat adresi
            </h2>
            <p className={styles.address}>
              {DEMO_ADDRESS.street} {DEMO_ADDRESS.line}
              <br />
              {DEMO_ADDRESS.district} / {DEMO_ADDRESS.city} {DEMO_ADDRESS.zipCode}
            </p>
            <span className={styles.demoTag}>Demo adres</span>
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
