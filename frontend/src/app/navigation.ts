import {
  BarChart3,
  Building2,
  ClipboardCheck,
  FilePlus,
  FileText,
  GitBranch,
  LayoutDashboard,
  List,
  Settings,
  Tags,
  UserCheck,
  Users,
  type LucideIcon,
} from 'lucide-react';
import { tr } from '@/shared/i18n/tr';

export interface NavItem {
  label: string;
  path: string;
  icon: LucideIcon;
  /** The permission that shows the item (spec 0002, R-11); none = every signed-in user. */
  permission?: string;
}

/**
 * The main menu, in the order of spec 0001 R-4, filtered by permission (spec 0002, R-11). Hiding an
 * item is user experience only; the server authorises every request (docs/security.md).
 */
export const navItems: readonly NavItem[] = [
  { label: tr.nav.overview, path: '/', icon: LayoutDashboard },
  { label: tr.nav.myRequests, path: '/taleplerim', icon: FileText },
  { label: tr.nav.newRequest, path: '/talepler/yeni', icon: FilePlus },
  {
    label: tr.nav.pendingApprovals,
    path: '/onay-bekleyenler',
    icon: ClipboardCheck,
    permission: 'approvals.view',
  },
  {
    label: tr.nav.unitRequests,
    path: '/birim-talepleri',
    icon: Building2,
    permission: 'requests.view_unit',
  },
  {
    label: tr.nav.assignedToMe,
    path: '/bana-atananlar',
    icon: UserCheck,
    permission: 'assignments.view_own',
  },
  { label: tr.nav.allRequests, path: '/talepler', icon: List, permission: 'requests.view_all' },
  { label: tr.nav.reports, path: '/raporlar', icon: BarChart3, permission: 'reports.view' },
  {
    label: tr.nav.userManagement,
    path: '/yonetim/kullanicilar',
    icon: Users,
    permission: 'admin.users',
  },
  {
    label: tr.nav.categoryManagement,
    path: '/yonetim/kategoriler',
    icon: Tags,
    permission: 'admin.categories',
  },
  {
    label: tr.nav.workflowManagement,
    path: '/yonetim/onay-akislari',
    icon: GitBranch,
    permission: 'admin.workflows',
  },
  { label: tr.nav.settings, path: '/ayarlar', icon: Settings, permission: 'admin.settings' },
];
