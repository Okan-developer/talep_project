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
}

/** The main menu, in the order of spec 0001 R-4. Role filtering arrives with spec 0002. */
export const navItems: readonly NavItem[] = [
  { label: tr.nav.overview, path: '/', icon: LayoutDashboard },
  { label: tr.nav.myRequests, path: '/taleplerim', icon: FileText },
  { label: tr.nav.newRequest, path: '/talepler/yeni', icon: FilePlus },
  { label: tr.nav.pendingApprovals, path: '/onay-bekleyenler', icon: ClipboardCheck },
  { label: tr.nav.unitRequests, path: '/birim-talepleri', icon: Building2 },
  { label: tr.nav.assignedToMe, path: '/bana-atananlar', icon: UserCheck },
  { label: tr.nav.allRequests, path: '/talepler', icon: List },
  { label: tr.nav.reports, path: '/raporlar', icon: BarChart3 },
  { label: tr.nav.userManagement, path: '/yonetim/kullanicilar', icon: Users },
  { label: tr.nav.categoryManagement, path: '/yonetim/kategoriler', icon: Tags },
  { label: tr.nav.workflowManagement, path: '/yonetim/onay-akislari', icon: GitBranch },
  { label: tr.nav.settings, path: '/ayarlar', icon: Settings },
];
