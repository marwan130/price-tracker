import { useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { ArrowRight, TrendingDown, Bell, Search, Sparkles, Activity } from "lucide-react";
import { gsap } from "gsap";

// Types
interface TickerItem {
  name: string;
  price: string;
  change: string;
  isPositive: boolean;
}

interface FeatureCardProps {
  icon: React.ReactNode;
  title: string;
  description: string;
  glowColor: string;
}

// Mock ticker items
const tickerItems: TickerItem[] = [
  { name: "iPhone 15 Pro Max", price: "$1,099", change: "-15%", isPositive: false },
  { name: "Sony WH-1000XM5", price: "$298", change: "-25%", isPositive: false },
  { name: "RTX 4090 Founders Edition", price: "$1,599", change: "+4%", isPositive: true },
  { name: "MacBook Pro M3 Max", price: "$2,899", change: "-10%", isPositive: false },
  { name: "Nintendo Switch OLED", price: "$319", change: "-9%", isPositive: false },
  { name: "PlayStation 5 Slim", price: "$449", change: "-10%", isPositive: false },
  { name: "iPad Air M2", price: "$549", change: "-12%", isPositive: false },
];

function FeaturesGrid() {
  return (
    <section className="relative py-24 px-6 md:px-12 max-w-7xl mx-auto z-10">
      <div className="text-center mb-16 reveal">
        <h2 className="text-4xl md:text-5xl font-display font-black text-white mb-4">
          Advanced Tracking Features
        </h2>
        <p className="text-text-secondary text-lg max-w-xl mx-auto">
          We use real-time scrapers and alert triggers so you never miss a deal again.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
        <div className="reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
          <FeatureCard
            icon={<Search className="w-8 h-8 text-accent" />}
            title="Instant Search Scrapers"
            description="Direct connection with global retail APIs ensures we scrape the most up-to-date prices in seconds."
            glowColor="rgba(0, 212, 255, 0.4)"
          />
        </div>
        <div className="reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <FeatureCard
            icon={<Bell className="w-8 h-8 text-primary" />}
            title="Instant Price Alerts"
            description="Get immediate push or email notifications the exact moment a product drops below your target threshold."
            glowColor="rgba(108, 99, 255, 0.4)"
          />
        </div>
        <div className="reveal" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
          <FeatureCard
            icon={<TrendingDown className="w-8 h-8 text-success" />}
            title="Historical Trend Graphs"
            description="Examine interactive historical charts to determine if current prices are actually a deal."
            glowColor="rgba(0, 230, 118, 0.4)"
          />
        </div>
      </div>
    </section>
  );
}

function FeatureCard({ icon, title, description, glowColor }: FeatureCardProps) {
  const cardRef = useRef<HTMLDivElement>(null);
  const rectRef = useRef<DOMRect | null>(null);

  const handleMouseEnter = (e: React.MouseEvent<HTMLDivElement>) => {
    rectRef.current = e.currentTarget.getBoundingClientRect();
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    const card = cardRef.current;
    if (!card) return;

    // Use cached rect to prevent layout reflows on every pixel of mouse movement
    const rect = rectRef.current || card.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    const xc = rect.width / 2;
    const yc = rect.height / 2;
    // Maximum rotate 12deg
    const rotateX = ((yc - y) / yc) * 12;
    const rotateY = ((x - xc) / xc) * 12;

    card.style.transform = `perspective(800px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.02, 1.02, 1.02)`;
    card.style.setProperty("--glow-x", `${x}px`);
    card.style.setProperty("--glow-y", `${y}px`);
  };

  const handleMouseLeave = () => {
    const card = cardRef.current;
    if (!card) return;
    rectRef.current = null; // Reset cache
    card.style.transform = "perspective(800px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)";
  };

  return (
    <div
      ref={cardRef}
      onMouseEnter={handleMouseEnter}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      style={{
        transition: "transform 0.15s cubic-bezier(0.25, 0.46, 0.45, 0.94), border-color 0.3s ease",
      }}
      className="relative overflow-hidden hp-glass-card p-8 group border border-border-custom cursor-pointer flex flex-col items-start text-left"
    >
      {/* 3D hover radial glow gradient */}
      <div
        className="absolute pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity duration-300 rounded-full w-56 h-56 -translate-x-1/2 -translate-y-1/2"
        style={{
          left: "var(--glow-x, 0px)",
          top: "var(--glow-y, 0px)",
          background: `radial-gradient(circle, ${glowColor} 0%, transparent 70%)`,
        }}
      />

      <div className="mb-6 p-4 rounded-2xl bg-white/5 border border-white/10 z-10">
        {icon}
      </div>

      <h3 className="text-2xl font-display font-bold text-white mb-3 z-10 group-hover:text-accent transition-colors duration-300">
        {title}
      </h3>

      <p className="text-text-secondary z-10 leading-relaxed">
        {description}
      </p>
    </div>
  );
}

function SelfDrawingSteps() {
  const sectionRef = useRef<HTMLDivElement>(null);
  const pathRef = useRef<SVGPathElement>(null);

  useEffect(() => {
    const path = pathRef.current;
    if (!path) return;
    const length = path.getTotalLength();
    path.style.strokeDasharray = `${length} ${length}`;
    path.style.strokeDashoffset = `${length}`;

    let ticking = false;
    const handleScroll = () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          const section = sectionRef.current;
          if (!section || !path) {
            ticking = false;
            return;
          }
          const rect = section.getBoundingClientRect();
          const windowHeight = window.innerHeight;

          // Scroll progress mapping
          const sectionTop = rect.top;
          const sectionHeight = rect.height;

          // Calculate how far through the section the scroll is (0 to 1)
          const startTrigger = windowHeight * 0.40;
          const endTrigger = windowHeight * 0.15;
          const progress = Math.min(
            1,
            Math.max(0, (startTrigger - sectionTop) / (sectionHeight - endTrigger))
          );

          path.style.strokeDashoffset = `${length - progress * length}`;
          ticking = false;
        });
        ticking = true;
      }
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    // Trigger once to init
    handleScroll();

    return () => {
      window.removeEventListener("scroll", handleScroll);
    };
  }, []);

  return (
    <section ref={sectionRef} className="relative py-24 px-6 md:px-12 max-w-6xl mx-auto z-10">
      <div className="text-center mb-20 reveal">
        <h2 className="text-4xl md:text-5xl font-display font-black text-white mb-4">
          How It Works
        </h2>
        <p className="text-text-secondary text-lg">
          Zero friction. Just paste, configure, and wait for the notification.
        </p>
      </div>

      <div className="relative grid grid-cols-1 md:grid-cols-3 gap-16 md:gap-8 min-h-[300px]">
        {/* Draw Line Background (Desktop-only) */}
        <div className="hidden md:block absolute inset-0 -z-10 pointer-events-none" aria-hidden="true">
          <svg className="w-full h-full" viewBox="0 0 900 200" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              ref={pathRef}
              d="M 150 160 Q 300 80, 450 140 T 750 160"
              stroke="url(#line-gradient)"
              strokeWidth="3"
              strokeLinecap="round"
              strokeDasharray="12 6"
            />
            <defs>
              <linearGradient id="line-gradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#6c63ff" />
                <stop offset="50%" stopColor="#00d4ff" />
                <stop offset="100%" stopColor="#00e676" />
              </linearGradient>
            </defs>
          </svg>
        </div>

        {/* Step 1 */}
        <div className="flex flex-col items-center text-center px-4 reveal reveal-left" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
          <div className="w-16 h-16 rounded-full bg-primary/20 border border-primary/40 flex items-center justify-center text-xl font-bold font-display text-white mb-6 shadow-[0_0_20px_rgba(108,99,255,0.3)]">
            1
          </div>
          <h3 className="text-xl font-bold text-white mb-2">Paste URL</h3>
          <p className="text-text-secondary">
            Copy the product URL from any supported retail store and search it.
          </p>
        </div>

        {/* Step 2 */}
        <div className="flex flex-col items-center text-center px-4 reveal reveal-up" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <div className="w-16 h-16 rounded-full bg-accent/20 border border-accent/40 flex items-center justify-center text-xl font-bold font-display text-white mb-6 shadow-[0_0_20px_rgba(0,212,255,0.3)]">
            2
          </div>
          <h3 className="text-xl font-bold text-white mb-2">Set Threshold</h3>
          <p className="text-text-secondary">
            Define your ideal target price and choose alert channel options.
          </p>
        </div>

        {/* Step 3 */}
        <div className="flex flex-col items-center text-center px-4 reveal reveal-right" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
          <div className="w-16 h-16 rounded-full bg-success/20 border border-success/40 flex items-center justify-center text-xl font-bold font-display text-white mb-6 shadow-[0_0_20px_rgba(0,230,118,0.3)]">
            3
          </div>
          <h3 className="text-xl font-bold text-white mb-2">Save Money</h3>
          <p className="text-text-secondary">
            Sit back! We'll ping you immediately when the threshold is hit.
          </p>
        </div>
      </div>
    </section>
  );
}

