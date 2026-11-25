import { useCallback, useEffect, useState, type DependencyList } from "react";

interface AsyncResult<T> {
  deps: DependencyList;
  reloadCount: number;
  data: T | undefined;
  error: unknown;
}

export function useAsync<T>(asyncFn: () => Promise<T>, deps: DependencyList) {
  // Sonuç, hangi istek için geldiğiyle birlikte saklanır
  const [result, setResult] = useState<AsyncResult<T> | null>(null);
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let ignore = false;

    asyncFn().then(
      (data) => {
        if (!ignore) setResult({ deps, reloadCount, data, error: null });
      },
      (error: unknown) => {
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

function sameValues(a: DependencyList, b: DependencyList) {
  return a.length === b.length && a.every((value, i) => Object.is(value, b[i]));
}
