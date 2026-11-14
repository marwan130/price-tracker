try {
  const locale = localStorage.getItem('pt-locale') || 'en';
  document.documentElement.lang = locale;
  document.documentElement.dir = locale === 'ar' ? 'rtl' : 'ltr';
} catch {
  document.documentElement.lang = 'en';
  document.documentElement.dir = 'ltr';
}

try {
  const theme = localStorage.getItem('pt-theme') || 'system';
  document.documentElement.dataset.theme = theme === 'system'
    ? (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
    : theme;
} catch {
  document.documentElement.dataset.theme = 'light';
}
