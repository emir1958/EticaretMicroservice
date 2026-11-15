const DEV_GATEWAY_URL = "http://localhost:5000";

const gatewayUrl = (import.meta.env.VITE_API_URL || DEV_GATEWAY_URL).replace(/\/$/, "");

export const env = {
  apiBaseUrl: gatewayUrl,
  orderHubUrl: `${gatewayUrl}/orderhub`,
};
