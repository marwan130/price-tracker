export type ThemedDropdownOption = {
  value: string;
  label: string;
  disabled?: boolean;
};

export type ThemedDropdownConfig = {
  value: string;
  options: ThemedDropdownOption[];
  onChange: (value: string) => void;
  ariaLabel?: string;
  className?: string;
  name?: string;
  placeholder?: string;
};

export type ThemedDropdownHandle = {
  update: (config: ThemedDropdownConfig) => void;
  destroy: () => void;
};

let nextDropdownId = 0;

export function mountThemedDropdown(
  root: HTMLElement,
  initial: ThemedDropdownConfig,
): ThemedDropdownHandle {
  let config = initial;
  let isOpen = false;
  let optionSignature = '';
  const listId = `dealio-dropdown-${++nextDropdownId}`;
  const trigger = document.createElement('button');
  const label = document.createElement('span');
  const chevron = document.createElement('span');
  const hiddenInput = document.createElement('input');

  root.className = `custom-dropdown ${config.className || ''}`.trim();
  trigger.type = 'button';
  trigger.className = 'custom-dropdown-btn';
  trigger.setAttribute('aria-haspopup', 'listbox');
  trigger.setAttribute('aria-expanded', 'false');
  trigger.setAttribute('aria-controls', listId);
  label.className = 'custom-dropdown-text';
  chevron.className = 'custom-dropdown-chevron';
  chevron.setAttribute('aria-hidden', 'true');
  const chevronIcon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  chevronIcon.setAttribute('class', 'glyph');
  chevronIcon.setAttribute('viewBox', '0 0 24 24');
  chevronIcon.setAttribute('aria-hidden', 'true');
  const chevronPath = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  chevronPath.setAttribute('d', 'm6 9 6 6 6-6');
  chevronPath.setAttribute('fill', 'none');
  chevronPath.setAttribute('stroke', 'currentColor');
  chevronPath.setAttribute('stroke-width', '2');
  chevronPath.setAttribute('stroke-linecap', 'round');
  chevronPath.setAttribute('stroke-linejoin', 'round');
  chevronIcon.append(chevronPath);
  chevron.append(chevronIcon);
  hiddenInput.type = 'hidden';
  root.replaceChildren(trigger, hiddenInput);
  trigger.append(label, chevron);

  const selected = () => config.options.find((option) => option.value === config.value);

  const close = (returnFocus = false) => {
    if (!isOpen) return;
    isOpen = false;
    root.classList.remove('is-open');
    trigger.setAttribute('aria-expanded', 'false');
    root.querySelector('[role="listbox"]')?.remove();
    document.removeEventListener('pointerdown', onOutsidePointer);
    if (returnFocus) trigger.focus();
  };

  const focusOption = (index: number) => {
    const items = [...root.querySelectorAll<HTMLButtonElement>('[role="option"]:not(:disabled)')];
    if (!items.length) return;
    const bounded = Math.max(0, Math.min(index, items.length - 1));
    items[bounded].focus();
    items[bounded].scrollIntoView({ block: 'nearest' });
  };

  const choose = (option: ThemedDropdownOption) => {
    if (option.disabled) return;
    config = { ...config, value: option.value };
    hiddenInput.value = option.value;
    label.textContent = option.label;
    trigger.setAttribute('aria-label', config.ariaLabel || option.label);
    hiddenInput.dispatchEvent(new Event('input', { bubbles: true }));
    hiddenInput.dispatchEvent(new Event('change', { bubbles: true }));
    config.onChange(option.value);
    close(true);
  };

  const onOutsidePointer = (event: PointerEvent) => {
    if (event.target instanceof Node && !root.contains(event.target)) close();
  };

  const open = (focusIndex?: number) => {
    if (isOpen) return;
    isOpen = true;
    root.classList.add('is-open');
    trigger.setAttribute('aria-expanded', 'true');
    const menu = document.createElement('div');
    menu.id = listId;
    menu.className = 'custom-dropdown-menu';
    menu.setAttribute('role', 'listbox');
    menu.setAttribute('aria-label', config.ariaLabel || label.textContent || 'Options');
    for (const option of config.options) {
      const item = document.createElement('button');
      item.type = 'button';
      item.className = `custom-dropdown-option${option.value === config.value ? ' is-selected' : ''}`;
      item.setAttribute('role', 'option');
      item.setAttribute('aria-selected', String(option.value === config.value));
      item.disabled = !!option.disabled;
      item.textContent = option.label;
      if (option.value === config.value) {
        const check = document.createElement('span');
        check.className = 'option-check';
        check.setAttribute('aria-hidden', 'true');
        check.textContent = '✓';
        item.append(check);
      }
      item.addEventListener('click', () => choose(option));
      item.addEventListener('keydown', (event) => {
        const enabled = config.options.filter((entry) => !entry.disabled);
        const current = enabled.findIndex((entry) => entry.value === option.value);
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
          event.preventDefault();
          focusOption(current + (event.key === 'ArrowDown' ? 1 : -1));
        } else if (event.key === 'Home' || event.key === 'End') {
          event.preventDefault();
          focusOption(event.key === 'Home' ? 0 : enabled.length - 1);
        } else if (event.key === 'Escape') {
          event.preventDefault();
          close(true);
        } else if (event.key === 'Tab') {
          close();
        }
      });
      menu.append(item);
    }
    root.append(menu);
    document.addEventListener('pointerdown', onOutsidePointer);
    if (focusIndex !== undefined) requestAnimationFrame(() => focusOption(focusIndex));
  };

  trigger.addEventListener('click', () => isOpen ? close() : open());
  trigger.addEventListener('keydown', (event) => {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      const current = config.options.filter((option) => !option.disabled)
        .findIndex((option) => option.value === config.value);
      const target = current < 0 ? 0 : current + (event.key === 'ArrowDown' ? 1 : -1);
      if (isOpen) focusOption(target);
      else open(target);
    } else if (event.key === 'Escape') {
      close();
    }
  });

  const update = (next: ThemedDropdownConfig) => {
    config = next;
    root.className = `custom-dropdown ${config.className || ''}${isOpen ? ' is-open' : ''}`.trim();
    hiddenInput.name = config.name || '';
    hiddenInput.value = config.value || '';
    const current = selected();
    label.textContent = current?.label || config.placeholder || 'Select';
    trigger.setAttribute('aria-label', config.ariaLabel || label.textContent);
    const signature = config.options.map(({ value, label: text, disabled }) => `${value}\u0000${text}\u0000${!!disabled}`).join('\u0001');
    if (isOpen && signature !== optionSignature) {
      close();
      open();
    }
    optionSignature = signature;
  };

  const destroy = () => {
    close();
    root.replaceChildren();
    root.className = '';
  };

  update(initial);
  return { update, destroy };
}
