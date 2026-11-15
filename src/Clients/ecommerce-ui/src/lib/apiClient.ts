import axios from "axios";
import { env } from "../config/env";

const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
});

apiClient.interceptors.request.use((config) => {
  config.headers.set("X-Correlation-ID", crypto.randomUUID());

  const token = localStorage.getItem("token");
  if (token) {
    config.headers.set("Authorization", `Bearer ${token}`);
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    // Yalnızca oturumu düşen kullanıcı girişe yönlendirilir; misafirin aldığı 401 sadece o isteği başarısız kılar
    if (axios.isAxiosError(error) && error.response?.status === 401 && localStorage.getItem("token")) {
      localStorage.removeItem("token");
      const path = window.location.pathname;
      if (!path.startsWith("/auth") && !path.startsWith("/admin/login")) {
        window.location.href = `/auth?redirect=${encodeURIComponent(path)}`;
      }
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
