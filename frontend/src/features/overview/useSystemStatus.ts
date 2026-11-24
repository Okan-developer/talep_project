import { useQuery } from '@tanstack/react-query';
import { apiClient, withTimeout, type SystemStatus } from '@/shared/api/client';

export const systemStatusQueryKey = ['system-status'] as const;

/** Loads once on page open; refreshed only by "Tekrar dene" (spec 0001, Constraints). */
export function useSystemStatus() {
  return useQuery({
    queryKey: systemStatusQueryKey,
    queryFn: async ({ signal }): Promise<SystemStatus> => {
      const timeout = withTimeout(signal);
      try {
        const { data, response } = await timeout.race(
          apiClient.GET('/api/system/status', { signal: timeout.signal }),
        );
        if (!response.ok || !data)
          throw new Error(`System status request failed (${response.status})`);
        return data;
      } finally {
        timeout.dispose();
      }
    },
    retry: false,
    refetchOnWindowFocus: false,
  });
}
