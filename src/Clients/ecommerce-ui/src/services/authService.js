import { jwtDecode } from "jwt-decode";
import api from "./api"; // YARP Gateway kullanan ortak api servisimiz

// 🟢 Kullanıcı Kayıt Olma Servisi
export const register = async (username, email, password) => {
  const response = await api.post("/Auth/register", { username, email, password });
  return response.data;
};

// 🟢 Kullanıcı Giriş Yapma Servisi
export const login = async (email, password) => {
  const response = await api.post("/Auth/login", { email, password });
  if (response.data.token) {
    localStorage.setItem("token", response.data.token);
  }
  return response.data;
};

// 🟢 Çıkış Yapma
export const logout = () => {
  localStorage.removeItem("token");
};

// 🟢 Aktif Token'ı Alma
export const getCurrentToken = () => {
  return localStorage.getItem("token");
};

// 🟢 Token'ı Çözüp Kullanıcı Bilgilerini ve Rolünü Alma
export const getCurrentUser = () => {
  const token = localStorage.getItem("token");
  if (!token) return null;

  try {
    const decoded = jwtDecode(token);

    // 🔍 Hata ayıklama için token içeriğini konsola basalım
    console.log("Decoded Token Claims:", decoded);

    // .NET'in farklı JWT claim formatlarını kontrol et
    const role =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role"] ||
      decoded["role"] ||
      decoded["Role"] ||
      "User";

    const email =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress"] ||
      decoded["email"] ||
      decoded["sub"];

    const username =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/name"] ||
      decoded["name"];

    return { username, email, role };
  } catch (error) {
    console.error("Token decode hatası:", error);
    return null;
  }
};