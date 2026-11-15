import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
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

const TOKEN_KEY = "token";

function parseJwt(token: string): JwtPayload | null {
  try {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
  } catch {
    return null;
  }
}

function userFromToken(token: string | null): User | null {
  const payload = token ? parseJwt(token) : null;
  if (!payload || payload.exp * 1000 <= Date.now()) return null;

  return {
    id: payload.sub,
    email: payload.email,
    username: payload.unique_name,
    role: payload.role,
    isAdmin: payload.role === "Admin",
  };
}

function readStoredToken() {
  const stored = localStorage.getItem(TOKEN_KEY);
  if (stored && !userFromToken(stored)) {
    localStorage.removeItem(TOKEN_KEY);
    return null;
  }
  return stored;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState(readStoredToken);

  const user = useMemo(() => userFromToken(token), [token]);

  const login = async (email: string, password: string) => {
    const receivedToken = await authApi.login(email, password);
    const loggedUser = userFromToken(receivedToken);
    if (!loggedUser) {
      throw new Error("Token alınamadı.");
    }

    localStorage.setItem(TOKEN_KEY, receivedToken);
    setToken(receivedToken);
    return loggedUser;
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    setToken(null);
  };

  return (
    <AuthContext
      value={{
        user,
        isAuthenticated: user !== null,
        isAdmin: user?.isAdmin ?? false,
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
