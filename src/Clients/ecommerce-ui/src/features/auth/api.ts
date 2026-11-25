import apiClient from "../../lib/apiClient";

// Refresh token cookie'si yalnızca /api/auth altında geçerli ve çapraz-origin istekte tarayıcı onu
// ancak withCredentials ile kabul eder/gönderir; bu yüzden yalnızca bu uçlarda açılır.
const withCookie = { withCredentials: true };

export async function login(email: string, password: string) {
  const { data } = await apiClient.post<{ token: string }>(
    "/api/auth/login",
    { email: email.trim(), password },
    withCookie,
  );
  return data.token;
}

export async function register(username: string, email: string, password: string) {
  await apiClient.post("/api/auth/register", {
    username: username.trim(),
    email: email.trim(),
    password,
  });
}

// Sunucuda refresh token'ı iptal eder ve cookie'yi siler
export async function logout() {
  await apiClient.post("/api/auth/logout", null, withCookie);
}
