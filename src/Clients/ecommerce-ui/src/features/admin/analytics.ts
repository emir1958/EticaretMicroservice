import { parseApiDate } from "../../shared/utils/format";
import type { CustomerOrderStats, DailySales, Order } from "../orders/types";
import type { UserSummary } from "./api";

// Yönetim paneli raporları Order servisinde SQL ile hesaplanır (bkz. orders/api.ts). Burada yalnızca istemciye özgü,
// saf yardımcılar kalır: dönem sınırları (kullanıcının yerel saati), yüzde değişim ve sunucu verisini kullanıcılarla birleştirme.

const DAY_MS = 24 * 60 * 60 * 1000;

const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate());

export const itemCountOf = (order: Order) => order.orderItems.reduce((sum, item) => sum + item.quantity, 0);

export interface Period {
  from: Date;
  to: Date;
}

// Bugün dahil son N gün ve hemen öncesindeki aynı uzunluktaki dönem. Sınırlar yerel gece yarısıdır; sunucuya
// kesin birer an olarak gönderildiği için "bugün" kullanıcının gününe göre hesaplanır.
export function periodsFor(days: number, now = new Date()) {
  const to = new Date(startOfDay(now).getTime() + DAY_MS);
  const from = new Date(to.getTime() - days * DAY_MS);
  return {
    current: { from, to },
    previous: { from: new Date(from.getTime() - days * DAY_MS), to: from },
  };
}

export function percentChange(current: number, previous: number) {
  if (previous === 0) return current === 0 ? 0 : null;
  return (current - previous) / previous;
}

export interface DailyPoint {
  date: Date;
  revenue: number;
  orders: number;
}

export const toDailyPoints = (sales: DailySales[]): DailyPoint[] =>
  sales.map((day) => ({ date: parseApiDate(day.date), revenue: day.revenue, orders: day.orders }));

export interface CustomerStats {
  user: UserSummary;
  orderCount: number;
  completedCount: number;
  totalSpent: number;
  lastOrderAt: Date | null;
  city: string | null;
}

// Müşteri bilgisi Identity'de, sipariş özeti Order'da: ikisi burada birleşir. Hiç siparişi olmayan müşteri sıfırlarla görünür.
export function customerStats(users: UserSummary[], stats: CustomerOrderStats[]): CustomerStats[] {
  const byBuyer = new Map(stats.map((s) => [s.buyerId, s]));

  return users
    .filter((user) => user.role !== "Admin")
    .map((user) => {
      const own = byBuyer.get(user.id);
      return {
        user,
        orderCount: own?.orderCount ?? 0,
        completedCount: own?.completedCount ?? 0,
        totalSpent: own?.totalSpent ?? 0,
        lastOrderAt: own ? parseApiDate(own.lastOrderAt) : null,
        city: own?.city || null,
      };
    });
}

// Siparişte müşteri kimliği yerine ad gösterebilmek için
export const usersById = (users: UserSummary[]) => new Map(users.map((user) => [user.id, user]));
