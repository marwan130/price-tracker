export interface User {
  userId: string;
  name: string;
  email: string;
  role: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface AuthResponse {
  userId: string;
  name: string;
  email: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface Category {
  categoryId: number;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface Currency {
  currencyId: number;
  code: string;
  name: string;
  symbol: string;
  isActive: boolean;
  createdAt: string;
}

export interface StoreItem {
  storeId: string;
  name: string;
  websiteUrl: string | null;
  baseUrl?: string | null;
  country: string | null;
  currency: string | null;
  currencyCode?: string | null;
  isActive: boolean;
  scraperType?: string;
  listingCount?: number;
  createdAt: string;
}

export interface NotificationItem {
  notificationId: number;
  userId?: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  actionUrl?: string;
}

export interface ProductDetail {
  productId: string;
  name: string;
  brand: string | null;
  category: string | null;
  description: string | null;
  primaryImage: string | null;
  images: string[];
  lowestPrice: number | null;
  currency: string | null;
}

export interface StoreListing {
  listingId: string;
  storeId: string;
  storeName: string;
  storeUrl: string | null;
  productUrl?: string | null;
  currentPrice: number;
  currency: string;
  currencyCode?: string | null;
  lastScrapedAt: string;
  isActive: boolean;
}

export interface PriceHistoryPoint {
  priceHistoryId: string;
  price: number;
  recordedAt: string;
}

export type TimeRange = "7d" | "30d" | "90d" | "all";

export interface ScrapeLog {
  id: string;
  storeName: string;
  scraperType: string;
  status: "success" | "failed" | "partial";
  itemsScraped: number;
  durationMs: number | null;
  errorMessage: string | null;
  startedAt: string;
}