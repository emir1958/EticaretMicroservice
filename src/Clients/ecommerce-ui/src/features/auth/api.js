import apiClient from "../../lib/apiClient";

// Identity.Api ile konuşan tek yer. Token'ın saklanması ve çözülmesi AuthContext'in işi.

export async function login(email, password) {
  const { data } = await apiClient.post("/api/auth/login", { email: email.trim(), password });
  return data.token;
}

export async function register(username, email, password) {
  const { data } = await apiClient.post("/api/auth/register", {
    username: username.trim(),
    email: email.trim(),
    password,
  });
  return data;
}
