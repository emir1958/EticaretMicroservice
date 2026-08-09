import { jwtDecode } from "jwt-decode";

export const login = (token) => {
  localStorage.setItem("token", token);
};

export const logout = () => {
  localStorage.removeItem("token");
};

export const getCurrentToken = () => {
  return localStorage.getItem("token");
};

// 🟢 JWT içindeki Claim'leri çözen fonksiyon
export const getCurrentUser = () => {
  const token = localStorage.getItem("token");
  if (!token) return null;

  try {
    const decoded = jwtDecode(token);

    // .NET ClaimTypes.Role, ClaimTypes.Email vb. standart URI formatında gelir
    const role =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      decoded["role"] ||
      "User";

    const email =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress"] ||
      decoded["email"];

    const username =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/name"] ||
      decoded["name"];

    return { username, email, role };
  } catch (error) {
    console.error("Token decode hatası:", error);
    return null;
  }
};