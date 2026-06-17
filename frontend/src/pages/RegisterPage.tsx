import { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Mail, User as UserIcon, Phone as PhoneIcon, ShoppingBag, Eye, EyeOff, Loader2, ArrowLeft } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import { useAuthStore } from "@/lib/store/useAuthStore";
import toast from "react-hot-toast";

// Validation schema matching the backend rules
const registerSchema = z.object({
  name: z.string().min(1, "Name is required").max(150, "Name must not exceed 150 characters"),
  email: z.string().min(1, "Email is required").email("Invalid email format").max(255, "Email must not exceed 255 characters"),
  phone: z
    .string()
    .max(13, "Phone must not exceed 13 characters")
    .optional()
    .or(z.literal("")),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters")
    .max(100, "Password must not exceed 100 characters")
    .regex(/[A-Z]/, "Must contain at least one uppercase letter")
    .regex(/[a-z]/, "Must contain at least one lowercase letter")
    .regex(/[0-9]/, "Must contain at least one digit")
    .regex(/[^a-zA-Z0-9]/, "Must contain at least one special character"),
});

type RegisterFields = z.infer<typeof registerSchema>;

interface StrengthIndicator {
  score: number;
  label: string;
  color: string;
  width: string;
}

export function RegisterPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegisterFields>({
    resolver: zodResolver(registerSchema),
  });

  const passwordValue = watch("password", "");
  const [strength, setStrength] = useState<StrengthIndicator>({
    score: 0,
    label: "Empty",
    color: "bg-text-muted",
    width: "w-0",
  });

  // Calculates password strength in real time
  useEffect(() => {
    if (!passwordValue) {
      setStrength({ score: 0, label: "Empty", color: "bg-text-muted", width: "w-0" });
      return;
    }

    let score = 0;
    if (passwordValue.length >= 8) score++;
    if (/[A-Z]/.test(passwordValue)) score++;
    if (/[a-z]/.test(passwordValue)) score++;
    if (/[0-9]/.test(passwordValue)) score++;
    if (/[^a-zA-Z0-9]/.test(passwordValue)) score++;

    let label = "Weak";
    let color = "bg-accent-secondary"; // Red
    let width = "w-1/5";

    if (score === 3 || score === 4) {
      label = "Medium";
      color = "bg-warning"; // Yellow
      width = score === 3 ? "w-3/5" : "w-4/5";
    } else if (score === 5) {
      label = "Strong";
      color = "bg-success"; // Green
      width = "w-full";
    } else if (score === 2) {
      width = "w-2/5";
    }

    setStrength({ score, label, color, width });
  }, [passwordValue]);

  const onSubmit = async (data: RegisterFields) => {
    setIsLoading(true);
    // Remove empty phone field to prevent backend validations issues
    const formattedData = {
      ...data,
      phone: data.phone === "" ? null : data.phone,
    };

    try {
      const res = await apiClient.post("/v1/auth/register", formattedData);

      if (res.data?.success && res.data?.data) {
        const payload = res.data.data;
        setSession(payload.accessToken, payload.refreshToken, {
          userId: payload.userId,
          name: payload.name,
          email: payload.email,
          role: payload.role,
        });
        toast.success("Account created successfully!");
        navigate("/dashboard");
      } else {
        toast.error("Registration failed. Please check details.");
      }
    } catch (err: any) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-[calc(100vh-5rem)] flex items-center justify-center p-4 md:p-8">
      {/* Back to Home Button */}
      <Link
        to="/"
        className="absolute top-4 left-4 md:top-8 md:left-8 flex items-center gap-2 text-sm text-text-secondary hover:text-white transition-colors duration-200 z-20"
      >
        <ArrowLeft className="w-4 h-4" />
        <span>Back to Home</span>
      </Link>

      <div className="w-full max-w-5xl grid grid-cols-1 lg:grid-cols-12 rounded-3xl overflow-hidden hp-glass-card border border-border-custom shadow-2xl min-h-[600px]">

        {/* Left Panel: Form */}
        <div className="lg:col-span-6 p-8 md:p-12 flex flex-col justify-center bg-surface/40 backdrop-blur-md">
          <div className="mb-6 text-center lg:text-left">
            <Link to="/" className="inline-flex items-center gap-2 font-display font-black text-2xl text-white mb-4">
              <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/20 border border-primary/30">
                <ShoppingBag className="h-5 w-5 text-accent" />
              </div>
              <span>SmartTracker</span>
            </Link>
            <h2 className="text-3xl font-display font-bold text-white mb-1">Create Account</h2>
            <p className="text-text-secondary text-sm">Join to track price drops across supported platforms.</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

            {/* Name Field */}
            <div className={`relative ${errors.name ? "animate-shake" : ""}`}>
              <div className="relative">
                <input
                  id="name"
                  type="text"
                  placeholder=" "
                  {...register("name")}
                  className={`peer w-full rounded-xl border ${errors.name ? "border-accent-secondary" : "border-border-custom"
                    } bg-surface/60 px-4 pt-5 pb-2 text-white placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
                />
                <label
                  htmlFor="name"
                  className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
                >
                  Full Name
                </label>
                <UserIcon className="absolute right-4 top-1/2 -translate-y-1/2 w-5 h-5 text-text-muted pointer-events-none" />
              </div>
              {errors.name && (
                <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.name.message}</p>
              )}
            </div>

            {/* Email Field */}
            <div className={`relative ${errors.email ? "animate-shake" : ""}`}>
              <div className="relative">
                <input
                  id="email"
                  type="email"
                  placeholder=" "
                  {...register("email")}
                  className={`peer w-full rounded-xl border ${errors.email ? "border-accent-secondary" : "border-border-custom"
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

            {/* Phone Field */}
            <div className={`relative ${errors.phone ? "animate-shake" : ""}`}>
              <div className="relative">
                <input
                  id="phone"
                  type="text"
                  placeholder=" "
                  {...register("phone")}
                  className={`peer w-full rounded-xl border ${errors.phone ? "border-accent-secondary" : "border-border-custom"
                    } bg-surface/60 px-4 pt-5 pb-2 text-white placeholder-transparent focus:border-primary focus:bg-surface/90 focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all`}
                />
                <label
                  htmlFor="phone"
                  className="absolute left-4 top-1.5 text-xs text-text-muted transition-all peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-sm peer-placeholder-shown:text-text-secondary peer-focus:top-1.5 peer-focus:text-xs peer-focus:text-primary-light pointer-events-none origin-left"
                >
                  Phone Number (Optional)
                </label>
                <PhoneIcon className="absolute right-4 top-1/2 -translate-y-1/2 w-5 h-5 text-text-muted pointer-events-none" />
              </div>
              {errors.phone && (
                <p className="mt-1 text-xs text-accent-secondary pl-1 font-semibold">{errors.phone.message}</p>
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
                  className={`peer w-full rounded-xl border ${errors.password ? "border-accent-secondary" : "border-border-custom"
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

              {/* Password Strength Indicator */}
              <div className="mt-3">
                <div className="flex items-center justify-between text-xs mb-1 font-semibold">
                  <span className="text-text-secondary">Password Strength:</span>
                  <span
                    className={`font-bold ${strength.score <= 2
                        ? "text-accent-secondary"
                        : strength.score <= 4
                          ? "text-warning"
                          : "text-success"
                      }`}
                  >
                    {strength.label}
                  </span>
                </div>
                <div className="h-1.5 w-full bg-surface-elevated rounded-full overflow-hidden">
                  <div className={`h-full ${strength.color} ${strength.width} transition-all duration-300`} />
                </div>
                <p className="text-[10px] text-text-muted mt-1 leading-relaxed">
                  Requires 8+ characters, uppercase, lowercase, number, and special character.
                </p>
              </div>

              {errors.password && (
                <p className="mt-2 text-xs text-accent-secondary pl-1 font-semibold">{errors.password.message}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="btn-ieee btn-shimmer w-full py-3.5 bg-primary text-white font-bold rounded-xl flex items-center justify-center gap-2 shadow-[0_8px_24px_rgba(108,99,255,0.25)] disabled:opacity-50 mt-2"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <span>Registering Account...</span>
                </>
              ) : (
                <span>Register Account</span>
              )}
            </button>

          </form>

          <div className="mt-6 text-center text-sm text-text-secondary font-semibold">
            Already have an account?{" "}
            <Link to="/login" className="text-primary-light hover:text-accent font-bold transition-colors">
              Log In
            </Link>
          </div>
        </div>

        {/* Right Panel: Illustration/Description */}
        <div className="lg:col-span-6 relative overflow-hidden hidden lg:flex flex-col justify-between p-12 border-l border-border-custom bg-surface-elevated/20">
          {/* Decorative neon highlights */}
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_70%_30%,rgba(0,212,255,0.1),transparent_50%)]" aria-hidden="true" />
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_80%,rgba(108,99,255,0.12),transparent_50%)]" aria-hidden="true" />

          <div className="relative z-10">
            <span className="px-3 py-1 rounded-full bg-primary/15 border border-primary/30 text-xs font-bold text-primary-light">
              Interactive Tools
            </span>
          </div>

          <div className="relative z-10 my-auto text-left max-w-sm space-y-6">
            <h3 className="text-3xl font-display font-black text-white leading-tight">
              Begin Tracking in under 60 seconds
            </h3>
            <p className="text-text-secondary text-sm leading-relaxed">
              Create alert thresholds, verify historical charts, and configure real-time notifications all from a single dashboard.
            </p>
            <div className="border-t border-border-custom pt-6 flex items-center gap-4">
              <div className="flex -space-x-2">
                <div className="w-8 h-8 rounded-full bg-primary/40 border border-background flex items-center justify-center text-[10px] font-bold text-white">U1</div>
                <div className="w-8 h-8 rounded-full bg-accent/40 border border-background flex items-center justify-center text-[10px] font-bold text-white">U2</div>
                <div className="w-8 h-8 rounded-full bg-success/40 border border-background flex items-center justify-center text-[10px] font-bold text-white">U3</div>
              </div>
              <span className="text-xs text-text-secondary font-semibold">Join 10k+ shoppers today</span>
            </div>
          </div>

        </div>

      </div>
    </div>
  );
}
