import { useQuery } from '@tanstack/react-query';
import { UserRound } from 'lucide-react';
import { authClient, type DemoUser } from '@/shared/api/client';
import { tr } from '@/shared/i18n/tr';

/**
 * "Demo kullanıcı seç" (spec 0002, R-5): the backend answers only in Development; anywhere else it
 * answers "not found" and the list does not render.
 */
export function DemoUserList({
  onPick,
  disabled,
}: {
  onPick: (user: DemoUser) => void;
  disabled: boolean;
}) {
  const { data } = useQuery({
    queryKey: ['demo-users'],
    queryFn: async () => {
      const { data, response } = await authClient.GET('/api/auth/demo-users');
      return response.ok && data ? data : [];
    },
    retry: false,
    staleTime: Infinity,
  });

  if (!data?.length) return null;

  return (
    <section aria-labelledby="demo-users-title" className="flex flex-col gap-2 border-t pt-4">
      <div>
        <h2 id="demo-users-title" className="text-sm font-semibold">
          {tr.auth.demoTitle}
        </h2>
        <p className="text-xs text-muted-foreground">{tr.auth.demoDescription}</p>
      </div>
      <ul className="flex flex-col gap-1">
        {data.map((user) => (
          <li key={user.email}>
            <button
              type="button"
              disabled={disabled}
              onClick={() => onPick(user)}
              className="flex w-full items-center gap-3 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring disabled:opacity-50"
            >
              <UserRound aria-hidden="true" className="size-4 shrink-0 text-muted-foreground" />
              <span className="flex min-w-0 flex-col">
                <span className="truncate font-medium">{user.fullName}</span>
                <span className="truncate text-xs text-muted-foreground">
                  {tr.roles[user.role] ?? user.role} · {user.email}
                </span>
              </span>
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
