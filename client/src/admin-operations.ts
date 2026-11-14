import { translations } from './i18n';

type Summary = {
  activeUsers: number; activeTrackings: number; verifiedOffers: number; activeStores: number;
  unpublishedOutboxMessages: number; outboxRetries: number; oldestUnpublishedOutboxAgeSeconds: number;
  scrapeJobsLast24Hours: number; averageScrapeDurationMs?: number | null;
  apiRequests: { windowMinutes: number; requestCount: number; averageLatencyMs: number; p95LatencyMs: number };
  providerQuota: { provider: string; used: number | null; limit: number; month: string };
  rabbitMq?: { available: boolean; message?: string; queues?: { name: string; messages: number; consumers: number }[] } | null;
};
type Store = { storeId: number; name: string; state: string; checks: number; failed: number; partial: number; consecutiveScrapeFailures: number; autoSuspendedUntilUtc?: string | null };
const api = import.meta.env.VITE_API_URL || '';
const getLabels = () => document.documentElement.lang.toLowerCase().startsWith('ar') ? {
  heading: 'نظرة عامة على العمليات', description: 'نشاط الحسابات والبحث والطوابير وجمع الأسعار.', refresh: 'تحديث', retry: 'إعادة المحاولة',
  users: 'الحسابات النشطة', trackings: 'قواعد المتابعة النشطة', offers: 'العروض النشطة', stores: 'المتاجر النشطة', outbox: 'رسائل بانتظار النشر',
  scrape: 'عمليات الجمع · آخر 24 ساعة', avgScrape: 'متوسط مدة الجمع', api: 'طلبات API · آخر 15 دقيقة', apiAvg: 'متوسط زمن الاستجابة', apiP95: 'المئين 95 لزمن الاستجابة',
  retries: 'إعادات نشر الطابور', oldest: 'أقدم رسالة منتظرة · ثوانٍ', quota: 'استخدام حصة البحث', health: 'صحة جمع الأسعار حسب المتجر', checks: 'فحوص', failed: 'فشل', partial: 'جزئي',
  consecutive: 'إخفاقات متتالية', suspended: 'موقوف حتى', noStores: 'لا توجد متاجر مهيأة.', noAdmin: 'سجّل الدخول بحساب مسؤول لعرض العمليات.', queue: 'طوابير RabbitMQ', unavailable: 'بيانات الطوابير غير متاحة حالياً.', loadError: 'تعذّر تحميل بيانات العمليات.', state: { healthy: 'سليم', degraded: 'متراجع', unhealthy: 'غير سليم', stale: 'قديم', suspended: 'موقوف', unknown: 'غير معروف' }
} : {
  heading: 'Operations overview', description: 'Account, search, queue, and scrape activity at a glance.', refresh: 'Refresh', retry: 'Try again',
  users: 'Active accounts', trackings: 'Active tracking rules', offers: 'Active offers', stores: 'Active stores', outbox: 'Unpublished messages',
  scrape: 'Scrapes · last 24 hours', avgScrape: 'Average scrape duration', api: 'API requests · last 15 minutes', apiAvg: 'Average API latency', apiP95: 'API latency · p95',
  retries: 'Outbox retries', oldest: 'Oldest pending message · seconds', quota: 'Search quota usage', health: 'Store scrape health', checks: 'checks', failed: 'failed', partial: 'partial',
  consecutive: 'consecutive failures', suspended: 'Suspended until', noStores: 'No stores are configured.', noAdmin: 'Sign in with an administrator account to view operations.', queue: 'RabbitMQ queues', unavailable: 'Queue metrics are temporarily unavailable.', loadError: 'Could not load operations data.', state: { healthy: 'Healthy', degraded: 'Degraded', unhealthy: 'Unhealthy', stale: 'Stale', suspended: 'Suspended', unknown: 'Unknown' }
};
const escapeHtml = (value: unknown) => String(value ?? '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]!));
const metric = (label: string, value: string | number) => `<article class="metric"><div class="metric-label">${label}</div><div class="metric-value">${Number.isFinite(Number(value)) ? Number(value).toLocaleString(document.documentElement.lang || 'en') : escapeHtml(value)}</div></article>`;

async function load(section: HTMLElement) {
  const labels = getLabels();
  const strings = document.documentElement.lang.toLowerCase().startsWith('ar') ? translations.ar : translations.en;
  const token = localStorage.getItem('pt-token');
  if (!token) { section.innerHTML = `<h2>${labels.heading}</h2><p>${labels.noAdmin}</p>`; return; }
  try {
    const headers = { Authorization: `Bearer ${token}` };
    const [summaryResponse, healthResponse] = await Promise.all([
      fetch(`${api}/v1/operations/summary`, { headers }), fetch(`${api}/v1/operations/store-health?windowDays=7`, { headers })
    ]);
    if (!summaryResponse.ok || !healthResponse.ok) throw new Error(`Operations data request failed (${summaryResponse.status || healthResponse.status}).`);
    const [summaryEnvelope, healthEnvelope] = await Promise.all([summaryResponse.json(), healthResponse.json()]);
    const summary = summaryEnvelope.data as Summary; const stores = healthEnvelope.data.stores as Store[];
    const stats = [
      metric(labels.users,summary.activeUsers), metric(labels.trackings,summary.activeTrackings), metric(labels.offers,summary.verifiedOffers),
      metric(labels.stores,summary.activeStores), metric(labels.outbox,summary.unpublishedOutboxMessages), metric(labels.retries,summary.outboxRetries),
      metric(labels.scrape,summary.scrapeJobsLast24Hours), metric(labels.avgScrape,Math.round(summary.averageScrapeDurationMs || 0)),
      metric(labels.api,summary.apiRequests.requestCount), metric(labels.apiAvg,summary.apiRequests.averageLatencyMs), metric(labels.apiP95,summary.apiRequests.p95LatencyMs),
      metric(labels.oldest,summary.oldestUnpublishedOutboxAgeSeconds), metric(labels.quota,`${summary.providerQuota.used ?? '—'} / ${summary.providerQuota.limit}`)
    ];
    const queues = summary.rabbitMq?.available ? (summary.rabbitMq.queues || []).map(queue => `<article class="ops-store-row"><strong>${escapeHtml(queue.name)}</strong><span>${strings.operationsQueueMessages(queue.messages)}</span><span>${strings.operationsQueueConsumers(queue.consumers)}</span></article>`).join('') : `<p>${labels.unavailable}</p>`;
    section.innerHTML = `<div class="section-heading"><div><h2>${labels.heading}</h2><p>${labels.description}</p></div><button type="button" class="button button--quiet" data-refresh>${labels.refresh}</button></div><div class="stat-grid">${stats.join('')}</div><h3>${labels.queue}</h3><div class="ops-store-list">${queues}</div><h3>${labels.health}</h3><div class="ops-store-list">${stores.length ? stores.map(store => { const state = store.state.toLowerCase(); return `<article class="ops-store-row"><strong>${escapeHtml(store.name)}</strong><span class="ops-store-state ops-store-state--${escapeHtml(store.state)}">${labels.state[state as keyof typeof labels.state] || labels.state.unknown}</span><span>${store.checks} ${labels.checks} · ${store.failed} ${labels.failed} · ${store.partial} ${labels.partial}</span>${store.consecutiveScrapeFailures ? `<span>${store.consecutiveScrapeFailures} ${labels.consecutive}</span>` : ''}${store.autoSuspendedUntilUtc ? `<span>${labels.suspended} ${escapeHtml(new Date(store.autoSuspendedUntilUtc).toLocaleString(document.documentElement.lang || 'en'))}</span>` : ''}</article>`; }).join('') : `<p>${labels.noStores}</p>`}</div>`;
    section.querySelector('[data-refresh]')?.addEventListener('click', () => void load(section));
  } catch { section.innerHTML = `<h2>${labels.heading}</h2><p class="form-error" role="alert">${labels.loadError}</p><button type="button" class="button button--quiet" data-refresh>${labels.retry}</button>`; section.querySelector('[data-refresh]')?.addEventListener('click', () => void load(section)); }
}

function mount() {
  const reviews = document.querySelector<HTMLElement>('#match-reviews');
  if (!reviews || document.querySelector('[data-dealio-operations]')) return;
  const section = document.createElement('section'); section.className = 'settings-panel settings-panel--wide ops-overview'; section.dataset.dealioOperations = 'true';
  reviews.before(section); void load(section);
}
const observer = new MutationObserver(mount);
observer.observe(document.documentElement, { childList: true, subtree: true });
window.addEventListener('pt-locale-changed', () => {
  document.querySelector('[data-dealio-operations]')?.remove();
  mount();
});
mount();
