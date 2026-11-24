import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { SystemStatus } from '@/shared/api/client';

export const healthyStatus: SystemStatus = { api: 'up', database: 'available', departmentCount: 6 };

export const statusUrl = '*/api/system/status';

export function statusResponse(status: SystemStatus) {
  return http.get(statusUrl, () => HttpResponse.json(status));
}

/** API stub shaped by the generated contract types (docs/testing.md, Frameworks & layout). */
export const server = setupServer(statusResponse(healthyStatus));
