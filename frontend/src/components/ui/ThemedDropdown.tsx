import { useState } from "react";
import { ChevronDown } from "lucide-react";

interface DropdownOption<T extends string> {
  value: T;
  label: string;
}

interface ThemedDropdownProps<T extends string> {
  value: T;
  options: DropdownOption<T>[];
  onChange: (value: T) => void;
  className?: string;
  menuClassName?: string;
}

export function ThemedDropdown<T extends string>({
  value,
  options,
  onChange,
  className = "",
  menuClassName = "",
}: ThemedDropdownProps<T>) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = options.find((option) => option.value === value) ?? options[0];

  return (
    <div className={`relative ${className}`}>
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        className="flex w-full items-center justify-between gap-2 rounded-full border border-border-custom bg-surface/40 backdrop-blur-md px-4 py-2 text-sm text-text-secondary transition-all duration-300 hover:border-primary hover:bg-primary/15 hover:text-text-primary focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/50 shadow-lg"
      >
        <span className="truncate font-medium">{selectedOption?.label}</span>
        <ChevronDown className={`h-4 w-4 shrink-0 transition-transform duration-300 text-primary ${isOpen ? "rotate-180" : ""}`} />
      </button>

      {isOpen && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setIsOpen(false)} />
          <div className={`absolute right-0 z-20 mt-2 min-w-fit overflow-y-auto hp-glass-card rounded-2xl border border-border-custom shadow-2xl shadow-primary/40 backdrop-blur-xl bg-surface/60 transition-all duration-300 animate-in fade-in slide-in-from-top-2 ${menuClassName}`}>
            {options.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`w-full px-4 py-2.5 text-left text-sm transition-all duration-200 border-b border-border-custom/30 last:border-b-0 ${
                  option.value === value
                    ? "bg-primary/40 font-semibold text-text-primary shadow-inner"
                    : "text-text-secondary hover:bg-primary/25 hover:text-text-primary"
                }`}
              >
                <span className="block truncate font-medium">{option.label}</span>
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
