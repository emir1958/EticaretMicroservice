import type { CartItem } from "../cart/types";

export type OrderStatus = "Beklemede" | "Tamamlandı" | "IptalEdildi";

export type PaymentStatus = "Alinmadi" | "Alindi" | "Basarisiz" | "IadeEdildi";

export interface Address {
  city: string;
  district: string;
  street: string;
  zipCode: string;
  line: string;
}

export interface OrderItem {
  productId: string;
  productName: string;
  price: number;
  quantity: number;
}

export interface Order {
  id: number;
  buyerId: string;
  orderStatus: OrderStatus;
  paymentStatus: PaymentStatus;
  refundedAt: string | null;
  createdDate: string;
  address: Address;
  orderItems: OrderItem[];
  totalPrice: number;
}

export interface NewOrder {
  address: Address;
  paymentToken: string;
  items: CartItem[];
}
