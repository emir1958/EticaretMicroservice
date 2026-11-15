import { useCallback, useEffect, useState } from "react";

export function useAsync(asyncFn, deps) {
  // Sonuç, hangi istek için geldiğiyle birlikte saklanır: { deps, reloadCount, data, error }
  const [result, setResult] = useState(null);
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let ignore = false;

    asyncFn().then(
      (data) => {
        if (!ignore) setResult({ deps, reloadCount, data, error: null });
      },
      (error) => {
        if (ignore) return;
        console.error("[useAsync]", error);
        // reload başarısız olursa aynı kaydın eski verisi ekranda kalır
        setResult((prev) => ({
          deps,
          reloadCount,
          data: prev && sameValues(prev.deps, deps) ? prev.data : undefined,
          error,
        }));
      },
    );

    return () => {
      ignore = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, reloadCount]);

  // "Yükleniyor" ayrı bir state değil, türetilen değer: effect içinde senkron setState gerekmez.
  const isSameRecord = result !== null && sameValues(result.deps, deps);
  const isLoading = !isSameRecord || result.reloadCount !== reloadCount;

  const reload = useCallback(() => setReloadCount((count) => count + 1), []);

  return {
    data: isSameRecord ? result.data : undefined,
    error: isLoading ? null : result.error,
    isLoading,
    reload,
  };
}

function sameValues(a, b) {
  return a.length === b.length && a.every((value, i) => Object.is(value, b[i]));
}
