import { createContext, useContext, useState, useMemo } from "react";
import * as authApi from "./api";

const AuthContext = createContext();

const TOKEN_KEY = "token";

// ASP.NET Core Identity, claim'leri kısa adla (sub, email...) ya da uzun URI ile yazabiliyor.
const CLAIMS = {
  role: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
  id: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
  email: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
  name: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
};

const parseJwt = (token) => {
  try {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join(""),
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
};

// Token'dan kullanıcıyı türetir. Geçersiz veya süresi dolmuşsa null döner.
// Not: Bu sadece UI içindir; imza doğrulaması yapmaz. Asıl yetki kontrolü backend'de.
function userFromToken(token) {
  const decoded = token ? parseJwt(token) : null;
  if (!decoded || !(decoded.exp * 1000 > Date.now())) return null;

  const role = decoded.role || decoded[CLAIMS.role] || "User";
  return {
    id: decoded.sub || decoded[CLAIMS.id] || decoded.nameid,
    email: decoded.email || decoded[CLAIMS.email],
    username: decoded.unique_name || decoded[CLAIMS.name] || "Kullanıcı",
    role,
    isAdmin: role.toLowerCase() === "admin",
  };
}

// Uygulama açılışında bir kez çalışır: süresi dolmuş token'ı localStorage'dan temizler.
function readStoredToken() {
  const stored = localStorage.getItem(TOKEN_KEY);
  if (stored && !userFromToken(stored)) {
    localStorage.removeItem(TOKEN_KEY);
    return null;
  }
  return stored;
}

export const AuthProvider = ({ children }) => {
  // Lazy initializer: fonksiyon referansı verilir, React onu yalnızca ilk render'da çağırır.
  const [token, setToken] = useState(readStoredToken);

  // user ayrı bir state değil, token'dan türetilen değer. useMemo, token değişmedikçe
  // aynı nesne referansını korur (context tüketicileri gereksiz yere yeniden render olmaz).
  const user = useMemo(() => userFromToken(token), [token]);

  const login = async (email, password) => {
    const receivedToken = await authApi.login(email, password);
    const loggedUser = userFromToken(receivedToken);
    if (!loggedUser) {
      throw new Error("Token alınamadı.");
    }

    localStorage.setItem(TOKEN_KEY, receivedToken);
    setToken(receivedToken);
    return loggedUser;
  };

  const register = (username, email, password) => authApi.register(username, email, password);

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    setToken(null);
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        user,
        isAuthenticated: !!user,
        isAdmin: user?.isAdmin || false,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

// Hook'u provider ile aynı dosyada tutmak bilinçli bir tercih (context + hook birlikte okunur).
// Bedeli: bu dosya değişince Fast Refresh state'i koruyamaz, tam yeniden yükleme yapar.
// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => useContext(AuthContext);
