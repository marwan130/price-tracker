import { translations } from './i18n';

const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
const api = import.meta.env.VITE_API_URL || '';
const googleMark = [
  ['#4285F4', 'M23.49 12.27c0-.79-.07-1.55-.2-2.27H12v4.3h6.44a5.5 5.5 0 0 1-2.39 3.61v3h3.87c2.27-2.09 3.57-5.17 3.57-8.64Z'],
  ['#34A853', 'M12 24c3.24 0 5.95-1.08 7.93-2.92l-3.87-3c-1.08.72-2.46 1.15-4.06 1.15-3.12 0-5.77-2.11-6.72-4.96H1.28v3.09A12 12 0 0 0 12 24Z'],
  ['#FBBC05', 'M5.28 14.27A7.2 7.2 0 0 1 4.9 12c0-.79.14-1.55.38-2.27V6.64H1.28A12 12 0 0 0 0 12c0 1.94.46 3.77 1.28 5.36l4-3.09Z'],
  ['#EA4335', 'M12 4.77c1.77 0 3.35.61 4.6 1.82l3.44-3.44C17.95 1.17 15.24 0 12 0A12 12 0 0 0 1.28 6.64l4 3.09c.95-2.85 3.6-4.96 6.72-4.96Z'],
] as const;

function showError(dialog: HTMLElement, message: string) {
  let status = dialog.querySelector<HTMLElement>('[data-dealio-google-error]');
  if (!status) {
    status = document.createElement('p');
    status.dataset.dealioGoogleError = 'true';
    status.className = 'form-error';
    dialog.querySelector('[data-dealio-google]')?.after(status);
  }
  status.setAttribute('role', 'alert');
  status.textContent = message;
}

function mountGoogleButton(dialog: HTMLElement) {
  if (dialog.querySelector('[data-dealio-google]') || !dialog.querySelector('input[type="password"]')) return;

  const copy = document.documentElement.lang.toLowerCase().startsWith('ar') ? translations.ar : translations.en;
  const container = document.createElement('div');
  container.dataset.dealioGoogle = 'true';
  container.className = 'google-signin-button';
  dialog.querySelector('form')?.before(container);

  const separator = document.createElement('div');
  separator.className = 'auth-divider';
  separator.setAttribute('aria-hidden', 'true');
  separator.textContent = copy.authOr;
  container.after(separator);

  const fallback = document.createElement('button');
  fallback.type = 'button';
  fallback.className = 'auth-google-fallback';
  fallback.setAttribute('aria-label', copy.authGoogleButton);

  const mark = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  mark.setAttribute('viewBox', '0 0 24 24');
  mark.setAttribute('aria-hidden', 'true');
  mark.classList.add('auth-google-mark');
  googleMark.forEach(([color, pathData]) => {
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    path.setAttribute('fill', color);
    path.setAttribute('d', pathData);
    mark.append(path);
  });

  const label = document.createElement('span');
  label.textContent = copy.authGoogleButton;
  fallback.append(mark, label);
  fallback.addEventListener('click', () => showError(dialog, copy.authGoogleUnavailable));
  container.append(fallback);

  if (!clientId) {
    return;
  }

  const initialize = () => {
    if (!window.google || !container.isConnected) return;
    container.replaceChildren();
    window.google.accounts.id.initialize({ client_id: clientId, callback: async ({ credential }) => {
      container.setAttribute('aria-busy', 'true');
      try {
        const response = await fetch(`${api}/v1/auth/google`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ credential }) });
        const envelope = await response.json();
        if (!response.ok || !envelope.success || !envelope.data?.accessToken) throw new Error(copy.authGoogleIncomplete);
        localStorage.setItem('pt-token', envelope.data.accessToken);
        localStorage.setItem('pt-name', envelope.data.name || envelope.data.email || 'Account');
        location.reload();
      } catch {
        showError(dialog, copy.authGoogleFailure);
      } finally {
        container.removeAttribute('aria-busy');
      }
    } });
    window.google.accounts.id.renderButton(container, { theme: document.documentElement.dataset.theme === 'dark' ? 'filled_black' : 'outline', size: 'large', shape: 'pill', width: Math.min(360, container.clientWidth || 360), text: 'continue_with' });
  };

  if (window.google) initialize();
  else {
    let script = document.querySelector<HTMLScriptElement>('script[data-google-identity]');
    if (!script) {
      script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.dataset.googleIdentity = 'true';
      document.head.append(script);
    }
    script.addEventListener('load', initialize, { once: true });
    script.addEventListener('error', () => showError(dialog, copy.authGoogleUnavailable), { once: true });
  }
}

const observer = new MutationObserver(() => document.querySelectorAll<HTMLElement>('.auth-dialog').forEach(mountGoogleButton));
observer.observe(document.documentElement, { childList: true, subtree: true });
document.querySelectorAll<HTMLElement>('.auth-dialog').forEach(mountGoogleButton);

window.addEventListener('pt-locale-changed', () => {
  document.querySelectorAll<HTMLElement>('.auth-dialog').forEach(dialog => {
    dialog.querySelector('[data-dealio-google]')?.remove();
    dialog.querySelector('.auth-divider')?.remove();
    dialog.querySelector('[data-dealio-google-error]')?.remove();
    mountGoogleButton(dialog);
  });
});
