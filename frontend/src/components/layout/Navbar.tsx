import { useEffect, useRef, useState } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { Bell, Menu, X, LogOut, Sun, Moon } from "lucide-react";
import { useAuthStore } from "@/lib/store/useAuthStore";
import { useNotificationStore } from "@/lib/store/useNotificationStore";
import { useTheme } from "@/context/ThemeContext";
import { useCurrency } from "@/context/CurrencyContext";
import { ThemedDropdown } from "@/components/ui/ThemedDropdown";
import type { CurrencyCode } from "@/context/CurrencyContext";


const linkBase =
  "relative rounded-full px-3 py-2 text-sm font-semibold text-text-secondary hover:bg-white/10 hover:text-text-primary whitespace-nowrap transition-all duration-200";
const activeClass = "bg-primary/20 text-text-primary";

function NavItem({ to, label }: { to: string; label: string }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) => `${linkBase} ${isActive ? activeClass : ""}`}
    >
      {({ isActive }) => (
        <>
          <span>{label}</span>
          {isActive && <span className="nav-active-dot animate-pulse-slow" />}
        </>
      )}
    </NavLink>
  );
}

function CurrencyDropdown() {
  const { currency, setCurrency } = useCurrency();
  const currencies: CurrencyCode[] = ["EGP", "SAR", "AED", "USD"];

  return (
    <ThemedDropdown
      value={currency}
      options={currencies.map((c) => ({ value: c, label: c }))}
      onChange={(value) => setCurrency(value as CurrencyCode)}
      className="w-28"
    />
  );
}

