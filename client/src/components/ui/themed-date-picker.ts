export type ThemedDatePickerConfig = {
  input: HTMLInputElement;
  placeholder: string;
  locale?: string;
};

export function mountThemedDatePicker({ input, placeholder, locale = 'en' }: ThemedDatePickerConfig) {
  const host = document.createElement('div');
  host.className = 'dealio-date-picker';
  input.before(host);
  host.append(input);
  input.hidden = true;
  input.tabIndex = -1;
  input.setAttribute('aria-hidden', 'true');

  const trigger = document.createElement('button');
  trigger.type = 'button';
  trigger.className = 'dealio-date-trigger';
  trigger.setAttribute('aria-haspopup', 'dialog');
  trigger.setAttribute('aria-expanded', 'false');
  trigger.setAttribute('aria-label', placeholder);
  trigger.textContent = placeholder;
  host.append(trigger);

  let open = false;
  let view = parseValue(input.value) || new Date();
  let panel: HTMLDivElement | null = null;
  let timeList: HTMLDivElement | null = null;

  const dateFormatter = new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short', day: 'numeric' });
  const monthFormatter = new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'long' });
  const timeFormatter = new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit' });

  function parseValue(value: string) {
    const match = /^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2}))?$/.exec(value);
    return match ? new Date(+match[1], +match[2] - 1, +match[3], +(match[4] || 0), +(match[5] || 0)) : null;
  }

  function updateValue(date: Date) {
    const pad = (value: number) => String(value).padStart(2, '0');
    const next = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    input.value = next;
    trigger.textContent = `${dateFormatter.format(date)} · ${timeFormatter.format(date)}`;
    trigger.setAttribute('aria-label', trigger.textContent);
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function closeTimeList() {
    timeList?.remove();
    timeList = null;
    panel?.querySelector<HTMLButtonElement>('.dealio-calendar-time-trigger')?.setAttribute('aria-expanded', 'false');
  }

  function close() {
    if (!open) return;
    open = false;
    trigger.setAttribute('aria-expanded', 'false');
    closeTimeList();
    panel?.remove();
    panel = null;
    host.classList.remove('is-open');
    document.removeEventListener('pointerdown', outside);
    document.removeEventListener('keydown', escape);
    window.removeEventListener('resize', placePanel);
    window.removeEventListener('scroll', placePanel, true);
    window.removeEventListener('resize', repositionTimeList);
    window.removeEventListener('scroll', repositionTimeList, true);
  }

  function outside(event: PointerEvent) {
    if (event.target instanceof Node && !host.contains(event.target) && !panel?.contains(event.target) && !timeList?.contains(event.target)) close();
  }

  function placePanel() {
    if (!panel) return;
    const rect = trigger.getBoundingClientRect();
    const width = Math.min(320, window.innerWidth - 24);
    const height = panel.getBoundingClientRect().height;
    const left = Math.max(12, Math.min(rect.left, window.innerWidth - width - 12));
    const below = window.innerHeight - rect.bottom - 8;
    const above = rect.top - 8;
    const maxTop = Math.max(12, window.innerHeight - Math.min(height, window.innerHeight - 24) - 12);
    const top = below >= height || below >= above
      ? Math.max(12, Math.min(rect.bottom + 4, maxTop))
      : Math.max(12, rect.top - height - 4);
    panel.style.width = `${width}px`;
    panel.style.left = `${left}px`;
    panel.style.top = `${top}px`;
  }

  function placeTimeList(anchor: HTMLButtonElement) {
    if (!timeList) return;
    const rect = anchor.getBoundingClientRect();
    const width = 150;
    const height = Math.min(timeList.scrollHeight, 260);
    const left = Math.max(12, Math.min(rect.left, window.innerWidth - width - 12));
    const below = window.innerHeight - rect.bottom - 8;
    const above = rect.top - 8;
    const top = below >= height || below >= above
      ? Math.max(12, Math.min(rect.bottom + 4, window.innerHeight - Math.min(height, window.innerHeight - 24) - 12))
      : Math.max(12, rect.top - height - 4);
    timeList.style.left = `${left}px`;
    timeList.style.top = `${top}px`;
  }

  function repositionTimeList() {
    if (!timeList || !panel) return;
    const button = panel.querySelector<HTMLButtonElement>('.dealio-calendar-time-trigger');
    if (button) placeTimeList(button);
  }

  function escape(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      close();
      trigger.focus();
    }
  }

  function render() {
    if (!panel) return;
    panel.replaceChildren();
    const heading = document.createElement('div');
    heading.className = 'dealio-calendar-heading';
    const previous = document.createElement('button');
    previous.type = 'button';
    previous.className = 'dealio-calendar-nav';
    previous.setAttribute('aria-label', locale.startsWith('ar') ? 'الشهر السابق' : 'Previous month');
    previous.textContent = '‹';
    previous.addEventListener('click', () => { closeTimeList(); view = new Date(view.getFullYear(), view.getMonth() - 1, 1, view.getHours(), view.getMinutes()); render(); });
    const title = document.createElement('strong');
    title.textContent = monthFormatter.format(view);
    const next = document.createElement('button');
    next.type = 'button';
    next.className = 'dealio-calendar-nav';
    next.setAttribute('aria-label', locale.startsWith('ar') ? 'الشهر التالي' : 'Next month');
    next.textContent = '›';
    next.addEventListener('click', () => { closeTimeList(); view = new Date(view.getFullYear(), view.getMonth() + 1, 1, view.getHours(), view.getMinutes()); render(); });
    heading.append(previous, title, next);
    panel.append(heading);

    const grid = document.createElement('div');
    grid.className = 'dealio-calendar-grid';
    grid.setAttribute('role', 'grid');
    const weekStart = locale.startsWith('ar') ? 6 : 0;
    for (let index = 0; index < 7; index++) {
      const weekday = document.createElement('span');
      weekday.className = 'dealio-calendar-weekday';
      weekday.textContent = new Intl.DateTimeFormat(locale, { weekday: 'short' }).format(new Date(2024, 0, 7 + ((index + weekStart) % 7)));
      weekday.setAttribute('aria-hidden', 'true');
      grid.append(weekday);
    }
    const first = new Date(view.getFullYear(), view.getMonth(), 1);
    const offset = (first.getDay() - weekStart + 7) % 7;
    const selected = parseValue(input.value);
    for (let index = 0; index < 42; index++) {
      const date = new Date(view.getFullYear(), view.getMonth(), index - offset + 1, view.getHours(), view.getMinutes());
      const day = document.createElement('button');
      day.type = 'button';
      day.className = 'dealio-calendar-day';
      day.textContent = String(date.getDate());
      day.setAttribute('aria-label', dateFormatter.format(date));
      day.setAttribute('aria-pressed', String(!!selected && date.toDateString() === selected.toDateString()));
      if (date.getMonth() !== view.getMonth()) day.classList.add('is-outside-month');
      if (date.toDateString() === new Date().toDateString()) day.classList.add('is-today');
      day.addEventListener('click', () => {
        closeTimeList();
        view = date;
        updateValue(date);
        render();
        placePanel();
      });
      grid.append(day);
    }
    panel.append(grid);

    const timeRow = document.createElement('label');
    timeRow.className = 'dealio-calendar-time';
    const timeLabel = document.createElement('span');
    timeLabel.textContent = locale.startsWith('ar') ? 'الوقت' : 'Time';
    const timeTrigger = document.createElement('button');
    timeTrigger.type = 'button';
    timeTrigger.className = 'dealio-calendar-time-trigger';
    timeTrigger.textContent = timeFormatter.format(view);
    timeTrigger.setAttribute('aria-haspopup', 'listbox');
    timeTrigger.setAttribute('aria-expanded', 'false');
    timeTrigger.addEventListener('click', () => {
      if (timeList) {
        closeTimeList();
        return;
      }
      timeList = document.createElement('div');
      timeList.className = 'dealio-time-list';
      timeList.setAttribute('role', 'listbox');
      timeList.setAttribute('aria-label', timeLabel.textContent || 'Time');
      const selectedTime = `${String(view.getHours()).padStart(2, '0')}:${String(view.getMinutes()).padStart(2, '0')}`;
      const times = new Set<string>([selectedTime]);
      for (let hour = 0; hour < 24; hour++) {
        for (let minute = 0; minute < 60; minute += 5) {
          times.add(`${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`);
        }
      }
      for (const value of [...times].sort()) {
        const option = document.createElement('button');
        option.type = 'button';
        option.className = 'dealio-time-option';
        option.setAttribute('role', 'option');
        option.setAttribute('aria-selected', String(value === selectedTime));
        const [hours, minutes] = value.split(':').map(Number);
        option.textContent = timeFormatter.format(new Date(view.getFullYear(), view.getMonth(), view.getDate(), hours, minutes));
        option.addEventListener('click', () => {
          closeTimeList();
          view.setHours(hours, minutes, 0, 0);
          updateValue(view);
          render();
          placePanel();
        });
        timeList.append(option);
      }
      document.body.append(timeList);
      timeTrigger.setAttribute('aria-expanded', 'true');
      placeTimeList(timeTrigger);
      timeList.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: 'nearest' });
    });
    timeRow.append(timeLabel, timeTrigger);
    panel.append(timeRow);
  }

  function show() {
    if (open) return;
    open = true;
    view = parseValue(input.value) || new Date();
    panel = document.createElement('div');
    panel.className = 'dealio-calendar-panel';
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', placeholder);
    document.body.append(panel);
    host.classList.add('is-open');
    trigger.setAttribute('aria-expanded', 'true');
    render();
    placePanel();
    document.addEventListener('pointerdown', outside);
    document.addEventListener('keydown', escape);
    window.addEventListener('resize', placePanel);
    window.addEventListener('scroll', placePanel, true);
    window.addEventListener('resize', repositionTimeList);
    window.addEventListener('scroll', repositionTimeList, true);
  }

  trigger.addEventListener('click', () => open ? close() : show());
  if (input.value) {
    const initial = parseValue(input.value);
    if (initial) trigger.textContent = `${dateFormatter.format(initial)} · ${timeFormatter.format(initial)}`;
  }
  return {
    destroy() {
      close();
      input.hidden = false;
      input.removeAttribute('aria-hidden');
      host.before(input);
      host.remove();
    }
  };
}
