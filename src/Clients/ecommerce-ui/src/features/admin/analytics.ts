import { parseApiDate } from "../../shared/utils/format";
import type { Order } from "../orders/types";
import type { UserSummary } from "./api";

// Yönetim panelindeki raporlar. Order servisinin rapor ucu olmadığı için sipariş listesinden tarayıcıda hesaplanır;
// sipariş sayısı büyüdüğünde bu hesap backend'e (ör. GET /api/orders/stats) taşınmalıdır.

const DAY_MS = 24 * 60 * 60 * 1000;

const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate());

export const orderDate = (order: Order) => parseApiDate(order.createdDate);

// Ciro yalnızca tamamlanan siparişlerden gelir: iptal edilenlerin ödemesi ya alınmadı ya iade edildi
export const isCompleted = (order: Order) => order.orderStatus === "Tamamlandı";

export const itemCountOf = (order: Order) => order.orderItems.reduce((sum, item) => sum + item.quantity, 0);

export interface Period {
  from: Date;
  to: Date;
}

// Bugün dahil son N gün ve hemen öncesindeki aynı uzunluktaki dönem
export function periodsFor(days: number, now = new Date()) {
  const to = new Date(startOfDay(now).getTime() + DAY_MS);
  const from = new Date(to.getTime() - days * DAY_MS);
  return {
    current: { from, to },
    previous: { from: new Date(from.getTime() - days * DAY_MS), to: from },
  };
}

export const inPeriod = (orders: Order[], { from, to }: Period) =>
  orders.filter((order) => {
    const date = orderDate(order);
    return date >= from && date < to;
  });

export function summarize(orders: Order[]) {
  const completed = orders.filter(isCompleted);
  const revenue = completed.reduce((sum, order) => sum + order.totalPrice, 0);
  const canceled = orders.filter((order) => order.orderStatus === "IptalEdildi").length;
  const decided = completed.length + canceled;

  return {
    revenue,
    orderCount: orders.length,
    completedCount: completed.length,
    pendingCount: orders.filter((order) => order.orderStatus === "Beklemede").length,
    canceledCount: canceled,
    averageOrderValue: completed.length > 0 ? revenue / completed.length : 0,
    // Sonuçlanmamış (bekleyen) siparişler oranı bozmasın
    cancelRate: decided > 0 ? canceled / decided : 0,
    itemsSold: completed.reduce((sum, order) => sum + itemCountOf(order), 0),
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

export function dailySeries(orders: Order[], { from, to }: Period): DailyPoint[] {
  const points: DailyPoint[] = [];
  for (let time = from.getTime(); time < to.getTime(); time += DAY_MS) {
    points.push({ date: new Date(time), revenue: 0, orders: 0 });
  }

  for (const order of inPeriod(orders, { from, to })) {
    const index = Math.floor((startOfDay(orderDate(order)).getTime() - from.getTime()) / DAY_MS);
    const point = points[index];
    if (!point) continue;
    point.orders += 1;
    if (isCompleted(order)) point.revenue += order.totalPrice;
  }
  return points;
}

export interface ProductSales {
  productId: string;
  name: string;
  quantity: number;
  revenue: number;
}

export function productSales(orders: Order[]) {
  const byProduct = new Map<string, ProductSales>();
  for (const order of orders.filter(isCompleted)) {
    for (const item of order.orderItems) {
      const entry = byProduct.get(item.productId) ?? { productId: item.productId, name: item.productName, quantity: 0, revenue: 0 };
      entry.quantity += item.quantity;
      entry.revenue += item.price * item.quantity;
      byProduct.set(item.productId, entry);
    }
  }
  return [...byProduct.values()].sort((a, b) => b.revenue - a.revenue);
}

export interface CustomerStats {
  user: UserSummary;
  orderCount: number;
  completedCount: number;
  totalSpent: number;
  lastOrderAt: Date | null;
  city: string | null;
}

export function customerStats(users: UserSummary[], orders: Order[]): CustomerStats[] {
  const ordersByBuyer = new Map<string, Order[]>();
  for (const order of orders) {
    ordersByBuyer.set(order.buyerId, [...(ordersByBuyer.get(order.buyerId) ?? []), order]);
  }

  return users
    .filter((user) => user.role !== "Admin")
    .map((user) => {
      const own = (ordersByBuyer.get(user.id) ?? []).sort((a, b) => orderDate(b).getTime() - orderDate(a).getTime());
      const completed = own.filter(isCompleted);
      return {
        user,
        orderCount: own.length,
        completedCount: completed.length,
        totalSpent: completed.reduce((sum, order) => sum + order.totalPrice, 0),
        lastOrderAt: own[0] ? orderDate(own[0]) : null,
        city: own[0]?.address?.city || null,
      };
    });
}

// Sipariş listesinde müşteri kimliği yerine ad gösterebilmek için
export const usersById = (users: UserSummary[]) => new Map(users.map((user) => [user.id, user]));
