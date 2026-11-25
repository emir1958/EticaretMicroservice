// Yalnızca testlerde kullanılır (üretim kodundan import edilmez)

function toBase64Url(text: string) {
  let binary = "";
  new TextEncoder().encode(text).forEach((byte) => (binary += String.fromCharCode(byte)));
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

// İmzası geçersiz ama payload'ı okunabilir sahte JWT
export const fakeJwt = (payload: object) => `header.${toBase64Url(JSON.stringify(payload))}.signature`;
