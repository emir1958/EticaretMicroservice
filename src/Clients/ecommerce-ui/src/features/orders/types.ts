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

// Admin sipariş listesi satırı: tam sipariş değil, listede gösterilenler (kalemler ve adres özetlenmiştir)
export interface OrderListItem {
  id: number;
  buyerId: string;
  contactName: string;
  createdDate: string;
  itemCount: number;
  paymentStatus: PaymentStatus;
  orderStatus: OrderStatus;
  totalPrice: number;
}

export interface PagedOrders {
  items: OrderListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  // Arama/müşteri filtresine uyan siparişlerin durum dağılımı (durum filtresinden bağımsız)
  statusCounts: Record<OrderStatus, number>;
}

export interface OrderSearchParams {
  q?: string;
  status?: OrderStatus;
  buyerId?: string;
  page?: number;
  pageSize?: number;
}

export interface SalesSummary {
  revenue: number;
  orderCount: number;
  completedCount: number;
  pendingCount: number;
  canceledCount: number;
  averageOrderValue: number;
  cancelRate: number;
  itemsSold: number;
}

export interface DailySales {
  date: string;
  revenue: number;
  orders: number;
}

export interface ProductSales {
  productId: string;
  name: string;
  quantity: number;
  revenue: number;
}

export interface CustomerOrderStats {
  buyerId: string;
  orderCount: number;
  completedCount: number;
  totalSpent: number;
  lastOrderAt: string;
  city: string | null;
}

export interface CustomerSummary {
  buyerCount: number;
  repeatBuyerCount: number;
  totalSpent: number;
}
