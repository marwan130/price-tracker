import { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Activity, Mail, ShoppingBag, Eye, EyeOff, Loader2, ArrowLeft } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import { useAuthStore } from "@/lib/store/useAuthStore";
import toast from "react-hot-toast";

// Validation schema using Zod
const loginSchema = z.object({
  email: z.string().min(1, "Email is required").email("Invalid email format"),
  password: z.string().min(6, "Password must be at least 6 characters"),
});

type LoginFields = z.infer<typeof loginSchema>;

export function LoginPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [loginError, setLoginError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<LoginFields>({
    resolver: zodResolver(loginSchema),
  });

  const password = watch("password");

  // Clear error when user starts typing password
  useEffect(() => {
    if (password) {
      setLoginError(null);
    }
  }, [password]);

  const onSubmit = async (data: LoginFields, event?: React.BaseSyntheticEvent) => {
    event?.preventDefault();
    setLoginError(null);
    console.log("Login attempt started", data);
    setIsLoading(true);
    try {
      console.log("Sending login request...");
      const res = await apiClient.post("/v1/auth/login", data);
      console.log("Login response:", res);
      
      if (res.data?.success && res.data?.data) {
        const payload = res.data.data;
        setSession(payload.accessToken, payload.refreshToken, {
          userId: payload.userId,
          name: payload.name,
          email: payload.email,
          role: payload.role,
        });
        toast.success(`Welcome back, ${payload.name}!`);
        navigate("/dashboard");
      } else {
        setLoginError("Invalid email or password. Please try again.");
      }
    } catch (err: any) {
      console.error("Login error caught:", err);
      console.error("Error response:", err.response);
      console.error("Error status:", err.response?.status);
      
      // Handle specific error messages from the API
      const errorMessage = err.response?.data?.error?.message || err.response?.data?.message || err.message;
      
      if (err.response?.status === 401) {
        setLoginError("Invalid email or password. Please check your credentials and try again.");
      } else if (err.response?.status === 400) {
        setLoginError(errorMessage || "Invalid request. Please check your input.");
      } else if (err.response?.status === 429) {
        setLoginError("Too many login attempts. Please wait a moment and try again.");
      } else {
        setLoginError(errorMessage || "An error occurred during login. Please try again.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-[calc(100vh-5rem)] flex items-center justify-center p-4 md:p-8">
      {/* Back to Home Button */}
      <Link
        to="/"
        className="absolute top-4 left-4 md:top-8 md:left-8 flex items-center gap-2 text-sm text-text-secondary hover:text-white transition-colors duration-200 z-20 reveal"
      >
        <ArrowLeft className="w-4 h-4" />
        <span>Back to Home</span>
      </Link>

      <div className="w-full max-w-5xl grid grid-cols-1 lg:grid-cols-12 rounded-3xl overflow-hidden hp-glass-card border border-border-custom shadow-2xl min-h-[600px] reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        
        {/* Left Panel: Form */}
        <div className="lg:col-span-6 p-8 md:p-12 flex flex-col justify-center bg-surface/40 backdrop-blur-md">
          <div className="mb-8 text-center lg:text-left reveal" style={{ "--reveal-delay": "150ms" } as React.CSSProperties}>
            <Link to="/" className="inline-flex items-center gap-2 font-display font-black text-2xl text-white mb-6">
              <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/20 border border-primary/30">
                <ShoppingBag className="h-5 w-5 text-accent" />
              </div>
              <span>SmartTracker</span>
            </Link>
            <h2 className="text-3xl font-display font-bold text-white mb-2">Welcome Back</h2>
            <p className="text-text-secondary text-sm">Enter your credentials to access your price alerts dashboard.</p>
          </div>

          <div className="space-y-6 reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
            
            {/* Email Field */}
            <div className={`relative ${errors.email ? "animate-shake" : ""}`}>
              <div className="relative">
                <input
                  id="email"
                  type="email"
                  placeholder=" "
                  {...register("email")}
                  className={`peer w-full rounded-xl border ${
                    errors.email ? "border-accent-secondary" : "border-border-custom"
                  } bg-surface/60 px-4 pt-5 pb-2 text-white placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
                />
                <label
                  htmlFor="email"
                  className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
                >
                  Email Address
                </label>
                <Mail className="absolute right-4 top-1/2 -translate-y-1/2 w-5 h-5 text-text-muted pointer-events-none" />
              </div>
              {errors.email && (
                <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.email.message}</p>
              )}
            </div>

            {/* Password Field */}
            <div className={`relative ${errors.password ? "animate-shake" : ""}`}>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  placeholder=" "
                  {...register("password")}
                  className={`peer w-full rounded-xl border ${
                    errors.password || loginError ? "border-accent-secondary" : "border-border-custom"
                  } bg-surface/60 px-4 pt-5 pb-2 pr-12 text-white placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
                />
                <label
                  htmlFor="password"
                  className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
                >
                  Password
                </label>
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-4 top-1/2 -translate-y-1/2 w-5 h-5 text-text-muted hover:text-white transition-colors duration-200"
                >
                  {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.password.message}</p>
              )}
              {loginError && !errors.password && (
                <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{loginError}</p>
              )}
            </div>

            <div className="flex items-center justify-between text-xs font-semibold">
              <label className="flex items-center gap-2 cursor-pointer text-text-secondary hover:text-white">
                <input type="checkbox" className="rounded bg-surface-elevated border-border-custom text-primary focus:ring-primary/20 w-4 h-4 cursor-pointer" />
                <span>Remember me</span>
              </label>
              <Link to="/forgot-password" className="text-primary-light hover:text-accent transition-colors duration-200">
                Forgot Password?
              </Link>
            </div>

            <button
              type="button"
              onClick={handleSubmit(onSubmit)}
              disabled={isLoading}
              className="btn-ieee btn-shimmer w-full py-3.5 bg-primary text-white font-bold rounded-xl flex items-center justify-center gap-2 shadow-[0_8px_24px_rgba(108,99,255,0.25)] disabled:opacity-50"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <span>Logging in...</span>
                </>
              ) : (
                <span>Log In</span>
              )}
            </button>

          </div>

          <div className="mt-8 text-center text-sm text-text-secondary font-semibold reveal" style={{ "--reveal-delay": "250ms" } as React.CSSProperties}>
            Don't have an account?{" "}
            <Link to="/register" className="text-primary-light hover:text-accent font-bold transition-colors">
              Sign Up
            </Link>
          </div>
        </div>

        {/* Right Panel: Illustration/Dashboard Metrics */}
        <div className="lg:col-span-6 relative overflow-hidden hidden lg:flex flex-col justify-between p-12 border-l border-border-custom bg-surface-elevated/20 reveal" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
          {/* Neon mesh background highlights */}
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_70%_30%,rgba(0,212,255,0.1),transparent_50%)]" aria-hidden="true" />
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_80%,rgba(108,99,255,0.12),transparent_50%)]" aria-hidden="true" />

          {/* Header info */}
          <div className="relative z-10">
            <span className="px-3 py-1 rounded-full bg-accent/15 border border-accent/30 text-xs font-bold text-accent">
              Platform Activity
            </span>
          </div>

          {/* Drifting stats */}
          <div className="relative z-10 my-auto space-y-6">
            <div
              className="hp-glass-card p-4 border border-border-custom max-w-sm flex items-center gap-4 animate-float"
              style={{ animationDuration: "10s" }}
            >
              <div className="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center text-primary-light">
                <Activity className="w-5 h-5" />
              </div>
              <div className="text-left">
                <div className="text-xs text-text-secondary font-semibold">Sony WH-1000XM5</div>
                <div className="text-sm font-bold text-white font-mono">Dropped from $349 to $280</div>
              </div>
            </div>

            <div
              className="hp-glass-card p-4 border border-border-custom max-w-sm ml-12 flex items-center gap-4 animate-float"
              style={{ animationDuration: "14s", animationDelay: "-3s" }}
            >
              <div className="w-10 h-10 rounded-full bg-accent/20 flex items-center justify-center text-accent">
                <Activity className="w-5 h-5" />
              </div>
              <div className="text-left">
                <div className="text-xs text-text-secondary font-semibold">User Savings</div>
                <div className="text-sm font-bold text-white font-mono">+$1,452 this month</div>
              </div>
            </div>

            <div
              className="hp-glass-card p-4 border border-border-custom max-w-sm flex items-center gap-4 animate-float"
              style={{ animationDuration: "12s", animationDelay: "-6s" }}
            >
              <div className="w-10 h-10 rounded-full bg-success/20 flex items-center justify-center text-success">
                <Activity className="w-5 h-5" />
              </div>
              <div className="text-left">
                <div className="text-xs text-text-secondary font-semibold">System Scrapers</div>
                <div className="text-sm font-bold text-white font-mono">1,824 active stores scan</div>
              </div>
            </div>
          </div>

          {/* Quote info */}
          <div className="relative z-10 text-left">
            <p className="text-sm text-text-secondary italic mb-2">
              "Saving 20% on tech products is standard now. The alerts system acts instantly."
            </p>
            <span className="text-xs text-text-muted font-bold">— Premium Subscriber</span>
          </div>

        </div>

      </div>
    </div>
  );
}
