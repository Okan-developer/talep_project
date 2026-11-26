import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { AuthSession, CurrentUser, DemoUser, SystemStatus } from '@/shared/api/client';

export const healthyStatus: SystemStatus = { api: 'up', database: 'available', departmentCount: 6 };

export const statusUrl = '*/api/system/status';
export const loginUrl = '*/api/auth/login';
export const refreshUrl = '*/api/auth/refresh';
export const logoutUrl = '*/api/auth/logout';
export const demoUsersUrl = '*/api/auth/demo-users';

export function statusResponse(status: SystemStatus) {
  return http.get(statusUrl, () => HttpResponse.json(status));
}

/** What the backend's role → permission map grants (spec 0002, R-11 table). */
const rolePermissions: Record<string, string[]> = {
  Requester: [],
  DepartmentManager: ['approvals.view'],
  SoftwareStaff: ['assignments.view_own'],
  SystemStaff: ['assignments.view_own'],
  ItUnitManager: [
    'approvals.view',
    'requests.view_unit',
    'assignments.view_own',
    'requests.view_all',
    'reports.view',
  ],
  Admin: [
    'approvals.view',
    'requests.view_unit',
    'requests.view_all',
    'reports.view',
    'admin.users',
    'admin.categories',
    'admin.workflows',
    'admin.settings',
  ],
};

/** The seven demo users of spec 0002, R-2. */
export const demoUsers: DemoUser[] = [
  {
    fullName: 'Talep Sahibi',
    email: 'talep.sahibi@demo.local',
    role: 'Requester',
    password: 'Demo.2026!',
  },
  {
    fullName: 'Birim Yöneticisi',
    email: 'birim.yoneticisi@demo.local',
    role: 'DepartmentManager',
    password: 'Demo.2026!',
  },
  {
    fullName: 'BT Yazılım Personeli',
    email: 'bt.yazilim@demo.local',
    role: 'SoftwareStaff',
    password: 'Demo.2026!',
  },
  {
    fullName: 'BT Sistem Personeli',
    email: 'bt.sistem@demo.local',
    role: 'SystemStaff',
    password: 'Demo.2026!',
  },
  {
    fullName: 'BT Yazılım Birim Yöneticisi',
    email: 'bt.yazilim.yonetici@demo.local',
    role: 'ItUnitManager',
    password: 'Demo.2026!',
  },
  {
    fullName: 'BT Sistem Birim Yöneticisi',
    email: 'bt.sistem.yonetici@demo.local',
    role: 'ItUnitManager',
    password: 'Demo.2026!',
  },
  {
    fullName: 'Sistem Yöneticisi',
    email: 'admin@demo.local',
    role: 'Admin',
    password: 'Demo.2026!',
  },
];

export function userWith(roles: string[], fullName = 'Sistem Yöneticisi'): CurrentUser {
  const granted = new Set(roles.flatMap((role) => rolePermissions[role] ?? []));
  const order = Object.values(rolePermissions).flat();
  return {
    id: '019ab000-0000-7000-8000-0000000000ff',
    fullName,
    email: 'kullanici@demo.local',
    roles,
    permissions: [...new Set(order)].filter((p) => granted.has(p)),
  };
}

export const adminUser = userWith(['Admin']);

let tokenCounter = 0;

/** A session body as login and refresh return it; the access token is valid for 15 minutes. */
export function sessionFor(user: CurrentUser, now = Date.now()): AuthSession {
  tokenCounter += 1;
  return {
    accessToken: `token-${tokenCounter}`,
    expiresAt: new Date(now + 15 * 60_000).toISOString(),
    user,
  };
}

export const sessionExpiredProblem = {
  status: 401,
  title: 'Oturum sona erdi',
  detail: 'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.',
  code: 'session_expired',
};

export function refreshFails() {
  return http.post(refreshUrl, () =>
    HttpResponse.json(sessionExpiredProblem, {
      status: 401,
      headers: { 'Content-Type': 'application/problem+json' },
    }),
  );
}

export function refreshReturns(user: CurrentUser) {
  return http.post(refreshUrl, () => HttpResponse.json(sessionFor(user)));
}

/** API stub shaped by the generated contract types (docs/testing.md, Frameworks & layout). */
export const server = setupServer(
  statusResponse(healthyStatus),
  refreshFails(),
  http.post(logoutUrl, () => new HttpResponse(null, { status: 204 })),
  http.get(demoUsersUrl, () => new HttpResponse(null, { status: 404 })),
);
