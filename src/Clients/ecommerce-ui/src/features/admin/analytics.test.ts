import { describe, expect, it } from "vitest";
import type { CustomerOrderStats } from "../orders/types";
import type { UserSummary } from "./api";
import { customerStats, percentChange, periodsFor, toDailyPoints } from "./analytics";

const DAY_MS = 24 * 60 * 60 * 1000;

// Rapor hesapları Order servisinde SQL ile yapılır (IntegrationTests/OrderReportTests.cs); burada istemci tarafı yardımcılar test edilir

describe("periodsFor", () => {
  const now = new Date(2026, 10, 21, 15, 0);

  it("bugün dahil son N günü ve hemen öncesindeki aynı uzunlukta dönemi verir", () => {
    const { current, previous } = periodsFor(7, now);

    expect(current.to.getTime() - current.from.getTime()).toBe(7 * DAY_MS);
    expect(previous.to.getTime()).toBe(current.from.getTime());
    expect(previous.to.getTime() - previous.from.getTime()).toBe(7 * DAY_MS);
  });

  it("dönem, 'şimdi'nin ait olduğu günün bitişinde (ertesi gün yerel 00:00) sona erer", () => {
    expect(periodsFor(7, now).current.to).toEqual(new Date(2026, 10, 22));
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

describe("toDailyPoints", () => {
  it("sunucudaki UTC tarihlerini Date'e çevirir, değerleri korur", () => {
    const [point] = toDailyPoints([{ date: "2026-11-20T21:00:00Z", revenue: 300, orders: 2 }]);

    expect(point.date.toISOString()).toBe("2026-11-20T21:00:00.000Z");
    expect(point).toMatchObject({ revenue: 300, orders: 2 });
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

  const stat = (buyerId: string, overrides: Partial<CustomerOrderStats> = {}): CustomerOrderStats => ({
    buyerId,
    orderCount: 3,
    completedCount: 2,
    totalSpent: 400,
    lastOrderAt: "2026-11-15T12:00:00",
    city: "İstanbul",
    ...overrides,
  });

  it("yöneticiyi dışarıda bırakır ve sunucu özetini kullanıcıyla birleştirir", () => {
    const result = customerStats([user("admin", "Admin"), user("u1")], [stat("u1")]);

    expect(result.map((c) => c.user.id)).toEqual(["u1"]);
    expect(result[0]).toMatchObject({ orderCount: 3, completedCount: 2, totalSpent: 400, city: "İstanbul" });
    // Backend saat dilimi eki olmadan döndürebilir; UTC kabul edilir
    expect(result[0].lastOrderAt?.toISOString()).toBe("2026-11-15T12:00:00.000Z");
  });

  it("hiç siparişi olmayan müşteri için sıfır ve boş değerler döner", () => {
    const [result] = customerStats([user("u2")], []);

    expect(result).toMatchObject({ orderCount: 0, completedCount: 0, totalSpent: 0, lastOrderAt: null, city: null });
  });

  it("siparişi olan ama kullanıcı listesinde bulunmayan alıcıyı yok sayar", () => {
    expect(customerStats([user("u1")], [stat("silinmis-kullanici")])[0].orderCount).toBe(0);
  });
});
