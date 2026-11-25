export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
  // Boş olabilir: eski ürünlerde kategori yok (arayüzde "Diğer" altında gösterilir)
  category: string;
  // İndirim öncesi liste fiyatı; yalnızca price'tan büyükse doludur
  compareAtPrice: number | null;
  // Geliştirme ortamındaki seed ile eklenen örnek ürün
  isSample: boolean;
}

export interface ProductWithStock extends Product {
  availableStock: number;
}

export interface ProductInput {
  name: string;
  description: string;
  price: number;
  imageUrl: string;
  category: string;
  compareAtPrice: number | null;
}

export interface NewProductInput extends ProductInput {
  initialStock: number;
}

export interface Category {
  name: string;
  count: number;
  imageUrl: string;
}
