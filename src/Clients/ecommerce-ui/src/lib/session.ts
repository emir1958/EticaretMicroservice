import axios from "axios";
import { env } from "../config/env";
import { getTokenExpiry } from "./jwt";

// Access token (15 dk) yalnızca bellekte durur: localStorage gibi XSS'in okuyabileceği bir yerde tutulmaz.
// Oturumu sürdüren refresh token'ı sunucu HttpOnly cookie olarak verir; JavaScript ona hiç erişemez.
let accessToken: string | null = null;
const listeners = new Set<() => void>();

// Gizli bilgi değil, yalnızca "bu tarayıcıda oturum açılmış olabilir" ipucu: hiç giriş yapmamış ziyaretçide
// her sayfa açılışında boşuna (ve konsola 401 düşürerek) refresh isteği atılmaz.
const SESSION_HINT_KEY = "auth_session";

// Süresi dolmaya bu kadar kala istekten önce yenilenir
const REFRESH_SKEW_MS = 30_000;

export const getAccessToken = () => accessToken;

export function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function setAccessToken(token: string | null) {
  accessToken = token;
  if (token) localStorage.setItem(SESSION_HINT_KEY, "1");
  else localStorage.removeItem(SESSION_HINT_KEY);
  listeners.forEach((listener) => listener());
}

export const hasSessionHint = () => localStorage.getItem(SESSION_HINT_KEY) !== null;

// Interceptor'sız ayrı istemci: refresh isteğinin kendisi 401 alırsa tekrar refresh denemesi (sonsuz döngü) olmaz
const refreshClient = axios.create({ baseURL: env.apiBaseUrl, withCredentials: true });

let inFlight: Promise<string | null> | null = null;

// Cookie'deki refresh token ile yeni access token alır. Aynı anda gelen çağrılar tek isteğe birleşir
// (birden çok istek ya da StrictMode'un çift effect'i gereksiz yere birden fazla refresh isteği atmasın).
export function refreshAccessToken(): Promise<string | null> {
  inFlight ??= refreshClient
    .post<{ token: string }>("/api/auth/refresh")
    .then(({ data }) => {
      setAccessToken(data.token);
      return data.token;
    })
    .catch((error: unknown) => {
      // 401: oturum gerçekten bitti. Ağ kesintisi ya da 5xx: oturum düşürülmez, mevcut token ile devam edilir
      if (axios.isAxiosError(error) && error.response?.status === 401) {
        setAccessToken(null);
        return null;
      }
      return accessToken;
    })
    .finally(() => {
      inFlight = null;
    });

  return inFlight;
}

// İstekten önce çağrılır: token süresi dolmak üzereyse yenilenir. Oturum yoksa (misafir) hiçbir şey yapılmaz.
export async function getValidAccessToken(): Promise<string | null> {
  const token = accessToken;
  if (!token) return null;

  const expiresAt = getTokenExpiry(token);
  if (expiresAt === null || expiresAt - Date.now() > REFRESH_SKEW_MS) return token;

  return refreshAccessToken();
}

// Token'ın süresi dolmadan yenilenmesi gereken an (ms cinsinden bekleme); okunamazsa null
export function msUntilRefresh(token: string, now = Date.now()): number | null {
  const expiresAt = getTokenExpiry(token);
  return expiresAt === null ? null : Math.max(expiresAt - now - REFRESH_SKEW_MS, 0);
}
