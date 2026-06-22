import { BrowserRouter, Routes, Route } from "react-router-dom";
import { Layout } from "@/components/layout/Layout";
import { LandingPage } from "@/pages/LandingPage";
import { LoginPage } from "@/pages/LoginPage";
import { RegisterPage } from "@/pages/RegisterPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { ProductsPage } from "@/pages/ProductsPage";
import { ProductDetailPage } from "@/pages/ProductDetailPage";
import { ActiveTrackingsPage } from "@/pages/ActiveTrackingsPage";
import { PriceHistoryPage } from "@/pages/PriceHistoryPage";
import { NotificationsPage } from "@/pages/NotificationsPage";
import { StoresDirectoryPage } from "@/pages/StoresDirectoryPage";
import { AdminDashboardPage } from "@/pages/AdminDashboardPage";
import { AdminCategoriesPage } from "@/pages/AdminCategoriesPage";
import { AdminCurrenciesPage } from "@/pages/AdminCurrenciesPage";
import { AdminStoresPage } from "@/pages/AdminStoresPage";
import { AdminScrapeLogsPage } from "@/pages/AdminScrapeLogsPage";
import { Toaster } from "react-hot-toast";
import { ThemeProvider } from "@/context/ThemeContext";
import { CurrencyProvider } from "@/context/CurrencyContext";

export default function App() {
  return (
    <ThemeProvider>
      <CurrencyProvider>
        <BrowserRouter>
          <Toaster
            position="top-right"
            toastOptions={{
              style: {
                background: "var(--color-surface)",
                color: "var(--color-text-primary)",
                border: "1px solid var(--color-border-custom)",
              },
            }}
          />
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<LandingPage />} />
              <Route path="login" element={<LoginPage />} />
              <Route path="register" element={<RegisterPage />} />
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="products" element={<ProductsPage />} />
              <Route path="products/:productId" element={<ProductDetailPage />} />
              <Route path="products/:productId/history" element={<PriceHistoryPage />} />
              <Route path="trackings" element={<ActiveTrackingsPage />} />
              <Route path="notifications" element={<NotificationsPage />} />
              <Route path="stores" element={<StoresDirectoryPage />} />
              <Route path="admin" element={<AdminDashboardPage />} />
              <Route path="admin/categories" element={<AdminCategoriesPage />} />
              <Route path="admin/currencies" element={<AdminCurrenciesPage />} />
              <Route path="admin/stores" element={<AdminStoresPage />} />
              <Route path="admin/scrape-logs" element={<AdminScrapeLogsPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </CurrencyProvider>
    </ThemeProvider>
  );
}