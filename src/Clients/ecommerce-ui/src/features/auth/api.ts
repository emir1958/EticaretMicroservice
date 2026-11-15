import apiClient from "../../lib/apiClient";

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
