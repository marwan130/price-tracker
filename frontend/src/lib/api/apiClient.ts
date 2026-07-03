import axios from "axios";
import { useAuthStore } from "@/lib/store/useAuthStore";
import toast from "react-hot-toast";

const API_URL =
  import.meta.env.VITE_API_URL ??
  (import.meta.env.DEV ? "https://localhost:5001" : "");

export const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// request interceptor injects jwt access token if available
apiClient.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().token;
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (token) {
      prom.resolve(token);
    } else {
      prom.reject(error);
    }
  });
  failedQueue = [];
};

// response interceptor handles api failures and automatic token refreshes
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Skip redirect and toast for auth page requests to allow local handlers to show precise messages.
    if (
      originalRequest.url?.includes("/auth/login") ||
      originalRequest.url?.includes("/auth/register") ||
      originalRequest.url?.includes("/auth/resend-verification") ||
      originalRequest.url?.includes("/auth/verify-email")
    ) {
      return Promise.reject(error);
    }

    // checks for unauthorized status to attempt session recovery
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return apiClient(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = useAuthStore.getState().refreshToken;
      if (refreshToken) {
        try {
          // calls refresh endpoint with current token
          const res = await axios.post(`${API_URL}/v1/auth/refresh`, {
            refreshToken,
          });

          if (res.data?.success && res.data?.data) {
            const { accessToken, refreshToken: newRefreshToken } = res.data.data;
            const user = useAuthStore.getState().user;
            if (user) {
              useAuthStore.getState().setSession(accessToken, newRefreshToken, user);
            }
            originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            processQueue(null, accessToken);
            isRefreshing = false;
            return apiClient(originalRequest);
          }
        } catch (refreshError) {
          processQueue(refreshError, null);
          isRefreshing = false;
          useAuthStore.getState().logout();
          window.location.href = "/login";
          return Promise.reject(refreshError);
        }
      } else {
        useAuthStore.getState().logout();
        window.location.href = "/login";
      }
    }

    // fallback formatting of error messages to alert user
    const apiError = error.response?.data?.error;
    const msg = apiError?.message || error.message || "something went wrong";

    // prevents toast flooding for auth status checks
    if (!originalRequest.url?.includes("/auth/refresh")) {
      toast.error(msg);
    }

    return Promise.reject(error);
  }
);
