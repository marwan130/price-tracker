import { useState, useEffect } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Lock, ArrowLeft, Loader2, CheckCircle, AlertCircle } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import toast from "react-hot-toast";

const resetPasswordSchema = z
  .object({
    password: z
      .string()
      .min(8, "Password must be at least 8 characters")
      .max(100, "Password must not exceed 100 characters")
      .regex(/[A-Z]/, "Must contain at least one uppercase letter")
      .regex(/[a-z]/, "Must contain at least one lowercase letter")
      .regex(/[0-9]/, "Must contain at least one digit")
      .regex(/[^a-zA-Z0-9]/, "Must contain at least one special character"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type ResetPasswordFields = z.infer<typeof resetPasswordSchema>;

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [tokenError, setTokenError] = useState(false);

  const token = searchParams.get("token");

  useEffect(() => {
    if (!token) {
      setTokenError(true);
    }
  }, [token]);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFields>({
    resolver: zodResolver(resetPasswordSchema),
  });

  const onSubmit = async (data: ResetPasswordFields) => {
    if (!token) {
      toast.error("Invalid reset link");
      return;
    }

    setIsLoading(true);
    try {
      await apiClient.post("/v1/auth/reset-password", {
        token,
        newPassword: data.password,
      });
      setIsSuccess(true);
      toast.success("Password reset successfully");
    } catch {
      toast.error("Failed to reset password. The link may be expired.");
      setTokenError(true);
    } finally {
      setIsLoading(false);
    }
  };

  if (tokenError) {
    return (
      <div className="relative min-h-[calc(100vh-5rem)] flex items-center justify-center p-4 md:p-8">
        <Link
          to="/forgot-password"
          className="absolute top-4 left-4 md:top-8 md:left-8 flex items-center gap-2 text-sm text-text-secondary hover:text-text-primary transition-colors duration-200 z-20"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Request New Link</span>
        </Link>

        <div className="w-full max-w-md rounded-3xl overflow-hidden hp-glass-card border border-border-custom shadow-2xl p-8 md:p-12 text-center">
          <div className="mb-4 flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-accent-secondary/20 border border-accent-secondary/30">
              <AlertCircle className="h-8 w-8 text-accent-secondary" />
            </div>
          </div>
          <h2 className="text-3xl font-display font-bold text-text-primary mb-2">Invalid Reset Link</h2>
          <p className="text-text-secondary text-sm mb-6">
            This password reset link is invalid or has expired. Please request a new one.
          </p>
          <Link
            to="/forgot-password"
            className="inline-block w-full rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50"
          >
            Request New Link
          </Link>
        </div>
      </div>
    );
  }

  if (isSuccess) {
    return (
      <div className="relative min-h-[calc(100vh-5rem)] flex items-center justify-center p-4 md:p-8">
        <div className="w-full max-w-md rounded-3xl overflow-hidden hp-glass-card border border-border-custom shadow-2xl p-8 md:p-12 text-center">
          <div className="mb-4 flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-success/20 border border-success/30">
              <CheckCircle className="h-8 w-8 text-success" />
            </div>
          </div>
          <h2 className="text-3xl font-display font-bold text-text-primary mb-2">Password Reset</h2>
          <p className="text-text-secondary text-sm mb-6">
            Your password has been successfully reset. You can now log in with your new password.
          </p>
          <button
            onClick={() => navigate("/login")}
            className="w-full rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50"
          >
            Go to Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="relative min-h-[calc(100vh-5rem)] flex items-center justify-center p-4 md:p-8">
      {/* Back Button */}
      <Link
        to="/forgot-password"
        className="absolute top-4 left-4 md:top-8 md:left-8 flex items-center gap-2 text-sm text-text-secondary hover:text-text-primary transition-colors duration-200 z-20"
      >
        <ArrowLeft className="w-4 h-4" />
        <span>Back</span>
      </Link>

      <div className="w-full max-w-md rounded-3xl overflow-hidden hp-glass-card border border-border-custom shadow-2xl p-8 md:p-12">
        <div className="mb-8 text-center">
          <div className="mb-4 flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/20 border border-primary/30">
              <Lock className="h-8 w-8 text-primary" />
            </div>
          </div>
          <h2 className="text-3xl font-display font-bold text-text-primary mb-2">Reset Password</h2>
          <p className="text-text-secondary text-sm">
            Enter your new password below. Make sure it's strong and secure.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className={`relative ${errors.password ? "animate-shake" : ""}`}>
            <div className="relative">
              <input
                id="password"
                type="password"
                placeholder=" "
                {...register("password")}
                className={`peer w-full rounded-xl border ${
                  errors.password ? "border-accent-secondary" : "border-border-custom"
                } bg-surface/60 px-4 pt-5 pb-2 pr-12 text-text-primary placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
              />
              <label
                htmlFor="password"
                className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
              >
                New Password
              </label>
            </div>
            {errors.password && (
              <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.password.message}</p>
            )}
          </div>

          <div className={`relative ${errors.confirmPassword ? "animate-shake" : ""}`}>
            <div className="relative">
              <input
                id="confirmPassword"
                type="password"
                placeholder=" "
                {...register("confirmPassword")}
                className={`peer w-full rounded-xl border ${
                  errors.confirmPassword ? "border-accent-secondary" : "border-border-custom"
                } bg-surface/60 px-4 pt-5 pb-2 text-text-primary placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
              />
              <label
                htmlFor="confirmPassword"
                className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
              >
                Confirm New Password
              </label>
            </div>
            {errors.confirmPassword && (
              <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.confirmPassword.message}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="w-full rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? (
              <span className="flex items-center justify-center gap-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Resetting...
              </span>
            ) : (
              "Reset Password"
            )}
          </button>
        </form>
      </div>
    </div>
  );
}
