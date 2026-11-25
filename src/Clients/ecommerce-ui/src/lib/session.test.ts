import { beforeEach, describe, expect, it, vi } from "vitest";
import { fakeJwt } from "./testHelpers";

// axios.create() ile üretilen refresh istemcisi sahte bir post fonksiyonuna bağlanır
const post = vi.fn();
vi.mock("axios", () => ({
  default: {
    create: () => ({ post }),
    isAxiosError: (error: unknown) => (error as { isAxiosError?: boolean } | null)?.isAxiosError === true,
  },
}));

const tokenExpiringIn = (seconds: number) => fakeJwt({ exp: Math.floor(Date.now() / 1000) + seconds });
const httpError = (status: number) => ({ isAxiosError: true, response: { status } });

// Node ortamında localStorage yoktur; bellekte çalışan basit bir karşılığı
function stubLocalStorage() {
  const store = new Map<string, string>();
  vi.stubGlobal("localStorage", {
    getItem: (key: string) => store.get(key) ?? null,
    setItem: (key: string, value: string) => void store.set(key, value),
    removeItem: (key: string) => void store.delete(key),
  });
}

// Modül durumu (bellekteki token, uçuştaki istek) testler arasında sızmasın diye her testte yeniden yüklenir
async function loadSession() {
  vi.resetModules();
  return import("./session");
}

beforeEach(() => {
  post.mockReset();
  stubLocalStorage();
});

describe("refreshAccessToken", () => {
  it("aynı anda gelen çağrıları tek isteğe birleştirir", async () => {
    const session = await loadSession();
    const fresh = tokenExpiringIn(900);
    post.mockResolvedValue({ data: { token: fresh } });

    const results = await Promise.all([session.refreshAccessToken(), session.refreshAccessToken(), session.refreshAccessToken()]);

    expect(post).toHaveBeenCalledTimes(1);
    expect(results).toEqual([fresh, fresh, fresh]);
    expect(session.getAccessToken()).toBe(fresh);
  });

  it("başarılı yenileme oturum ipucunu bırakır", async () => {
    const session = await loadSession();
    post.mockResolvedValue({ data: { token: tokenExpiringIn(900) } });

    await session.refreshAccessToken();

    expect(session.hasSessionHint()).toBe(true);
  });

  it("401'de oturum biter: token ve ipucu temizlenir", async () => {
    const session = await loadSession();
    session.setAccessToken(tokenExpiringIn(5));
    post.mockRejectedValue(httpError(401));

    const result = await session.refreshAccessToken();

    expect(result).toBeNull();
    expect(session.getAccessToken()).toBeNull();
    expect(session.hasSessionHint()).toBe(false);
  });

  it("ağ kesintisinde oturumu düşürmez, mevcut token ile devam edilir", async () => {
    const session = await loadSession();
    const current = tokenExpiringIn(5);
    session.setAccessToken(current);
    post.mockRejectedValue(new Error("Network Error"));

    const result = await session.refreshAccessToken();

    expect(result).toBe(current);
    expect(session.getAccessToken()).toBe(current);
    expect(session.hasSessionHint()).toBe(true);
  });

  it("sunucu 500 dönerse de oturum düşmez", async () => {
    const session = await loadSession();
    const current = tokenExpiringIn(5);
    session.setAccessToken(current);
    post.mockRejectedValue(httpError(500));

    await session.refreshAccessToken();

    expect(session.getAccessToken()).toBe(current);
  });

  it("bir yenileme bitince sonraki çağrı yeni bir istek atar", async () => {
    const session = await loadSession();
    post.mockResolvedValue({ data: { token: tokenExpiringIn(900) } });

    await session.refreshAccessToken();
    await session.refreshAccessToken();

    expect(post).toHaveBeenCalledTimes(2);
  });
});

describe("getValidAccessToken", () => {
  it("oturum yoksa (misafir) istek atmaz", async () => {
    const session = await loadSession();

    expect(await session.getValidAccessToken()).toBeNull();
    expect(post).not.toHaveBeenCalled();
  });

  it("token'ın yeterli ömrü varsa aynen döner, yenileme yapmaz", async () => {
    const session = await loadSession();
    const token = tokenExpiringIn(600);
    session.setAccessToken(token);

    expect(await session.getValidAccessToken()).toBe(token);
    expect(post).not.toHaveBeenCalled();
  });

  it("token'ın süresi 30 saniyenin altına inmişse önce yeniler", async () => {
    const session = await loadSession();
    const fresh = tokenExpiringIn(900);
    session.setAccessToken(tokenExpiringIn(10));
    post.mockResolvedValue({ data: { token: fresh } });

    expect(await session.getValidAccessToken()).toBe(fresh);
    expect(post).toHaveBeenCalledTimes(1);
  });

  it("okunamayan token'a dokunmaz (karar sunucuya bırakılır)", async () => {
    const session = await loadSession();
    session.setAccessToken("bozuk-token");

    expect(await session.getValidAccessToken()).toBe("bozuk-token");
    expect(post).not.toHaveBeenCalled();
  });
});

describe("msUntilRefresh", () => {
  it("süre dolmadan 30 saniye önceyi hedefler", async () => {
    const session = await loadSession();
    const now = Date.now();
    const token = fakeJwt({ exp: Math.floor(now / 1000) + 900 });

    const delay = session.msUntilRefresh(token, now)!;

    expect(delay).toBeGreaterThan(868_000);
    expect(delay).toBeLessThanOrEqual(870_000);
  });

  it("zaten süresi dolmak üzereyse hemen (0) yeniler; okunamazsa null", async () => {
    const session = await loadSession();

    expect(session.msUntilRefresh(tokenExpiringIn(5))).toBe(0);
    expect(session.msUntilRefresh("bozuk")).toBeNull();
  });
});

describe("abonelik", () => {
  it("token değişince dinleyiciler haberdar edilir, abonelik iptal edilebilir", async () => {
    const session = await loadSession();
    const listener = vi.fn();
    const unsubscribe = session.subscribe(listener);

    session.setAccessToken(tokenExpiringIn(900));
    unsubscribe();
    session.setAccessToken(null);

    expect(listener).toHaveBeenCalledTimes(1);
  });
});
