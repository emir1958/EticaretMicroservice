import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from "react";
import { AlertTriangle, CheckCircle, X } from "lucide-react";
import styles from "./Toast.module.css";

type ToastTone = "success" | "danger";

interface ToastAction {
  label: string;
  onClick: () => void;
}

interface ToastItem {
  id: number;
  tone: ToastTone;
  message: string;
  action?: ToastAction;
}

interface ToastApi {
  success: (message: string, action?: ToastAction) => void;
  error: (message: string) => void;
}

const ToastContext = createContext<ToastApi | null>(null);

const VISIBLE_MS = 4500;
const MAX_VISIBLE = 3;

// Kısa ömürlü geri bildirim ("sepete eklendi", "ürün kaydedildi"). Kullanıcının düzeltmesi gereken
// hatalar burada değil, ilgili formun yanında Alert ile gösterilir: toast kaybolur, hata kaybolmamalı.
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(0);

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const show = useCallback(
    (tone: ToastTone, message: string, action?: ToastAction) => {
      const id = nextId.current++;
      setToasts((current) => [...current.slice(1 - MAX_VISIBLE), { id, tone, message, action }]);
      window.setTimeout(() => dismiss(id), VISIBLE_MS);
    },
    [dismiss],
  );

  const api = useMemo<ToastApi>(
    () => ({
      success: (message, action) => show("success", message, action),
      error: (message) => show("danger", message),
    }),
    [show],
  );

  return (
    <ToastContext value={api}>
      {children}
      <div className={styles.viewport} role="region" aria-label="Bildirimler">
        {toasts.map((toast) => (
          <div key={toast.id} className={styles.toast} role={toast.tone === "danger" ? "alert" : "status"}>
            {toast.tone === "danger" ? (
              <AlertTriangle size={18} className={styles.dangerIcon} aria-hidden="true" />
            ) : (
              <CheckCircle size={18} className={styles.successIcon} aria-hidden="true" />
            )}
            <span className={styles.message}>{toast.message}</span>
            {toast.action && (
              <button
                type="button"
                className={styles.action}
                onClick={() => {
                  toast.action?.onClick();
                  dismiss(toast.id);
                }}
              >
                {toast.action.label}
              </button>
            )}
            <button type="button" className={styles.close} onClick={() => dismiss(toast.id)} aria-label="Bildirimi kapat">
              <X size={16} aria-hidden="true" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) throw new Error("useToast, ToastProvider içinde kullanılmalı.");
  return context;
}
