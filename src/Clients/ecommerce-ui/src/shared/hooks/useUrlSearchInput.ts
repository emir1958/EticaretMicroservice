import { useState } from "react";

// Kutunun değeri yerel state'te durur, URL'ye de yazılır. Input doğrudan searchParams'a bağlansaydı hızlı yazarken
// harf düşerdi: setSearchParams asenkron tamamlanır ve kontrollü input bir an eski değere döner.
// urlValue yalnızca ilk değerdir; URL'yi dışarıdan sıfırlayan eylemler ("Filtreleri temizle") clear() da çağırmalı.
export function useUrlSearchInput(urlValue: string, writeToUrl: (value: string | null) => void) {
  const [value, setValue] = useState(urlValue);

  return {
    value,
    onChange: (next: string) => {
      setValue(next);
      writeToUrl(next || null);
    },
    clear: () => setValue(""),
  };
}
