import apiClient from "../../lib/apiClient";

export async function login(email: string, password: string) {
  const { data } = await apiClient.post<{ token: string }>("/api/auth/login", {
    email: email.trim(),
    password,
  });
  return data.token;
}

export async function register(username: string, email: string, password: string) {
  await apiClient.post("/api/auth/register", {
    username: username.trim(),
    email: email.trim(),
    password,
  });
}
