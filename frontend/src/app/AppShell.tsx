import { Menu, PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import { useState } from 'react';
import { Outlet } from 'react-router';
import { tr } from '@/shared/i18n/tr';
import { cn, safeStorage } from '@/shared/lib/utils';
import { Button } from '@/shared/ui/button';
import { Sheet, SheetContent, SheetTrigger } from '@/shared/ui/sheet';
import { AppSidebar } from './AppSidebar';
import { TopBar } from './TopBar';
import { MOBILE_QUERY, useMediaQuery } from './useMediaQuery';

export const SIDEBAR_STORAGE_KEY = 'talep.sidebar.collapsed';

/** The application frame: sidebar → top bar → content, in that tab order (spec 0001, R-4, R-7). */
export function AppShell() {
  const isMobile = useMediaQuery(MOBILE_QUERY);
  const [collapsed, setCollapsed] = useState(() => safeStorage.get(SIDEBAR_STORAGE_KEY) === 'true');
  const [drawerOpen, setDrawerOpen] = useState(false);

  const toggleCollapsed = () => {
    const next = !collapsed;
    safeStorage.set(SIDEBAR_STORAGE_KEY, String(next));
    setCollapsed(next);
  };

  return (
    <div className="flex min-h-svh bg-background text-foreground">
      {!isMobile && (
        <aside
          className={cn(
            'sticky top-0 flex h-svh shrink-0 flex-col border-r bg-sidebar transition-[width]',
            collapsed ? 'w-16' : 'w-64',
          )}
          data-collapsed={collapsed}
        >
          <div
            className={cn(
              'flex h-14 items-center border-b px-4 font-semibold',
              collapsed && 'justify-center px-0',
            )}
          >
            <span aria-hidden={collapsed} className={cn(collapsed && 'sr-only')}>
              {tr.app.name}
            </span>
            {collapsed && <span aria-hidden="true">BT</span>}
          </div>
          <AppSidebar collapsed={collapsed} />
        </aside>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-14 items-center gap-2 border-b bg-background/95 px-4 backdrop-blur">
          {isMobile ? (
            <Sheet open={drawerOpen} onOpenChange={setDrawerOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="icon" aria-label={tr.shell.openMenu}>
                  <Menu aria-hidden="true" />
                </Button>
              </SheetTrigger>
              <SheetContent title={tr.nav.label}>
                <div className="flex h-14 items-center border-b px-4 font-semibold">
                  {tr.app.name}
                </div>
                <AppSidebar onNavigate={() => setDrawerOpen(false)} />
              </SheetContent>
            </Sheet>
          ) : (
            <Button
              variant="ghost"
              size="icon"
              aria-label={collapsed ? tr.shell.expandSidebar : tr.shell.collapseSidebar}
              aria-expanded={!collapsed}
              onClick={toggleCollapsed}
            >
              {collapsed ? (
                <PanelLeftOpen aria-hidden="true" />
              ) : (
                <PanelLeftClose aria-hidden="true" />
              )}
            </Button>
          )}
          {isMobile && <span className="font-semibold">{tr.app.name}</span>}
          <TopBar />
        </header>

        <main id="main" className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
