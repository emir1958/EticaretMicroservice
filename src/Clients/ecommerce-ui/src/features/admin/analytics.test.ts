import { describe, expect, it } from "vitest";
import type { Order, OrderItem } from "../orders/types";
import type { UserSummary } from "./api";
import { customerStats, dailySeries, inPeriod, percentChange, periodsFor, productSales, summarize } from "./analytics";

// Saat dilimi ve yaz saati uygulamasından bağımsız kalmak için siparişler UTC öğlenine, "şimdi" yerel öğleden sonraya sabitlenir
const NOW = new Date(2026, 10, 21, 15, 0);
const DAY_MS = 24 * 60 * 60 * 1000;

const item = (productId: string, quantity: number, price: number): OrderItem => ({
  productId,
  productName: `Ürün ${productId}`,
  price,
  quantity,
});

function order(overrides: Partial<Order> & { id: number }): Order {
  const orderItems = overrides.orderItems ?? [item("p1", 1, 100)];
  return {
    buyerId: "u1",
    orderStatus: "Tamamlandı",
    paymentStatus: "Alindi",
    refundedAt: null,
    createdDate: "2026-11-20T12:00:00Z",
    address: { contactName: "T", phone: "", city: "İstanbul", district: "K", street: "M", zipCode: "", line: "1" },
    orderItems,
    totalPrice: orderItems.reduce((sum, i) => sum + i.price * i.quantity, 0),
    history: [],
    ...overrides,
  };
}

describe("periodsFor", () => {
  it("bugün dahil son N günü ve hemen öncesindeki aynı uzunlukta dönemi verir", () => {
    const { current, previous } = periodsFor(7, NOW);

    expect(current.to.getTime() - current.from.getTime()).toBe(7 * DAY_MS);
    expect(previous.to.getTime()).toBe(current.from.getTime());
    expect(previous.to.getTime() - previous.from.getTime()).toBe(7 * DAY_MS);
    // Dönem, "şimdi"nin ait olduğu günün bitişinde (ertesi gün 00:00) sona erer
    expect(current.to).toEqual(new Date(2026, 10, 22));
  });
});

describe("inPeriod", () => {
  it("başlangıç dahil, bitiş hariç olacak şekilde dönemdeki siparişleri seçer", () => {
    const { current } = periodsFor(7, NOW);
    const inside = order({ id: 1, createdDate: "2026-11-18T12:00:00Z" });
    const before = order({ id: 2, createdDate: "2026-11-10T12:00:00Z" });
    const after = order({ id: 3, createdDate: "2026-11-30T12:00:00Z" });

    expect(inPeriod([inside, before, after], current).map((o) => o.id)).toEqual([1]);
  });
});

describe("summarize", () => {
  const orders = [
    order({ id: 1, orderItems: [item("a", 1, 400), item("b", 2, 300)] }), // 1000 TL, 3 adet
    order({ id: 2, orderItems: [item("a", 1, 500)] }), // 500 TL, 1 adet
    order({ id: 3, orderStatus: "IptalEdildi", paymentStatus: "IadeEdildi" }),
    order({ id: 4, orderStatus: "Beklemede", paymentStatus: "Alinmadi" }),
  ];

  it("ciro ve satılan adet yalnızca tamamlanan siparişlerden gelir", () => {
    const summary = summarize(orders);

    expect(summary.revenue).toBe(1500);
    expect(summary.itemsSold).toBe(4);
    expect(summary.averageOrderValue).toBe(750);
  });

  it("siparişleri duruma göre sayar", () => {
    const summary = summarize(orders);

    expect(summary).toMatchObject({ orderCount: 4, completedCount: 2, canceledCount: 1, pendingCount: 1 });
  });

  it("iptal oranı bekleyen siparişleri paydaya katmaz (1 iptal / 3 sonuçlanan)", () => {
    expect(summarize(orders).cancelRate).toBeCloseTo(1 / 3);
  });

  it("boş listede sıfır döner, NaN üretmez", () => {
    expect(summarize([])).toMatchObject({ revenue: 0, averageOrderValue: 0, cancelRate: 0, itemsSold: 0 });
  });
});

describe("percentChange", () => {
  it("önceki döneme göre oransal değişimi hesaplar", () => {
    expect(percentChange(150, 100)).toBe(0.5);
    expect(percentChange(50, 100)).toBe(-0.5);
  });

  it("önceki dönem 0 iken: ikisi de 0 ise 0, yeni değer varsa tanımsız (null)", () => {
    expect(percentChange(0, 0)).toBe(0);
    expect(percentChange(5, 0)).toBeNull();
  });
});

describe("dailySeries", () => {
  it("her gün için bir nokta üretir; sipariş sayısı hepsini, ciro yalnızca tamamlananları sayar", () => {
    const { current } = periodsFor(3, NOW);
    const series = dailySeries(
      [
        order({ id: 1, createdDate: "2026-11-20T12:00:00Z", orderItems: [item("a", 1, 300)] }),
        order({ id: 2, createdDate: "2026-11-20T12:00:00Z", orderStatus: "IptalEdildi", orderItems: [item("a", 1, 200)] }),
      ],
      current,
    );

    expect(series).toHaveLength(3);
    expect(series.map((p) => p.orders)).toEqual([0, 2, 0]);
    expect(series.map((p) => p.revenue)).toEqual([0, 300, 0]);
  });
});

describe("productSales", () => {
  it("iptal edilenleri saymaz, ürünleri toplayıp cirosuna göre azalan sıralar", () => {
    const sales = productSales([
      order({ id: 1, orderItems: [item("kucuk", 5, 10), item("buyuk", 1, 900)] }),
      order({ id: 2, orderItems: [item("kucuk", 5, 10)] }),
      order({ id: 3, orderStatus: "IptalEdildi", orderItems: [item("iptal", 9, 9999)] }),
    ]);

    expect(sales.map((s) => s.productId)).toEqual(["buyuk", "kucuk"]);
    expect(sales[1]).toMatchObject({ quantity: 10, revenue: 100 });
  });
});

describe("customerStats", () => {
  const user = (id: string, role: UserSummary["role"] = "User"): UserSummary => ({
    id,
    username: id,
    email: `${id}@test.local`,
    role,
    isSample: false,
  });

  it("yöneticiyi dışarıda bırakır, harcamayı yalnızca tamamlanan siparişlerden hesaplar", () => {
    const stats = customerStats(
      [user("admin", "Admin"), user("u1"), user("u2")],
      [
        order({ id: 1, buyerId: "u1", createdDate: "2026-11-10T12:00:00Z", orderItems: [item("a", 1, 400)] }),
        order({ id: 2, buyerId: "u1", createdDate: "2026-11-15T12:00:00Z", orderStatus: "IptalEdildi", orderItems: [item("a", 1, 100)] }),
      ],
    );

    expect(stats.map((s) => s.user.id)).toEqual(["u1", "u2"]);
    expect(stats[0]).toMatchObject({ orderCount: 2, completedCount: 1, totalSpent: 400, city: "İstanbul" });
    expect(stats[0].lastOrderAt?.toISOString()).toBe("2026-11-15T12:00:00.000Z");
  });

  it("hiç siparişi olmayan müşteri için sıfır ve boş değerler döner", () => {
    const [stat] = customerStats([user("u2")], []);

    expect(stat).toMatchObject({ orderCount: 0, totalSpent: 0, lastOrderAt: null, city: null });
  });
});
