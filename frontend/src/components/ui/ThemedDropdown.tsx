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
        className="flex w-full items-center justify-between gap-2 rounded-full border border-border-custom bg-surface/50 px-4 py-2 text-sm text-text-secondary transition hover:border-primary hover:text-text-primary focus:border-primary focus:outline-none"
      >
        <span className="truncate">{selectedOption?.label}</span>
        <ChevronDown className={`h-4 w-4 shrink-0 transition-transform ${isOpen ? "rotate-180" : ""}`} />
      </button>

      {isOpen && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setIsOpen(false)} />
          <div className={`absolute right-0 z-20 mt-2 max-h-72 min-w-full overflow-y-auto hp-glass-card rounded-2xl border border-border-custom ${menuClassName}`}>
            {options.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`w-full px-4 py-3 text-left text-sm transition ${
                  option.value === value
                    ? "bg-primary/20 font-semibold text-text-primary"
                    : "text-text-secondary hover:bg-white/5 hover:text-text-primary"
                }`}
              >
                <span className="block truncate">{option.label}</span>
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
