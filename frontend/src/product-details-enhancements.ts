import { translations } from './i18n';

type Point = HTMLButtonElement & { x?: number; y?: number; day?: string };
const localizedText = () => {
  const strings = document.documentElement.lang.toLowerCase().startsWith('ar') ? translations.ar : translations.en;
  return { activity: strings.productNavActivity, offers: strings.productNavOffers, history: strings.productNavHistory, lowest: strings.productLowestVerified, checks: strings.productLastChecked, compare: strings.discoverCompare, empty: strings.productActivityEmpty };
};

function enhanceChart(section: HTMLElement) {
  const text = localizedText();
  const plot = section.querySelector<HTMLElement>('.chart-plot');
  if (!plot) return;
  const points = [...plot.querySelectorAll<Point>('button.chart-point')].map(point => {
    point.x = Number.parseFloat(point.style.left); point.y = Number.parseFloat(point.style.top);
    point.day = point.getAttribute('aria-label')?.split(' · ')[0] || '';
    return point;
  }).filter(point => Number.isFinite(point.x) && Number.isFinite(point.y) && point.day);
  const lowestByDay = new Map<string, Point>();
  for (const point of points) {
    const current = lowestByDay.get(point.day!);
    if (!current || point.y! > current.y!) lowestByDay.set(point.day!, point);
  }
  const lowest = [...lowestByDay.values()].sort((a,b) => a.x! - b.x!);
  if (!lowest.length) return;
  const xGaps = lowest.slice(1).map((point,index) => point.x! - lowest[index].x!).filter(gap => gap > 0).sort((a,b) => a-b);
  const typicalGap = xGaps.length ? xGaps[Math.floor((xGaps.length - 1) / 2)] : 100;
  const path = lowest.map((point,index) => `${index === 0 || point.x! - lowest[index-1].x! > typicalGap * 2.5 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' ');
  plot.querySelectorAll('.chart-series').forEach(series => (series as SVGElement).style.display = 'none');
  let svg = plot.querySelector<SVGSVGElement>('.chart-series--lowest');
  if (!svg) { svg = document.createElementNS('http://www.w3.org/2000/svg','svg'); svg.setAttribute('viewBox','0 0 100 100'); svg.setAttribute('preserveAspectRatio','none'); svg.setAttribute('aria-hidden','true'); svg.classList.add('chart-series','chart-series--lowest'); plot.prepend(svg); }  let line = svg.querySelector('path');
  if (!line) { line = document.createElementNS('http://www.w3.org/2000/svg','path'); line.setAttribute('fill','none'); line.setAttribute('stroke','var(--brand)'); line.setAttribute('stroke-width','2.6'); line.setAttribute('stroke-linecap','round'); line.setAttribute('stroke-linejoin','round'); svg.append(line); }
  line.setAttribute('d',path);
  // Re-runs hide every series above, which also hides the lowest line the
  // first pass drew. Restore it, but only when actually hidden so repeated
  // observer passes do not mutate style attributes forever.
  if (svg.style.display) svg.style.display = '';
  points.forEach(point => point.classList.toggle('chart-point--other',!lowest.includes(point)));
  const legend = section.querySelector<HTMLElement>('.history-legend');
  if (legend && legend.dataset.dealioLowest !== 'true') { legend.replaceChildren(); const label = document.createElement('span'); const swatch = document.createElement('i'); swatch.className = 'legend-dot legend-dot--0'; label.append(swatch,document.createTextNode(text.lowest)); legend.append(label); legend.dataset.dealioLowest = 'true'; }
}

function catalogSignature(items: Element[]) {
  return items.map(el => (el.textContent || '').trim()).join('¦');
}

function addActivity(offers: HTMLElement) {
  const text = localizedText();
  const rows = [...offers.querySelectorAll('tbody tr')];
  const trackingCards = [...document.querySelectorAll<HTMLElement>('.tracking-preference-card')];
  // The PDP hydrates progressively: #offers can mount before its rows and
  // tracking cards exist, and the first pass used to lock in an empty
  // "no activity" list that later fills never updated. Only render/rebuild
  // once content is actually available, so the observer can fill it in a
  // later pass instead of flashing an empty section.
  if (!rows.length && !trackingCards.length) {
    document.getElementById('activity')?.remove();
    return;
  }
  let section = document.getElementById('activity') || null;
  // Signature of what currently matters; rebuilding the same content again
  // would itself fire the MutationObserver and loop forever.
  const signature = `${catalogSignature(trackingCards)}|${catalogSignature(rows)}`;
  if (!section) {
    section = document.createElement('section'); section.className = 'content-section product-activity'; section.id = 'activity';
    section.dataset.dealioActivity = signature;
    const heading = document.createElement('h2'); heading.textContent = text.activity; section.append(heading);
    offers.before(section);
  } else if (section.dataset.dealioActivity === signature) {
    return;
  }
  section.dataset.dealioActivity = signature;
  section.querySelector('ol.product-activity-list')?.remove();
  section.querySelectorAll(':scope > p').forEach(p => p.remove());
  const list = document.createElement('ol'); list.className = 'product-activity-list';
  trackingCards.forEach(card => { const item = document.createElement('li'); item.textContent = card.querySelector('.tracking-preference-title')?.textContent?.trim() || ''; if (item.textContent) list.append(item); });
  rows.forEach(row => { const cells = [...row.querySelectorAll('td')]; if (cells.length < 4) return; const item = document.createElement('li'); const label = document.createElement('strong'); label.textContent = cells[0].textContent?.trim() || ''; const detail = document.createElement('span'); detail.textContent = `${cells[1].textContent?.trim() || ''} · ${text.checks}: ${cells[3].textContent?.trim() || '—'}`; item.append(label,detail); list.append(item); });
  if (!list.children.length) { const empty = document.createElement('p'); empty.textContent = text.empty; section.append(empty); } else section.append(list);
}

function addStickyNavigation(hero: HTMLElement, offers: HTMLElement, history: HTMLElement | null) {
  const text = localizedText();
  if (document.querySelector('.sticky-product-bar')) return;
  const name = hero.querySelector('h1')?.textContent?.trim();
  if (!name) return;
  const bar = document.createElement('div'); bar.className = 'sticky-product-bar';
  const identity = document.createElement('div'); identity.className = 'sticky-product-identity';
  const image = hero.querySelector<HTMLImageElement>('.product-hero-image img');
  if (image) { const thumb = image.cloneNode(true) as HTMLImageElement; thumb.alt = ''; thumb.setAttribute('aria-hidden','true'); identity.append(thumb); }
  const title = document.createElement('strong'); title.textContent = name; identity.append(title);
  const price = hero.querySelector('.product-hero-price strong'); if (price) { const value = document.createElement('span'); value.textContent = price.textContent; identity.append(value); }
  const actions = document.createElement('div'); actions.className = 'sticky-product-actions';
  const compare = document.createElement('button'); compare.type = 'button'; compare.className = 'button button--tonal'; compare.textContent = text.compare;
  compare.addEventListener('click',() => hero.querySelector<HTMLButtonElement>('.product-hero-actions button')?.click()); actions.append(compare);
  const nav = document.createElement('nav'); nav.setAttribute('aria-label',text.activity);
  const links: [string,HTMLElement|null][] = [[text.activity,document.getElementById('activity')],[text.offers,offers],[text.history,history]];
  links.forEach(([label,target]) => { if (!target) return; const link = document.createElement('a'); link.href = `#${target.id}`; link.textContent = label; nav.append(link); });
  bar.append(identity,nav,actions); hero.after(bar);
}

function enhanceProductDetails() {
  const hero = document.querySelector<HTMLElement>('.product-hero'); const offers = document.querySelector<HTMLElement>('#offers');
  if (!hero || !offers) {
    document.getElementById('activity')?.remove();
    document.querySelector('.sticky-product-bar')?.remove();
    return;
  }
  const heroName = hero.querySelector('h1')?.textContent?.trim();
  const existingBar = document.querySelector<HTMLElement>('.sticky-product-bar');
  if (existingBar && existingBar.querySelector('strong')?.textContent?.trim() !== heroName) {
    existingBar.remove();
    document.getElementById('activity')?.remove();
  }
  const history = [...document.querySelectorAll<HTMLElement>('.content-section')].find(section => section.querySelector('.range-select, .history-chart, .history-observation-list')) || null;
  if (history) history.id = 'history';
  enhanceChart(history || offers);
  addActivity(offers);
  addStickyNavigation(hero,offers,history);
}

const observer = new MutationObserver(() => enhanceProductDetails());
observer.observe(document.documentElement,{childList:true,subtree:true});
enhanceProductDetails();
document.addEventListener('change',event => { if ((event.target as HTMLElement).closest('.history-store-controls')) requestAnimationFrame(enhanceProductDetails); });
window.addEventListener('pt-locale-changed', () => {
  document.getElementById('activity')?.remove();
  document.querySelector('.sticky-product-bar')?.remove();
  const legend = document.querySelector<HTMLElement>('.history-legend');
  if (legend) delete legend.dataset.dealioLowest;
  requestAnimationFrame(enhanceProductDetails);
});
