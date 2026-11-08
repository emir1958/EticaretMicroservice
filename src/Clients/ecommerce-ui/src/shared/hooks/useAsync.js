import { useCallback, useEffect, useState } from "react";

// Sayfa açılınca (ve deps değişince) veri getiren tek kalıp.
//
//   const { data, error, isLoading, reload } = useAsync(() => getMyOrder(id), [id]);
//
// - asyncFn: Promise döndüren fonksiyon (genelde bir features/<x>/api.js fonksiyonu).
// - deps:    useEffect'teki gibi; bu değerlerden biri değişince istek yeniden atılır.
//            ESLint (exhaustive-deps) eksik bağımlılığı useEffect'teki gibi yakalar.
// - data:    bu deps için son başarılı sonuç.
//              reload sırasında eski veri korunur (liste boşalıp yanıp sönmez);
//              deps değişince hemen undefined olur (başka kaydın verisi asla gösterilmez).
// - error:   son isteğin hatası (axios hatası); yükleme sırasında null.
// - reload:  aynı isteği tekrar atar (örn. admin kaydettikten sonra listeyi yenilemek için).
export function useAsync(asyncFn, deps) {
  // Sonuç, hangi istek için geldiğiyle birlikte saklanır: { deps, reloadCount, data, error }
  const [result, setResult] = useState(null);
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    // Cevap gelmeden deps değişirse (örn. kullanıcı başka siparişe geçti) veya component kapanırsa
    // cleanup çalışır ve eski isteğin geç gelen cevabı yok sayılır: race condition olmaz.
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
    // asyncFn ve deps dizisinin kendisi bilerek listede yok: ikisi de her render'da yeni nesnedir ve
    // istek sonsuz döngüye girerdi. Listede deps'in *değerleri* var; neye bağlı olduğunu çağıran
    // taraf söyler ve ESLint orada kontrol eder (eslint.config.js → additionalHooks).
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
