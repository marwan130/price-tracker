import { mountThemedDropdown, type ThemedDropdownConfig, type ThemedDropdownHandle, type ThemedDropdownOption } from './components/ui/themed-dropdown';
import { mountThemedDatePicker } from './components/ui/themed-date-picker';

type Envelope<T> = { data: T };
type Tracking = { productId: string; productName: string };
type Store = { storeId: string; name: string; country: string; isActive: boolean };

const api = import.meta.env.VITE_API_URL || '';
const getCopy = () => document.documentElement.lang.toLowerCase().startsWith('ar') ? {
  title: 'تصدير جزء من البيانات', description: 'اختياري: حدّد منتجات أو متجراً أو سوقاً وفترة زمنية لتصدير البيانات المرتبطة بها.',
  product: 'المنتج', anyProduct: 'كل المنتجات', store: 'المتجر', anyStore: 'كل المتاجر', market: 'السوق', anyMarket: 'كل الأسواق',
  from: 'من التاريخ والوقت', to: 'إلى التاريخ والوقت', csv: 'تنزيل CSV المحدد', json: 'تنزيل JSON المحدد', loading: 'جارٍ تجهيز عوامل التصفية…',
  exporting: 'جارٍ تجهيز التصدير…', done: 'تم تنزيل التصدير المحدد.', error: 'تعذر تجهيز التصدير المحدد. حاول مرة أخرى.', empty: 'لا توجد منتجات أو متاجر متاحة للتصفية.'
} : {
  title: 'Export a selection', description: 'Optional: limit this export by product, store, market, and date range.',
  product: 'Product', anyProduct: 'All products', store: 'Store', anyStore: 'All stores', market: 'Market', anyMarket: 'All markets',
  from: 'From date and time', to: 'To date and time', csv: 'Download filtered CSV', json: 'Download filtered JSON', loading: 'Loading filter options…',
  exporting: 'Preparing export…', done: 'Filtered export downloaded.', error: 'Could not prepare the filtered export. Try again.', empty: 'No products or stores are available to filter.'
};
let copy = getCopy();
let pendingValues: Record<string, string> = {};

const markets = [
  ['AE', 'United Arab Emirates', 'الإمارات العربية المتحدة'], ['BH', 'Bahrain', 'البحرين'],
  ['EG', 'Egypt', 'مصر'], ['JO', 'Jordan', 'الأردن'], ['KW', 'Kuwait', 'الكويت'],
  ['OM', 'Oman', 'عُمان'], ['QA', 'Qatar', 'قطر'], ['SA', 'Saudi Arabia', 'السعودية'], ['TR', 'Türkiye', 'تركيا']
] as const;
const exportDropdowns = new Map<string, { handle: ThemedDropdownHandle; config: ThemedDropdownConfig }>();

function mountExportDropdown(root: HTMLElement, name: string, label: string, placeholder: string, options: ThemedDropdownOption[]) {
  const host = root.querySelector<HTMLElement>(`[data-account-dropdown="${name}"]`);
  if (!host) return;
  const config: ThemedDropdownConfig = {
    name,
    value: pendingValues[name] || '',
    options,
    ariaLabel: label,
    placeholder,
    className: 'filter-dropdown',
    onChange: (value) => {
      const entry = exportDropdowns.get(name);
      if (entry) entry.config = { ...entry.config, value };
    }
  };
  exportDropdowns.set(name, { handle: mountThemedDropdown(host, config), config });
}

function updateExportDropdown(name: string, options: ThemedDropdownOption[]) {
  const entry = exportDropdowns.get(name);
  if (!entry) return;
  const value = options.some(option => option.value === entry.config.value) ? entry.config.value : '';
  entry.config = { ...entry.config, value, options };
  entry.handle.update(entry.config);
}

async function loadOptions(root: HTMLElement) {
  const token = localStorage.getItem('pt-token');
  if (!token) return;
  const status = root.querySelector<HTMLElement>('[data-export-status]');
  if (status) status.textContent = copy.loading;
  try {
    const headers = { Authorization: `Bearer ${token}` };
    const [trackingResponse, storeResponse] = await Promise.all([
      fetch(`${api}/v1/tracking`, { headers }), fetch(`${api}/v1/stores`, { headers })
    ]);
    if (!trackingResponse.ok || !storeResponse.ok) throw new Error('Filter options unavailable');
    const [trackingEnvelope, storeEnvelope] = await Promise.all([
      trackingResponse.json() as Promise<Envelope<Tracking[]>>,
      storeResponse.json() as Promise<Envelope<Store[]>>
    ]);
    const products = [...new Map((trackingEnvelope.data || []).map(item => [item.productId, item.productName])).entries()]
      .sort((a, b) => a[1].localeCompare(b[1]));
    const stores = (storeEnvelope.data || []).filter(item => item.isActive).sort((a, b) => a.name.localeCompare(b.name));
    updateExportDropdown('productId', [{ value: '', label: copy.anyProduct }, ...products.map(([id, name]) => ({ value: id, label: name }))]);
    updateExportDropdown('storeId', [{ value: '', label: copy.anyStore }, ...stores.map(store => ({ value: store.storeId, label: `${store.name} · ${store.country}` }))]);
    if (status) status.textContent = products.length || stores.length ? '' : copy.empty;
  } catch {
    if (status) status.textContent = copy.error;
  }
}

