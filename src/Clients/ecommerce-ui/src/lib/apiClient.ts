import axios from "axios";
import { env } from "../config/env";
import { getAccessToken, getValidAccessToken, refreshAccessToken, setAccessToken } from "./session";

declare module "axios" {
  interface AxiosRequestConfig {
    // 401 sonrası token yenilenip istek bir kez tekrarlandı
    _retried?: boolean;
  }
}

const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
});

apiClient.interceptors.request.use(async (config) => {
  config.headers.set("X-Correlation-ID", crypto.randomUUID());

  // Süresi dolmak üzere olan access token istek atılmadan önce yenilenir
  const token = await getValidAccessToken();
  if (token) {
    config.headers.set("Authorization", `Bearer ${token}`);
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401) return Promise.reject(error);

    // Yalnızca oturumu olan kullanıcı etkilenir; misafirin aldığı 401 sadece o isteği başarısız kılar
    const hadSession = getAccessToken() !== null;
    if (!hadSession) return Promise.reject(error);

    // Token sunucuya göre geçersizse (örn. saat farkı) bir kez yenilenip istek tekrarlanır
    const config = error.config;
    if (config && !config._retried) {
      const freshToken = await refreshAccessToken();
      if (freshToken) {
        config._retried = true;
        config.headers.set("Authorization", `Bearer ${freshToken}`);
        return apiClient(config);
      }
    }

    // Yenilenemedi ya da yenilenen token da reddedildi: yerel oturum kapatılır
    setAccessToken(null);
    const path = window.location.pathname;
    if (!path.startsWith("/auth") && !path.startsWith("/admin/login")) {
      window.location.href = `/auth?redirect=${encodeURIComponent(path)}`;
    }
    return Promise.reject(error);
  },
);

// Controller'lar { message }, exception middleware'i ProblemDetails döner
interface ErrorBody {
  message?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function getErrorMessage(error: unknown, fallback = "Beklenmeyen bir hata oluştu."): string {
  if (!axios.isAxiosError<ErrorBody>(error)) return fallback;

  const data = error.response?.data;
  if (data?.errors) {
    return Object.values(data.errors).flat().join(" | ");
  }
  return data?.message || data?.detail || fallback;
}

export function getErrorStatus(error: unknown) {
  return axios.isAxiosError(error) ? error.response?.status : undefined;
}

export default apiClient;
