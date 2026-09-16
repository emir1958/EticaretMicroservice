import React, { createContext, useContext, useState, useEffect } from "react";
import api from "../services/api";

const AuthContext = createContext();

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

export const AuthProvider = ({ children }) => {
  const [token, setToken] = useState(localStorage.getItem("token") || null);
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (token) {
      const decoded = parseJwt(token);
      if (decoded && decoded.exp * 1000 > Date.now()) {
        setUser({
          id:
            decoded.sub ||
            decoded[
              "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            ] ||
            decoded.nameid,
          email:
            decoded.email ||
            decoded[
              "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
            ],
          username:
            decoded.unique_name ||
            decoded[
              "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
            ] ||
            "Kullanıcı",
          role:
            decoded.role ||
            decoded[
              "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            ] ||
            "User",
        });
      } else {
        logout();
      }
    } else {
      setUser(null);
    }
    setLoading(false);
  }, [token]);

  const login = async (email, password) => {
    // Gateway -> /api/auth/login rotasına gider
    const response = await api.post("/api/auth/login", {
      email: email.trim(),
      password,
    });

    // 🟢 Backend { Token: "..." } formatında döner
    const receivedToken = response.data.Token || response.data.token;
    if (!receivedToken) {
      throw new Error("Token alınamadı.");
    }

    localStorage.setItem("token", receivedToken);
    setToken(receivedToken);
    return response.data;
  };

  const register = async (username, email, password) => {
    // 🟢 Backend RegisterDto { Username, Email, Password } bekler
    const response = await api.post("/api/auth/register", {
      username: username.trim(),
      email: email.trim(),
      password,
    });
    return response.data;
  };

  const logout = () => {
    localStorage.removeItem("token");
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        user,
        isAuthenticated: !!token,
        login,
        register,
        logout,
        loading,
      }}
    >
      {!loading && children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