function buildQuery(form: HTMLFormElement, format: string) {
  const values = new FormData(form);
  const query = new URLSearchParams({ format });
  for (const key of ['productId', 'storeId', 'market']) {
    const value = String(values.get(key) || '');
    if (value) query.set(key, value);
  }
  for (const key of ['fromUtc', 'toUtc']) {
    const value = String(values.get(key) || '');
    if (!value) continue;
    const date = new Date(value);
    if (!Number.isNaN(date.getTime())) query.set(key, date.toISOString());
  }
  return query;
}

async function exportSelection(form: HTMLFormElement, format: 'csv' | 'json') {
  const token = localStorage.getItem('pt-token');
  const status = form.querySelector<HTMLElement>('[data-export-status]');
  if (!token) return;
  if (!form.reportValidity()) return;
  const button = form.querySelector<HTMLButtonElement>(`[data-export-format="${format}"]`);
  if (button) button.disabled = true;
  if (status) status.textContent = copy.exporting;
  try {
    const response = await fetch(`${api}/v1/users/me/export?${buildQuery(form, format)}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Export request failed');
    const blob = await response.blob();
    const objectUrl = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = `dealio-account-filtered.${format}`;
    anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    if (status) status.textContent = copy.done;
  } catch {
    if (status) status.textContent = copy.error;
  } finally {
    if (button) button.disabled = false;
  }
}

function mount() {
  const actions = document.querySelector<HTMLElement>('.settings-account-export-actions');
  const anchor = actions?.parentElement;
  if (!anchor || document.querySelector('[data-filtered-account-export]')) return;
  const section = document.createElement('section');
  section.className = 'filtered-export-card';
  section.dataset.filteredAccountExport = 'true';
  section.innerHTML = `<div class="filtered-export-heading"><div><h3>${copy.title}</h3><p>${copy.description}</p></div></div>
    <form class="filtered-export-form">
      <label>${copy.product}<div data-account-dropdown="productId"></div></label>
      <label>${copy.store}<div data-account-dropdown="storeId"></div></label>
      <label>${copy.market}<div data-account-dropdown="market"></div></label>
      <label>${copy.from}<input type="datetime-local" name="fromUtc"></label>
      <label>${copy.to}<input type="datetime-local" name="toUtc"></label>
      <div class="filtered-export-actions"><button class="button button--export button--export-blue" type="button" data-export-format="json">${copy.json}</button><button class="button button--export button--export-yellow" type="button" data-export-format="csv">${copy.csv}</button></div>
      <p class="filtered-export-status" data-export-status role="status" aria-live="polite"></p>
    </form>`;
  anchor.after(section);
  mountExportDropdown(section, 'productId', copy.product, copy.anyProduct, [{ value: '', label: copy.anyProduct }]);
  mountExportDropdown(section, 'storeId', copy.store, copy.anyStore, [{ value: '', label: copy.anyStore }]);
  mountExportDropdown(section, 'market', copy.market, copy.anyMarket, [
    { value: '', label: copy.anyMarket },
    ...markets.map(([code, name, nameAr]) => ({ value: code, label: document.documentElement.lang.toLowerCase().startsWith('ar') ? nameAr : name }))
  ]);
  const form = section.querySelector<HTMLFormElement>('form')!;
  form.querySelectorAll<HTMLInputElement>('input[type="datetime-local"]').forEach((input) => {
    input.value = pendingValues[input.name] || '';
    mountThemedDatePicker({ input, placeholder: input.name === 'fromUtc' ? copy.from : copy.to, locale: document.documentElement.lang || 'en' });
  });
  form.querySelector('[data-export-format="json"]')?.addEventListener('click', () => void exportSelection(form, 'json'));
  form.querySelector('[data-export-format="csv"]')?.addEventListener('click', () => void exportSelection(form, 'csv'));
  void loadOptions(section);
}

window.addEventListener('pt-locale-changed', () => {
  const section = document.querySelector<HTMLElement>('[data-filtered-account-export]');
  const form = section?.querySelector<HTMLFormElement>('form');
  pendingValues = form ? Object.fromEntries([...new FormData(form).entries()].map(([key, value]) => [key, String(value)])) : {};
  exportDropdowns.forEach(entry => entry.handle.destroy());
  exportDropdowns.clear();
  section?.remove();
  copy = getCopy();
  mount();
  pendingValues = {};
});

const observer = new MutationObserver(mount);
observer.observe(document.documentElement, { childList: true, subtree: true });
mount();
