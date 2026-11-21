import { describe, expect, it } from "vitest";
import { mergeCarts } from "./mergeCarts";
import type { CartItem } from "./types";

const item = (id: string, quantity: number): CartItem => ({ id, name: `Ürün ${id}`, price: 100, quantity, imageUrl: "" });

describe("mergeCarts", () => {
  it("aynı ürünün adetlerini toplar", () => {
    const merged = mergeCarts([item("a", 2)], [item("a", 3)]);

    expect(merged).toEqual([item("a", 5)]);
  });

  it("önce sunucu sepetini, ardından yalnızca misafirde olan ürünleri sıralar", () => {
    const merged = mergeCarts([item("a", 1), item("b", 1)], [item("c", 1), item("a", 1)]);

    expect(merged.map((i) => i.id)).toEqual(["a", "b", "c"]);
    expect(merged.find((i) => i.id === "a")?.quantity).toBe(2);
  });

  it("boş misafir sepetinde sunucu sepetini aynen korur", () => {
    expect(mergeCarts([item("a", 4)], [])).toEqual([item("a", 4)]);
  });

  it("girdi dizilerini ve ürün nesnelerini değiştirmez", () => {
    const server = [item("a", 2)];
    const guest = [item("a", 3)];

    mergeCarts(server, guest);

    expect(server[0].quantity).toBe(2);
    expect(guest[0].quantity).toBe(3);
  });
});
