import type { CartItem } from "../cart/types";

export type OrderStatus = "Beklemede" | "Tamamlandı" | "IptalEdildi";

export type PaymentStatus = "Alinmadi" | "Alindi" | "Basarisiz" | "IadeEdildi";

export type OrderHistoryType =
  | "SiparisAlindi"
  | "StokAyrildi"
  | "StokYetersiz"
  | "OdemeAlindi"
  | "OdemeBasarisiz"
  | "Tamamlandi"
  | "IptalEdildi"
  | "IadeEdildi";

export interface OrderHistoryEntry {
  type: OrderHistoryType;
  occurredAt: string;
  note: string | null;
}

export interface Address {
  contactName: string;
  phone: string;
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
  history: OrderHistoryEntry[];
}

export interface NewOrder {
  address: Address;
  paymentToken: string;
  items: CartItem[];
}
