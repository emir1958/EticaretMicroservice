import { useEffect, useState } from "react";

// Değer değiştikten sonra delayMs boyunca yeni değişiklik gelmezse güncellenen kopya.
// Arama kutusunda her tuş vuruşunda sunucuya istek atmamak için: kutu anında, istek gecikmeli.
export function useDebouncedValue<T>(value: T, delayMs: number) {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}