function StatsRow() {
  const statsContainerRef = useRef<HTMLDivElement>(null);
  const alertsRef = useRef<HTMLDivElement>(null);
  const savedRef = useRef<HTMLDivElement>(null);
  const activeRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const statsContainer = statsContainerRef.current;
    if (!statsContainer) return;

    const targetStats = { alerts: 14250, saved: 32540, active: 8900 };
    const counterObj = { alerts: 0, saved: 0, active: 0 };

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            // Animate using GSAP directly via DOM refs to avoid React re-renders on every animation frame
            gsap.to(counterObj, {
              alerts: targetStats.alerts,
              saved: targetStats.saved,
              active: targetStats.active,
              duration: 2.5,
              ease: "power3.out",
              onUpdate: () => {
                if (alertsRef.current) {
                  alertsRef.current.innerText = Math.floor(counterObj.alerts).toLocaleString() + "+";
                }
                if (savedRef.current) {
                  savedRef.current.innerText = "$" + Math.floor(counterObj.saved).toLocaleString() + "+";
                }
                if (activeRef.current) {
                  activeRef.current.innerText = Math.floor(counterObj.active).toLocaleString() + "+";
                }
              },
            });
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.15 }
    );

    observer.observe(statsContainer);
    return () => observer.disconnect();
  }, []);

  return (
    <section
      ref={statsContainerRef}
      className="py-16 border-y border-border-custom bg-surface-elevated/40 backdrop-blur-md relative z-10"
    >
      <div className="max-w-6xl mx-auto px-6 grid grid-cols-1 md:grid-cols-3 gap-12 text-center">
        <div>
          <div
            ref={alertsRef}
            className="text-5xl md:text-6xl font-display font-black text-accent mb-2"
          >
            0+
          </div>
          <div className="text-text-secondary font-semibold uppercase tracking-wider text-xs">
            Alerts Triggered
          </div>
        </div>
        <div>
          <div
            ref={savedRef}
            className="text-5xl md:text-6xl font-display font-black text-success mb-2"
          >
            $0+
          </div>
          <div className="text-text-secondary font-semibold uppercase tracking-wider text-xs">
            Total USD Saved
          </div>
        </div>
        <div>
          <div
            ref={activeRef}
            className="text-5xl md:text-6xl font-display font-black text-primary-light mb-2"
          >
            0+
          </div>
          <div className="text-text-secondary font-semibold uppercase tracking-wider text-xs">
            Active Trackings
          </div>
        </div>
      </div>
    </section>
  );
}

