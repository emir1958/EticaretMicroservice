import axios from "axios";
import { env } from "../config/env";

// Uygulamanın tek HTTP istemcisi. Bileşenler bunu doğrudan kullanmaz;
// her feature kendi api.js dosyasında endpoint fonksiyonlarını tanımlar.
const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
});

// Request Interceptor: her isteğe correlation id ve (varsa) token eklenir
apiClient.interceptors.request.use((config) => {
  // Backend loglarında bu isteğin izini sürmek için benzersiz id
  config.headers["X-Correlation-ID"] = crypto.randomUUID();

  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response Interceptor: 401'de token temizlenir ve giriş sayfasına yönlendirilir.
// Bu kod React dışında çalıştığı için router state yerine ?redirect= kullanır (bkz. AuthPage).
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("token");
      const path = window.location.pathname;
      if (!path.startsWith("/auth") && !path.startsWith("/admin/login")) {
        window.location.href = `/auth?redirect=${encodeURIComponent(path)}`;
      }
    }
    return Promise.reject(error);
  },
);

// Backend'in iki hata şekli var:
//  - Controller'lar:           { message }
//  - Exception middleware'i:   ProblemDetails { title, detail, errors? }  (errors: alan → mesaj[])
// Bileşenler hata cevabını kendileri ayrıştırmaz, bu fonksiyonu kullanır.
export function getErrorMessage(error, fallback = "Beklenmeyen bir hata oluştu.") {
  const data = error?.response?.data;
  if (data?.errors && typeof data.errors === "object") {
    return Object.values(data.errors).flat().join(" | ");
  }
  return data?.message || data?.detail || fallback;
}

export default apiClient;
