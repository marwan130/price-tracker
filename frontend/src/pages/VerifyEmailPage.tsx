import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Loader2,
  Mail,
  MailCheck,
  RefreshCw,
} from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import { emailValidationSchema } from "@/lib/validation/email";
import toast from "react-hot-toast";

type VerifyState = "loading" | "success" | "error" | "pending";

const resendSchema = z.object({
  email: emailValidationSchema,
});

type ResendFields = z.infer<typeof resendSchema>;

function extractApiMessage(error: unknown, fallback: string): string {
  const err = error as {
    response?: { data?: { error?: { message?: string }; message?: string } };
  };
  return (
    err.response?.data?.error?.message ??
    err.response?.data?.message ??
    fallback
  );
}

export function VerifyEmailPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const token = params.get("token");
  const emailFromQuery = params.get("email");

  const [state, setState] = useState<VerifyState>(() =>
    token ? "loading" : "pending"
  );
  const [message, setMessage] = useState(
    token ? "Confirming your email address..." : "Check your inbox to finish setting up your account."
  );
  const [isResending, setIsResending] = useState(false);
  const [resendSent, setResendSent] = useState(false);
  const [redirectSeconds, setRedirectSeconds] = useState(5);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<ResendFields>({
    resolver: zodResolver(resendSchema),
    defaultValues: { email: emailFromQuery ?? "" },
  });

  useEffect(() => {
    if (emailFromQuery) {
      setValue("email", emailFromQuery);
    }
  }, [emailFromQuery, setValue]);

  useEffect(() => {
    if (!token) return;

    let active = true;
    apiClient
      .get("/v1/auth/verify-email", { params: { token } })
      .then((res) => {
        if (!active) return;
        setState("success");
        setMessage(
          res.data?.message ??
            "Your email is verified. You can now sign in and start tracking prices."
        );
      })
      .catch((error) => {
        if (!active) return;
        setState("error");
        setMessage(
          extractApiMessage(
            error,
            "This verification link is invalid or has expired."
          )
        );
      });

    return () => {
      active = false;
    };
  }, [token]);

  useEffect(() => {
    if (state !== "success") return;

    const timer = window.setInterval(() => {
      setRedirectSeconds((seconds) => {
        if (seconds <= 1) {
          window.clearInterval(timer);
          navigate("/login");
          return 0;
        }
        return seconds - 1;
      });
    }, 1000);

    return () => window.clearInterval(timer);
  }, [state, navigate]);

  const onResend = async (data: ResendFields) => {
    setIsResending(true);
    try {
      await apiClient.post("/v1/auth/resend-verification", { email: data.email });
      setResendSent(true);
      toast.success("Verification email sent. Check your inbox.");
    } catch (error) {
      toast.error(
        extractApiMessage(error, "Failed to resend verification email.")
      );
    } finally {
      setIsResending(false);
    }
  };

  const backLink = (
    <Link
      to="/login"
      className="absolute top-4 left-4 z-20 flex items-center gap-2 text-sm text-text-secondary transition-colors duration-200 hover:text-text-primary md:top-8 md:left-8"
    >
      <ArrowLeft className="h-4 w-4" />
      <span>Back to Login</span>
    </Link>
  );

  if (state === "loading") {
    return (
      <div className="relative flex min-h-[calc(100vh-5rem)] items-center justify-center p-4 md:p-8">
        {backLink}
        <div className="w-full max-w-md rounded-3xl border border-border-custom p-8 text-center shadow-2xl hp-glass-card reveal md:p-12">
          <div className="mb-6 flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl border border-primary/30 bg-primary/20">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
            </div>
          </div>
          <h1 className="mb-2 font-display text-3xl font-bold text-text-primary">
            Verifying your email
          </h1>
          <p className="text-sm text-text-secondary">{message}</p>
          <p className="mt-6 text-xs text-text-muted">
            This usually takes a moment. Please keep this tab open.
          </p>
        </div>
      </div>
    );
  }

  if (state === "success") {
    return (
      <div className="relative flex min-h-[calc(100vh-5rem)] items-center justify-center p-4 md:p-8">
        <div className="w-full max-w-md rounded-3xl border border-border-custom p-8 text-center shadow-2xl hp-glass-card reveal md:p-12">
          <div className="mb-6 flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl border border-success/30 bg-success/20">
              <CheckCircle className="h-8 w-8 text-success" />
            </div>
          </div>
          <h1 className="mb-2 font-display text-3xl font-bold text-text-primary">
            Email verified
          </h1>
          <p className="mb-2 text-sm text-text-secondary">{message}</p>
          <p className="mb-8 text-xs text-text-muted">
            Redirecting to login in {redirectSeconds}s...
          </p>
          <button
            type="button"
            onClick={() => navigate("/login")}
            className="w-full rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50"
          >
            Continue to login
          </button>
        </div>
      </div>
    );
  }

  const showResendForm = state === "error" || state === "pending";

  return (
    <div className="relative flex min-h-[calc(100vh-5rem)] items-center justify-center p-4 md:p-8">
      {backLink}

      <div className="w-full max-w-md rounded-3xl border border-border-custom p-8 shadow-2xl hp-glass-card reveal md:p-12">
        <div className="mb-8 text-center">
          <div className="mb-4 flex justify-center">
            <div
              className={`flex h-16 w-16 items-center justify-center rounded-2xl border ${
                state === "error"
                  ? "border-accent-secondary/30 bg-accent-secondary/20"
                  : "border-primary/30 bg-primary/20"
              }`}
            >
              {state === "error" ? (
                <AlertCircle className="h-8 w-8 text-accent-secondary" />
              ) : resendSent ? (
                <MailCheck className="h-8 w-8 text-success" />
              ) : (
                <Mail className="h-8 w-8 text-primary" />
              )}
            </div>
          </div>

          <h1 className="mb-2 font-display text-3xl font-bold text-text-primary">
            {state === "error"
              ? "Verification failed"
              : resendSent
                ? "Email sent"
                : "Verify your email"}
          </h1>

          <p className="text-sm text-text-secondary">
            {state === "error"
              ? message
              : resendSent
                ? "We sent a new verification link. Open it from your inbox to activate your account."
                : emailFromQuery
                  ? `We sent a verification link to ${emailFromQuery}. Open it to activate your account before signing in.`
                  : message}
          </p>
        </div>

        {showResendForm && !resendSent && (
          <form onSubmit={handleSubmit(onResend)} className="space-y-6">
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
                  } bg-surface/60 px-4 pb-2 pt-5 text-text-primary placeholder-transparent transition-all focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20`}
                />
                <label
                  htmlFor="email"
                  className="pointer-events-none absolute left-4 top-1.5 origin-left text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light"
                >
                  Email address
                </label>
              </div>
              {errors.email && (
                <p className="mt-1 pl-1 text-xs font-semibold text-accent-secondary">
                  {errors.email.message}
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={isResending}
              className="flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isResending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Sending...
                </>
              ) : (
                <>
                  <RefreshCw className="h-4 w-4" />
                  Resend verification email
                </>
              )}
            </button>
          </form>
        )}

        {resendSent && (
          <div className="space-y-3">
            <button
              type="button"
              onClick={() => setResendSent(false)}
              className="w-full rounded-xl border border-primary/30 bg-primary/10 px-6 py-3 text-sm font-semibold text-primary-light transition hover:bg-primary/20"
            >
              Send to a different email
            </button>
            <button
              type="button"
              onClick={() => navigate("/login")}
              className="w-full rounded-xl bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition-all duration-200 hover:bg-primary-light hover:shadow-primary/40 focus:outline-none focus:ring-2 focus:ring-primary/50"
            >
              Go to login
            </button>
          </div>
        )}

        {state === "error" && !resendSent && (
          <div className="mt-6 space-y-3 border-t border-border-custom pt-6">
            <Link
              to="/register"
              className="flex w-full items-center justify-center rounded-xl border border-border-custom bg-surface/60 px-6 py-3 text-sm font-semibold text-text-secondary transition hover:border-primary hover:text-text-primary"
            >
              Create a new account
            </Link>
          </div>
        )}

        {state === "pending" && !resendSent && (
          <div className="mt-6 rounded-2xl border border-border-custom bg-surface/40 p-4 text-left">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-muted">
              Did not receive it?
            </p>
            <ul className="space-y-1 text-xs text-text-secondary">
              <li>Check your spam or promotions folder.</li>
              <li>Make sure you entered the correct email above.</li>
              <li>Verification links expire after 24 hours.</li>
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}
