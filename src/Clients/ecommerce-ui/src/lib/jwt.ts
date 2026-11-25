// JWT'nin payload'ını okur (imzayı doğrulamaz: doğrulama sunucunun işidir, burada yalnızca arayüz kararları için okunur)
export function parseJwt<T = Record<string, unknown>>(token: string): T | null {
  try {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
  } catch {
    return null;
  }
}

// Token'ın bitiş zamanı (ms, epoch); okunamıyorsa null
export function getTokenExpiry(token: string): number | null {
  const payload = parseJwt<{ exp?: number }>(token);
  return typeof payload?.exp === "number" ? payload.exp * 1000 : null;
}
