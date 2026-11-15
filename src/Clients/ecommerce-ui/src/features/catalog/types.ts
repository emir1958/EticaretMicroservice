export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
}

export interface ProductWithStock extends Product {
  availableStock: number;
}

export interface ProductInput {
  name: string;
  description: string;
  price: number;
  imageUrl: string;
}

export interface NewProductInput extends ProductInput {
  initialStock: number;
}
