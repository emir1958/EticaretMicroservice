import apiClient from "../../lib/apiClient";
import { env } from "../../config/env";

export interface UserSummary {
  id: string;
  username: string;
  email: string;
  role: "Admin" | "User";
  // Seed ile eklenen örnek müşteri (giriş yapamaz)
  isSample: boolean;
}

export async function getUsers() {
  const { data } = await apiClient.get<UserSummary[]>("/api/users");
  return data;
}

export interface PagedUsers {
  items: UserSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function getCustomers({ q, page = 1, pageSize = 20 }: { q?: string; page?: number; pageSize?: number } = {}) {
  const { data } = await apiClient.get<PagedUsers>("/api/users/customers", {
    params: { q: q || undefined, page, pageSize },
  });
  return data;
}

export type HealthStatus = "Healthy" | "Degraded" | "Unhealthy";

export interface ServiceHealth {
  name: string;
  status: HealthStatus;
  lastExecuted: string;
  entries: { name: string; status: HealthStatus; duration: string }[];
}

// Gateway'deki HealthChecks UI, servisleri 15 saniyede bir yoklar ve sonucu bu uçtan JSON olarak verir
export async function getServiceHealth() {
  const { data } = await apiClient.get<
    { name: string; status: HealthStatus; lastExecuted: string; entries: { name: string; status: HealthStatus; duration: string }[] }[]
  >("/healthchecks-api");
  return data.map<ServiceHealth>((service) => ({
    name: service.name,
    status: service.status,
    lastExecuted: service.lastExecuted,
    entries: service.entries ?? [],
  }));
}

export const HEALTH_DASHBOARD_URL = `${env.apiBaseUrl}/health-dashboard`;
