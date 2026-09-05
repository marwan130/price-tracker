export interface ApiErrorResponse {
  response?: {
    data?: {
      error?: {
        message?: string;
      };
      message?: string;
    };
  };
  message?: string;
}

export function getApiErrorMessage(error: unknown, fallback: string = "Something went wrong"): string {
  if (!error) return fallback;

  const err = error as ApiErrorResponse;
  return (
    err.response?.data?.error?.message ||
    err.response?.data?.message ||
    err.message ||
    fallback
  );
}