export function LandingPage() {
  return (
    <div className="relative w-full overflow-hidden">
      {/* Hero Section */}
      <section className="relative pt-24 pb-20 px-6 md:px-12 flex flex-col items-center justify-center text-center max-w-5xl mx-auto z-10 min-h-[85vh]">
        {/* Glow badge overlay */}
        <div className="reveal flex items-center gap-1.5 px-4 py-1.5 rounded-full bg-primary/10 border border-primary/30 text-xs font-semibold text-primary-light mb-8 shadow-[0_0_15px_rgba(108,99,255,0.15)] animate-pulse-slow">
          <Sparkles className="w-3.5 h-3.5" />
          <span>Smart real-time tracker</span>
        </div>

        <h1 className="reveal stagger-1 text-5xl md:text-7xl font-display font-black text-white tracking-tight leading-[1.15] mb-6 max-w-4xl" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
          Never Pay Full Price For <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary to-accent">Anything</span> Again
        </h1>

        <p className="reveal stagger-2 text-text-secondary text-lg md:text-xl font-sans max-w-2xl mb-10 leading-relaxed" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          Monitor your favorite products across major ecommerce stores. Get instant notifications when prices drop and buy at their lowest.
        </p>

        <div className="reveal stagger-3 flex flex-col sm:flex-row items-center gap-4 justify-center" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
          <Link
            to="/register"
            className="btn-ieee btn-shimmer w-full sm:w-auto px-8 py-4 bg-primary text-white font-bold rounded-full shadow-[0_8px_32px_-4px_rgba(108,99,255,0.4)] flex items-center justify-center gap-2 hover:brightness-110"
          >
            <span>Get Started Free</span>
            <ArrowRight className="w-5 h-5" />
          </Link>
          <a
            href="#how-it-works"
            onClick={(e) => {
              e.preventDefault();
              document.getElementById("how-it-works")?.scrollIntoView({ behavior: "smooth" });
            }}
            className="w-full sm:w-auto px-8 py-4 border border-border-custom bg-white/5 hover:bg-white/10 text-white font-bold rounded-full transition-all duration-300 flex items-center justify-center"
          >
            See How it Works
          </a>
        </div>

        {/* Decorative Floating Cards (Drift Parallax) */}
        <div className="relative w-full max-w-4xl mt-20 hidden md:block">
          <div
            className="absolute left-[5%] top-0 hp-glass-card p-4 border border-border-custom w-52 flex items-center gap-3 animate-float pointer-events-none"
            style={{ animationDuration: "12s" }}
          >
            <div className="w-10 h-10 rounded-xl bg-success/20 flex items-center justify-center text-success">
              <TrendingDown className="w-5 h-5" />
            </div>
            <div className="text-left">
              <div className="text-xs text-text-secondary font-semibold">RTX 4080</div>
              <div className="text-sm font-bold text-white font-mono">-24% Drop</div>
            </div>
          </div>

          <div
            className="absolute right-[8%] top-[-30px] hp-glass-card p-4 border border-border-custom w-56 flex items-center gap-3 animate-float pointer-events-none"
            style={{ animationDuration: "16s", animationDelay: "-4s" }}
          >
            <div className="w-10 h-10 rounded-xl bg-accent/20 flex items-center justify-center text-accent">
              <Activity className="w-5 h-5" />
            </div>
            <div className="text-left">
              <div className="text-xs text-text-secondary font-semibold">Active Alerts</div>
              <div className="text-sm font-bold text-white font-mono">Real-time update</div>
            </div>
          </div>
        </div>
      </section>

      {/* Infinite Horizontal Price Ticker Tape */}
      <div className="w-full overflow-hidden py-6 bg-surface-elevated/40 border-y border-border-custom relative z-10 select-none">
        <div className="flex gap-12 animate-[marquee_28s_linear_infinite] whitespace-nowrap w-max">
          {/* Double list for smooth loop wrapping */}
          {[...tickerItems, ...tickerItems].map((item, idx) => (
            <div key={idx} className="inline-flex items-center gap-3 text-sm font-medium">
              <span className="text-text-primary font-display font-semibold">{item.name}</span>
              <span className="text-white font-mono">{item.price}</span>
              <span
                className={`font-mono text-xs px-2 py-0.5 rounded-full ${item.isPositive ? "bg-accent-secondary/15 text-accent-secondary" : "bg-success/15 text-success"
                  }`}
              >
                {item.change}
              </span>
              <span className="text-text-muted font-light px-2">|</span>
            </div>
          ))}
        </div>
      </div>

      {/* Features Grid */}
      <FeaturesGrid />

      {/* Stats Divider Row */}
      <StatsRow />

      {/* How It Works with self drawing lines */}
      <div id="how-it-works">
        <SelfDrawingSteps />
      </div>

      {/* CTA Footer Wrapper */}
      <section className="relative py-24 px-6 md:px-12 max-w-5xl mx-auto text-center z-10">
        <div className="hp-glass-card p-12 md:p-16 border border-border-custom relative overflow-hidden flex flex-col items-center reveal">
          {/* Radial decorative gradient */}
          <div
            className="absolute inset-0 bg-[radial-gradient(circle_at_50%_120%,rgba(108,99,255,0.15),transparent_50%)]"
            aria-hidden="true"
          />

          <h2 className="text-3xl md:text-5xl font-display font-black text-white mb-6 relative z-10">
            Stop Overpaying Today
          </h2>
          <p className="text-text-secondary text-lg max-w-xl mb-8 relative z-10">
            Join thousands of smart shoppers who track product prices dynamically.
          </p>

          <Link
            to="/register"
            className="btn-ieee btn-shimmer px-8 py-4 bg-primary text-white font-bold rounded-full shadow-lg relative z-10 inline-flex items-center gap-2 hover:brightness-110"
          >
            <span>Start Tracking Now</span>
            <ArrowRight className="w-5 h-5" />
          </Link>
        </div>
      </section>

      {/* Global CSS Inject for Marquee */}
      <style>{`
        @keyframes marquee {
          0% {
            transform: translate3d(0, 0, 0);
          }
          100% {
            transform: translate3d(-50%, 0, 0);
          }
        }
      `}</style>
    </div>
  );
}
