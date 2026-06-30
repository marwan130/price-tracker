import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { CheckCircle2, Loader2, Mail, XCircle } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";

type VerifyState = "loading" | "success" | "error";

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const [state, setState] = useState<VerifyState>("loading");
  const [message, setMessage] = useState("Verifying your email...");

  useEffect(() => {
    const token = params.get("token");
    if (!token) {
      setState("error");
      setMessage("Verification link is missing a token.");
      return;
    }

    let active = true;
    apiClient
      .get("/v1/auth/verify-email", { params: { token } })
      .then(() => {
        if (!active) return;
        setState("success");
        setMessage("Your email is verified. You can now log in.");
      })
      .catch((error) => {
        if (!active) return;
        setState("error");
        setMessage(error?.response?.data?.message ?? "This verification link is invalid or expired.");
      });

    return () => {
      active = false;
    };
  }, [params]);

  const Icon = state === "loading" ? Loader2 : state === "success" ? CheckCircle2 : XCircle;

  return (
    <div className="relative flex min-h-[calc(100vh-5rem)] items-center justify-center px-4 py-10">
      <div className="hp-glass-card w-full max-w-md p-8 text-center reveal">
        <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-2xl border border-primary/20 bg-primary/10">
          <Icon className={`h-8 w-8 ${state === "loading" ? "animate-spin text-primary" : state === "success" ? "text-success" : "text-accent-secondary"}`} />
        </div>
        <h1 className="mb-2 text-2xl font-display font-bold text-text-primary">
          {state === "success" ? "Email verified" : state === "error" ? "Verification failed" : "Checking your link"}
        </h1>
        <p className="mb-6 text-sm text-text-secondary">{message}</p>

        {state === "success" ? (
          <Link to="/login" className="btn-ieee inline-flex w-full items-center justify-center rounded-xl bg-primary py-3 text-sm font-bold text-text-primary">
            Log in
          </Link>
        ) : state === "error" ? (
          <div className="space-y-3">
            <Link to="/register" className="btn-ieee inline-flex w-full items-center justify-center rounded-xl bg-primary py-3 text-sm font-bold text-text-primary">
              Register again
            </Link>
            <Link to="/login" className="inline-flex w-full items-center justify-center gap-2 text-sm font-semibold text-text-secondary transition hover:text-text-primary">
              <Mail className="h-4 w-4" />
              Back to login
            </Link>
          </div>
        ) : null}
      </div>
    </div>
  );
}
