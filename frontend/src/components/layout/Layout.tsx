import { Outlet, useLocation } from "react-router-dom";
import { useEffect, useRef } from "react";
import { Navbar } from "./Navbar";

const floatingSymbolsData = [
  { sym: "$", x: "8%", top: "15%", size: "2.5rem", dur: "18s", delay: "0s", rot: "-12deg", op: "0.07", color: "#6c63ff" },
  { sym: "€", x: "22%", top: "45%", size: "3.5rem", dur: "22s", delay: "3s", rot: "8deg", op: "0.06", color: "#00d4ff" },
  { sym: "₿", x: "78%", top: "25%", size: "3rem", dur: "20s", delay: "1.5s", rot: "-6deg", op: "0.08", color: "#ffd740" },
  { sym: "£", x: "88%", top: "65%", size: "2.2rem", dur: "16s", delay: "4s", rot: "15deg", op: "0.07", color: "#6c63ff" },
  { sym: "¥", x: "55%", top: "80%", size: "2.8rem", dur: "24s", delay: "2s", rot: "-8deg", op: "0.05", color: "#00d4ff" },
  { sym: "%", x: "42%", top: "35%", size: "2rem", dur: "19s", delay: "5s", rot: "10deg", op: "0.08", color: "#ff6b6b" },
  { sym: "↓", x: "12%", top: "60%", size: "3.2rem", dur: "15s", delay: "0.5s", rot: "0deg", op: "0.1", color: "#00e676" },
  { sym: "↗", x: "65%", top: "50%", size: "2.5rem", dur: "21s", delay: "3.5s", rot: "0deg", op: "0.09", color: "#00e676" },
  { sym: "↓", x: "90%", top: "30%", size: "2rem", dur: "17s", delay: "6s", rot: "5deg", op: "0.08", color: "#ff6b6b" },
];

function FloatingSymbols() {
  return (
    <>
      {floatingSymbolsData.map(({ sym, x, top, size, dur, delay, rot, op, color }, i) => (
        <span
          key={i}
          className="float-symbol"
          style={{
            left: x,
            top,
            fontSize: size,
            color,
            "--fs-dur": dur,
            "--fs-delay": delay,
            "--fs-rot": rot,
            "--fs-op": op,
          } as React.CSSProperties}
        >
          {sym}
        </span>
      ))}
    </>
  );
}

export function Layout() {
  const { pathname } = useLocation();
  const progressRef = useRef<HTMLDivElement>(null);

  // resets scroll position on route change
  useEffect(() => {
    window.scrollTo(0, 0);
    if (progressRef.current) {
      progressRef.current.style.transform = "scaleX(0)";
    }
  }, [pathname]);

  // Reveal animation observer is managed by the single unified effect below

  // computes scroll percentage for progress bar via direct DOM manipulation to avoid React re-renders
  useEffect(() => {
    let ticking = false;
    const onScroll = () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          const el = document.documentElement;
          const total = el.scrollHeight - el.clientHeight;
          const pct = total > 0 ? window.scrollY / total : 0;
          if (progressRef.current) {
            progressRef.current.style.transform = `scaleX(${pct})`;
          }
          ticking = false;
        });
        ticking = true;
      }
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  // sets up scroll triggered reveal animations via intersection observer and mutation observer
  useEffect(() => {
    const reveal = (el: Element) =>
      (el as HTMLElement).classList.add("reveal-visible");

    const io = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            reveal(entry.target);
            io.unobserve(entry.target);
          }
        });
      },
      {
        threshold: 0.05,
        rootMargin: "0px 0px -30px 0px",
      }
    );

    const observeElements = () => {
      document.querySelectorAll(".reveal:not(.reveal-visible)").forEach((el) => {
        io.observe(el);
      });
    };

    // Run initially to observe static components
    observeElements();

    // Set up MutationObserver to watch for newly added reveal elements (e.g. from dynamic search results)
    const mutationObserver = new MutationObserver((mutations) => {
      let hasNewElements = false;
      for (const mutation of mutations) {
        if (mutation.addedNodes.length > 0) {
          hasNewElements = true;
          break;
        }
      }
      if (hasNewElements) {
        observeElements();
      }
    });

    mutationObserver.observe(document.body, {
      childList: true,
      subtree: true,
    });

    return () => {
      io.disconnect();
      mutationObserver.disconnect();
    };
  }, [pathname]);

  return (
    <div className="flex min-h-screen flex-col">
      {/* skip link for accessibility */}
      <a href="#main-content" className="skip-link">
        Skip to main content
      </a>

      {/* procedural noise overlay */}
      <div className="noise-overlay" aria-hidden="true" />

      {/* floating background ambient blobs */}
      <div className="ambient-orb ambient-orb-1" aria-hidden="true" />
      <div className="ambient-orb ambient-orb-2" aria-hidden="true" />
      <div className="ambient-orb ambient-orb-3" aria-hidden="true" />
      <div className="ambient-orb ambient-orb-4" aria-hidden="true" />

      {/* floating currency symbols */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden z-0" aria-hidden="true">
        <FloatingSymbols />
      </div>

      {/* top progress bar tracker */}
      <div
        ref={progressRef}
        id="scroll-progress"
        style={{ transform: "scaleX(0)" }}
        aria-hidden="true"
      />

      {/* fixed overlay allows scrolling content to slide underneath */}
      <div className="fixed inset-x-0 top-0 z-50 flex justify-center pointer-events-none">
        <div className="pointer-events-auto">
          <Navbar />
        </div>
      </div>

      {/* routes placeholder container */}
      <main
        id="main-content"
        className="relative z-10 flex-1 flex flex-col pt-20"
      >
        <Outlet />
      </main>
    </div>
  );
}