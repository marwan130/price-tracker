import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Mail, ArrowLeft, Loader2, CheckCircle } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import { emailValidationSchema } from "@/lib/validation/email";
import toast from "react-hot-toast";

const forgotPasswordSchema = z.object({
  email: emailValidationSchema,
});

type ForgotPasswordFields = z.infer<typeof forgotPasswordSchema>;

export function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);
  const [emailSent, setEmailSent] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFields>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = async (data: ForgotPasswordFields) => {
    setIsLoading(true);
    try {
      await apiClient.post("/v1/auth/forgot-password", { email: data.email });
      setEmailSent(data.email);
      toast.success("Password reset email sent");
    } catch {
      toast.error("Failed to send reset email");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-[calc(100vh-5rem)] flex items-center justify-center p-4 md:p-8">
      {/* Back to Home Button */}
      <Link
        to="/login"
        className="absolute top-4 left-4 md:top-8 md:left-8 flex items-center gap-2 text-sm text-text-secondary hover:text-text-primary transition-colors duration-200 z-20 reveal"
      >
        <ArrowLeft className="w-4 h-4" />
        <span>Back to Login</span>
      </Link>

      <div className="w-full max-w-md rounded-3xl overflow-hidden hp-glass-card border border-border-custom shadow-2xl p-8 md:p-12 reveal">
        {!emailSent ? (
          <>
            <div className="mb-8 text-center">
              <div className="mb-4 flex justify-center">
                <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/20 border border-primary/30">
                  <Mail className="h-8 w-8 text-primary" />
                </div>
              </div>
              <h2 className="text-3xl font-display font-bold text-text-primary mb-2">Forgot Password?</h2>
              <p className="text-text-secondary text-sm">
                Enter your email address and we'll send you a link to reset your password.
              </p>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
              <div className={`relative ${errors.email ? "animate-shake" : ""}`}>
                <div className="relative">
                  <input
                    id="email"
                    type="email"
                    autoComplete="email"
                    inputMode="email"
                    placeholder=" "
                    {...register("email")}
                    className={`peer w-full rounded-xl border ${
                      errors.email ? "border-accent-secondary" : "border-border-custom"
                    } bg-surface/60 px-4 pt-5 pb-2 text-text-primary placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
                  />
                  <label
                    htmlFor="email"
                    className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
                  >
                    Email Address
                  </label>
                </div>
                {errors.email && (
                  <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.email.message}</p>
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
                    Sending...
                  </span>
                ) : (
                  "Send Reset Link"
                )}
              </button>
            </form>
          </>
        ) : (
          <div className="text-center">
            <div className="mb-4 flex justify-center">
              <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-success/20 border border-success/30">
                <CheckCircle className="h-8 w-8 text-success" />
              </div>
            </div>
            <h2 className="text-3xl font-display font-bold text-text-primary mb-2">Check Your Email</h2>
            <p className="text-text-secondary text-sm mb-6">
              We've sent a password reset link to <span className="font-semibold text-text-primary">{emailSent}</span>
            </p>
            <button
              onClick={() => {
                setEmailSent(null);
                navigate("/login");
              }}
              className="w-full rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50"
            >
              Back to Login
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