export function Navbar() {
  const navigate = useNavigate();
  const token = useAuthStore((s) => s.token);
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const unreadCount = useNotificationStore((s) => s.unreadCount);
  const { theme, toggleTheme } = useTheme();

  const [compact, setCompact] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [countAnimating, setCountAnimating] = useState(false);

  const compactRef = useRef(false);
  const scrolledRef = useRef(false);
  const rafRef = useRef<number | null>(null);
  const lastYRef = useRef(0);
  const prevCountRef = useRef(0);

  // hook tracking scroll ticks to adjust sizes
  useEffect(() => {
    lastYRef.current = window.scrollY;

    const handleScroll = () => {
      if (rafRef.current !== null) return;

      rafRef.current = requestAnimationFrame(() => {
        rafRef.current = null;
        const currentY = window.scrollY;
        const lastY = lastYRef.current;

        const nowScrolled = currentY > 6;
        if (nowScrolled !== scrolledRef.current) {
          scrolledRef.current = nowScrolled;
          setScrolled(nowScrolled);
        }

        if (!compactRef.current && currentY > lastY + 10 && currentY > 80) {
          compactRef.current = true;
          setCompact(true);
        } else if (compactRef.current && (currentY < lastY - 10 || currentY < 40)) {
          compactRef.current = false;
          setCompact(false);
        }

        lastYRef.current = currentY;
      });
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => {
      window.removeEventListener("scroll", handleScroll);
      if (rafRef.current !== null) cancelAnimationFrame(rafRef.current);
    };
  }, []);

  // trigger spring animation when count changes
  useEffect(() => {
    if (unreadCount !== prevCountRef.current) {
      setCountAnimating(true);
      setTimeout(() => setCountAnimating(false), 500);
      prevCountRef.current = unreadCount;
    }
  }, [unreadCount]);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const isAdmin = user?.role?.toLowerCase() === "admin";

  return (
    <>
      <header
        className={[
          "mt-4 flex w-max max-w-[calc(100vw-2rem)] items-center justify-between gap-6",
          "rounded-full border border-border-custom px-6 shadow-2xl backdrop-blur-lg z-50",
          "transition-[transform,opacity,background-color,border-color,box-shadow,height] duration-300 ease-out",
          compact
            ? "h-12 scale-[0.96] opacity-95"
            : "h-16 scale-100 opacity-100",
          scrolled
            ? "bg-surface/90 shadow-[0_8px_32px_rgba(108,99,255,0.2)] border-primary/30"
            : "bg-surface/70 border-border-custom",
        ].join(" ")}
      >
        {/* brand logo */}
        <Link
          to="/"
          className="group flex shrink-0 items-center gap-2 text-lg font-display font-black tracking-tight transition-colors duration-200 hover:text-accent md:text-xl"
        >
          <img 
            src="/logo.png" 
            alt="SmartTracker Logo" 
            className="h-9 w-9 rounded-xl shadow-inner transition-transform duration-300 group-hover:scale-110"
          />
          {!compact && (
            <span className="font-display">
              SmartTracker
            </span>
          )}
        </Link>

        {/* navigation links */}
        {!compact && (
          <nav className="hidden items-center gap-1 lg:flex">
            <NavItem to="/" label="Home" />
            <NavItem to="/products" label="Products" />
            {token && (
              <NavItem to="/dashboard" label="Dashboard" />
            )}
            {token && isAdmin && (
              <NavItem to="/admin/scrape-logs" label="Admin" />
            )}
          </nav>
        )}

        {/* user actions segment */}
        <div className="flex shrink-0 items-center gap-3">
          {/* Currency Dropdown Selector */}
          <CurrencyDropdown />

          {/* Theme Toggle Button */}
          <button
            onClick={toggleTheme}
            className="p-2 rounded-full text-text-secondary hover:bg-white/10 hover:text-text-primary transition-all duration-200 cursor-pointer"
            aria-label="Toggle Theme"
          >
            {theme === "dark" ? <Sun className="w-5 h-5 text-warning" /> : <Moon className="w-5 h-5 text-primary" />}
          </button>

          {/* notification bell */}
          {token && (
            <Link
              to="/notifications"
              className={`relative p-2 rounded-full text-text-secondary hover:bg-white/10 hover:text-text-primary transition-all ${
                unreadCount > 0 ? "animate-pulse-slow" : ""
              }`}
              aria-label="Notifications"
            >
              <Bell className={`w-5 h-5 ${countAnimating ? "animate-bounce" : ""}`} />
              {unreadCount > 0 && (
                <span className={`absolute top-1 right-1 flex h-4 w-4 items-center justify-center rounded-full bg-accent-secondary text-[9px] font-black text-text-primary ${
                  countAnimating ? "animate-scale-in" : "animate-bounce"
                }`}>
                  {unreadCount}
                </span>
              )}
            </Link>
          )}

          {token ? (
            <button
              onClick={handleLogout}
              className={`btn-ieee rounded-full border border-border-custom/20 bg-surface px-4 py-2 text-[11px] font-black uppercase tracking-widest text-text-secondary hover:border-border-custom/40 hover:text-text-primary flex items-center gap-1 cursor-pointer ${compact ? "px-2.5 py-1.5" : ""
                }`}
            >
              <LogOut className="w-3.5 h-3.5" />
              {!compact && <span>Sign Out</span>}
            </button>
          ) : (
            <div className="flex items-center gap-2">
              {!compact && (
                <Link
                  to="/login"
                  className="rounded-full px-3 py-2 text-sm font-semibold text-text-secondary hover:bg-white/10 hover:text-text-primary"
                >
                  Log In
                </Link>
              )}
              <Link
                to="/register"
                className={`btn-ieee btn-shimmer rounded-full bg-primary font-bold text-text-primary shadow-md hover:brightness-110 ${compact ? "px-4 py-1.5 text-xs" : "px-5 py-2 text-sm"
                  }`}
              >
                Get Started
              </Link>
            </div>
          )}

          {/* hamburger button for mobile view */}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="flex p-2 rounded-full text-text-secondary hover:bg-white/10 hover:text-text-primary lg:hidden cursor-pointer"
          >
            {mobileOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
          </button>
        </div>
      </header>

      {/* drawer component for mobile navigation */}
      <div
        className={[
          "fixed inset-y-0 right-0 z-40 w-64 bg-surface-elevated/95 backdrop-blur-xl shadow-2xl border-l border-border-custom p-6 pt-24",
          "transition-transform duration-300 ease-out transform lg:hidden",
          mobileOpen ? "translate-x-0" : "translate-x-full",
        ].join(" ")}
      >
        <nav className="flex flex-col gap-4">
          <Link
            to="/"
            onClick={() => setMobileOpen(false)}
            className="flex items-center gap-3 p-3 rounded-xl hover:bg-white/10 text-text-primary font-medium"
          >
            <span>Home</span>
          </Link>
          <Link
            to="/products"
            onClick={() => setMobileOpen(false)}
            className="flex items-center gap-3 p-3 rounded-xl hover:bg-white/10 text-text-primary font-medium"
          >
            <span>Products</span>
          </Link>
          {token && (
            <Link
              to="/dashboard"
              onClick={() => setMobileOpen(false)}
              className="flex items-center gap-3 p-3 rounded-xl hover:bg-white/10 text-text-primary font-medium"
            >
              <span>Dashboard</span>
            </Link>
          )}
          {token && isAdmin && (
            <Link
              to="/admin/scrape-logs"
              onClick={() => setMobileOpen(false)}
              className="flex items-center gap-3 p-3 rounded-xl hover:bg-white/10 text-text-primary font-medium"
            >
              <span>Admin Panel</span>
            </Link>
          )}

          {/* Mobile Currency & Theme Actions */}
          <div className="mt-4 border-t border-border-custom pt-4 flex flex-col gap-4">
            <div className="flex items-center justify-between px-3">
              <span className="text-sm font-semibold text-text-secondary">Currency</span>
              <CurrencyDropdown />
            </div>
            <div className="flex items-center justify-between px-3">
              <span className="text-sm font-semibold text-text-secondary">Theme</span>
              <button
                onClick={toggleTheme}
                className="flex items-center gap-2 bg-white/5 border border-border-custom/10 rounded-full px-3 py-1.5 text-xs text-text-secondary cursor-pointer"
              >
                {theme === "dark" ? (
                  <>
                    <Sun className="w-4 h-4 text-warning" />
                    <span>Light Mode</span>
                  </>
                ) : (
                  <>
                    <Moon className="w-4 h-4 text-primary" />
                    <span>Dark Mode</span>
                  </>
                )}
              </button>
            </div>
          </div>
        </nav>
      </div>
    </>
  );
}