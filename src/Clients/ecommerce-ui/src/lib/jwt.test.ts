import { describe, expect, it } from "vitest";
import { getTokenExpiry, parseJwt } from "./jwt";
import { fakeJwt } from "./testHelpers";

describe("parseJwt", () => {
  it("payload'ı okur, Türkçe karakterleri bozmaz", () => {
    const payload = parseJwt<{ unique_name: string }>(fakeJwt({ unique_name: "Şükrü Çağlar" }));

    expect(payload?.unique_name).toBe("Şükrü Çağlar");
  });

  it("bozuk token için null döner (exception fırlatmaz)", () => {
    expect(parseJwt("")).toBeNull();
    expect(parseJwt("tek-parca")).toBeNull();
    expect(parseJwt("a.%%%.c")).toBeNull();
  });
});

describe("getTokenExpiry", () => {
  it("exp saniyesini milisaniyeye çevirir", () => {
    expect(getTokenExpiry(fakeJwt({ exp: 1_800_000_000 }))).toBe(1_800_000_000_000);
  });

  it("exp yoksa ya da sayı değilse null döner", () => {
    expect(getTokenExpiry(fakeJwt({ sub: "1" }))).toBeNull();
    expect(getTokenExpiry(fakeJwt({ exp: "yarin" }))).toBeNull();
  });
});
