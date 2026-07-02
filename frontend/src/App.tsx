import { lazy, Suspense } from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { Layout } from "@/components/layout/Layout";
import { Toaster } from "react-hot-toast";
import { ThemeProvider } from "@/context/ThemeContext";
import { CurrencyProvider } from "@/context/CurrencyContext";

const LandingPage = lazy(() => import("@/pages/LandingPage").then((m) => ({ default: m.LandingPage })));
const LoginPage = lazy(() => import("@/pages/LoginPage").then((m) => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import("@/pages/RegisterPage").then((m) => ({ default: m.RegisterPage })));
const ForgotPasswordPage = lazy(() => import("@/pages/ForgotPasswordPage").then((m) => ({ default: m.ForgotPasswordPage })));
const ResetPasswordPage = lazy(() => import("@/pages/ResetPasswordPage").then((m) => ({ default: m.ResetPasswordPage })));
const VerifyEmailPage = lazy(() => import("@/pages/VerifyEmailPage").then((m) => ({ default: m.VerifyEmailPage })));
const DashboardPage = lazy(() => import("@/pages/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const ProductsPage = lazy(() => import("@/pages/ProductsPage").then((m) => ({ default: m.ProductsPage })));
const ProductDetailPage = lazy(() => import("@/pages/ProductDetailPage").then((m) => ({ default: m.ProductDetailPage })));
const ActiveTrackingsPage = lazy(() => import("@/pages/ActiveTrackingsPage").then((m) => ({ default: m.ActiveTrackingsPage })));
const PriceHistoryPage = lazy(() => import("@/pages/PriceHistoryPage").then((m) => ({ default: m.PriceHistoryPage })));
const NotificationsPage = lazy(() => import("@/pages/NotificationsPage").then((m) => ({ default: m.NotificationsPage })));
const StoresDirectoryPage = lazy(() => import("@/pages/StoresDirectoryPage").then((m) => ({ default: m.StoresDirectoryPage })));
const AdminDashboardPage = lazy(() => import("@/pages/AdminDashboardPage").then((m) => ({ default: m.AdminDashboardPage })));
const AdminCategoriesPage = lazy(() => import("@/pages/AdminCategoriesPage").then((m) => ({ default: m.AdminCategoriesPage })));
const AdminCurrenciesPage = lazy(() => import("@/pages/AdminCurrenciesPage").then((m) => ({ default: m.AdminCurrenciesPage })));
const AdminStoresPage = lazy(() => import("@/pages/AdminStoresPage").then((m) => ({ default: m.AdminStoresPage })));
const AdminScrapeLogsPage = lazy(() => import("@/pages/AdminScrapeLogsPage").then((m) => ({ default: m.AdminScrapeLogsPage })));

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
          <Suspense fallback={null}>
            <Routes>
              <Route path="/" element={<Layout />}>
                <Route index element={<LandingPage />} />
                <Route path="login" element={<LoginPage />} />
                <Route path="register" element={<RegisterPage />} />
                <Route path="forgot-password" element={<ForgotPasswordPage />} />
                <Route path="reset-password" element={<ResetPasswordPage />} />
                <Route path="verify-email" element={<VerifyEmailPage />} />
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
          </Suspense>
        </BrowserRouter>
      </CurrencyProvider>
    </ThemeProvider>
  );
}
