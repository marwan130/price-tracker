import { Loader2 } from "lucide-react";

interface LoadingStateProps {
  message?: string;
  className?: string;
  containerClassName?: string;
}

export function LoadingState({
  message = "Loading...",
  className = "min-h-[60vh]",
  containerClassName = "container mx-auto max-w-7xl px-4 py-8",
}: LoadingStateProps) {
  return (
    <div className={containerClassName}>
      <div className={`flex flex-1 items-center justify-center ${className}`}>
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-10 h-10 text-cyan-500 animate-spin" />
          {message && (
            <p className="text-text-secondary text-sm font-medium">{message}</p>
          )}
        </div>
      </div>
    </div>
  );
}
