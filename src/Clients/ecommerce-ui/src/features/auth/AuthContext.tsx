import { createContext, useContext, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from "react";
import { parseJwt } from "../../lib/jwt";
import {
  getAccessToken,
  hasSessionHint,
  msUntilRefresh,
  refreshAccessToken,
  setAccessToken,
  subscribe,
} from "../../lib/session";
import * as authApi from "./api";

export type Role = "Admin" | "User";

export interface User {
  id: string;
  email: string;
  username: string;
  role: Role;
  isAdmin: boolean;
}

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  // Sayfa açılırken refresh cookie'si ile oturum geri yükleniyor: guard'lar bu sırada yönlendirme yapmamalı
  isLoading: boolean;
  login: (email: string, password: string) => Promise<User>;
  register: (username: string, email: string, password: string) => Promise<void>;
  logout: () => void;
}

// Identity'nin JwtSecurityTokenHandler'ı claim'leri bu kısa adlarla yazar
interface JwtPayload {
  sub: string;
  unique_name: string;
  email: string;
  role: Role;
  exp: number;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// Eski sürüm 7 günlük token'ı localStorage'da tutuyordu; artık kullanılmadığı için bırakılmaz
localStorage.removeItem("token");

function userFromToken(token: string | null): User | null {
  const payload = token ? parseJwt<JwtPayload>(token) : null;
  if (!payload || payload.exp * 1000 <= Date.now()) return null;

  return {
    id: payload.sub,
    email: payload.email,
    username: payload.unique_name,
    role: payload.role,
    isAdmin: payload.role === "Admin",
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // Access token bellekte (lib/session) durur; değişince arayüz yeniden çizilir
  const token = useSyncExternalStore(subscribe, getAccessToken);
  // Daha önce giriş yapılmışsa ilk yenileme bitene kadar "yükleniyor"; hiç giriş yapılmamışsa beklenecek bir şey yok
  const [isLoading, setIsLoading] = useState(hasSessionHint);

  const user = useMemo(() => userFromToken(token), [token]);

  // Sayfa (yeniden) açılınca bellekteki token yoktur: cookie'deki refresh token ile oturum geri yüklenir
  useEffect(() => {
    if (!hasSessionHint()) return;

    let ignore = false;
    refreshAccessToken().finally(() => {
      if (!ignore) setIsLoading(false);
    });
    return () => {
      ignore = true;
    };
  }, []);

  // Oturum, süresi dolmadan önce kendini yeniler (sekme açık kaldıkça ve uyku sonrası uyanınca da)
  useEffect(() => {
    if (!token) return;
    const delay = msUntilRefresh(token);
    if (delay === null) return;

    const timer = setTimeout(() => void refreshAccessToken(), delay);
    return () => clearTimeout(timer);
  }, [token]);

  const login = async (email: string, password: string) => {
    const receivedToken = await authApi.login(email, password);
    const loggedUser = userFromToken(receivedToken);
    if (!loggedUser) {
      throw new Error("Token alınamadı.");
    }

    setAccessToken(receivedToken);
    return loggedUser;
  };

  const logout = () => {
    // Sunucudaki refresh token iptal edilir; arayüz yanıtı beklemeden oturumu kapatır
    authApi.logout().catch((err: unknown) => console.warn("Çıkış isteği başarısız:", err));
    setAccessToken(null);
  };

  return (
    <AuthContext
      value={{
        user,
        isAuthenticated: user !== null,
        isAdmin: user?.isAdmin ?? false,
        isLoading,
        login,
        register: authApi.register,
        logout,
      }}
    >
      {children}
    </AuthContext>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth, AuthProvider içinde kullanılmalı.");
  return context;
}
