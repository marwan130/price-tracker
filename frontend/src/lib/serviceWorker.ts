// Service Worker Registration for Web Push Notifications
export function registerServiceWorker() {
  if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
      navigator.serviceWorker.register('/sw.js')
        .then(() => undefined)
        .catch((error) => {
          if (import.meta.env.DEV) {
            console.error('Service Worker registration failed:', error);
          }
        });
    });
  }
}

export function unregisterServiceWorker() {
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.ready
      .then((registration) => {
        registration.unregister();
      })
      .catch((error) => {
        if (import.meta.env.DEV) {
          console.error(error.message);
        }
      });
  }
}

// Request notification permission
export async function requestNotificationPermission() {
  if ('Notification' in window) {
    const permission = await Notification.requestPermission();
    if (permission === 'granted') {
      return true;
    }
  }
  return false;
}

// Check if notifications are supported
export function areNotificationsSupported() {
  return 'Notification' in window && 'serviceWorker' in navigator;
}

// Subscribe to push notifications
export async function subscribeToPushNotifications() {
  if (!('serviceWorker' in navigator)) {
    throw new Error('Service workers are not supported');
  }

  const registration = await navigator.serviceWorker.ready;
  
  try {
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: import.meta.env.VITE_VAPID_PUBLIC_KEY,
    });
    
    // Send subscription to backend
    await fetch('/v1/notifications/subscribe', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(subscription),
    });
    
    return subscription;
  } catch (error) {
    if (import.meta.env.DEV) {
      console.error('Failed to subscribe to push notifications:', error);
    }
    throw error;
  }
}
