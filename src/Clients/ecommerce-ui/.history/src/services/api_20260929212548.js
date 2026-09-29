import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5000", // Gateway Adresimiz
});

// Request Interceptor: Her istek öncesi Token ekler
api.interceptors.request.use(
  (config) => {

// 🟢 EKLENDİ: Her isteğe frontend'den benzersiz bir UUID basılır
    config.headers["X-Correlation-ID"] = crypto.randomUUID();

    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// 🟢 Response Interceptor: 401 durumunda token'ı temizleyip logine yönlendirir
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("token");
      if (!window.location.pathname.includes("/auth") && !window.location.pathname.includes("/admin/login")) {
        window.location.href = `/auth?redirect=${encodeURIComponent(window.location.pathname)}`;
      }
    }
    return Promise.reject(error);
  }
);

export default api;