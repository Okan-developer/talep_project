import { zodResolver } from '@hookform/resolvers/zod';
import { CircleAlert, Info, LoaderCircle } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useSearchParams } from 'react-router';
import type { DemoUser } from '@/shared/api/client';
import { safeReturnTo } from '@/shared/auth/returnTo';
import { useAuth, type LoginOutcome } from '@/shared/auth/useAuth';
import { tr } from '@/shared/i18n/tr';
import { Button } from '@/shared/ui/button';
import { DemoUserList } from './DemoUserList';
import { loginSchema, type LoginForm } from './loginSchema';

const inputClass =
  'h-9 w-full rounded-md border bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring aria-invalid:border-danger';

function messageFor(outcome: Exclude<LoginOutcome, { ok: true }>) {
  switch (outcome.kind) {
    case 'invalid':
      return tr.auth.invalidCredentials;
    case 'locked':
      return outcome.message ?? tr.auth.invalidCredentials;
    case 'throttled':
      return tr.auth.throttled;
    default:
      return tr.auth.network;
  }
}

/** The login screen (spec 0002, R-3…R-5, User experience: giriş ekranı). */
export function LoginPage() {
  const { login, sessionExpired } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setValue,
    resetField,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  const submit = handleSubmit(async ({ email, password }) => {
    setServerError(null);
    const outcome = await login(email.trim(), password);
    if (outcome.ok) {
      void navigate(safeReturnTo(params.get('returnTo')), { replace: true });
      return;
    }
    resetField('password');
    setServerError(messageFor(outcome));
  });

  const pickDemoUser = (user: DemoUser) => {
    setValue('email', user.email, { shouldValidate: true });
    setValue('password', user.password, { shouldValidate: true });
    void submit();
  };

  return (
    <main className="flex min-h-svh items-center justify-center bg-background p-4 text-foreground">
      <section
        aria-labelledby="login-title"
        className="flex w-full max-w-sm flex-col gap-5 rounded-xl border bg-card p-6 text-card-foreground shadow-sm"
      >
        <header className="flex flex-col gap-1">
          <p className="text-sm font-semibold text-muted-foreground">{tr.app.name}</p>
          <h1 id="login-title" className="text-2xl font-semibold tracking-tight">
            {tr.auth.title}
          </h1>
          <p className="text-sm text-muted-foreground">{tr.auth.description}</p>
        </header>

        {sessionExpired && !serverError && (
          <p role="status" className="flex items-start gap-2 rounded-md border p-3 text-sm">
            <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
            {tr.auth.sessionExpired}
          </p>
        )}

        <form noValidate onSubmit={submit} className="flex flex-col gap-4" aria-busy={isSubmitting}>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="login-email" className="text-sm font-medium">
              {tr.auth.email}
            </label>
            <input
              id="login-email"
              type="email"
              autoComplete="username"
              className={inputClass}
              aria-invalid={errors.email ? true : undefined}
              aria-describedby={errors.email ? 'login-email-error' : undefined}
              {...register('email')}
            />
            {errors.email && (
              <p id="login-email-error" role="alert" className="text-sm text-danger">
                {errors.email.message}
              </p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="login-password" className="text-sm font-medium">
              {tr.auth.password}
            </label>
            <input
              id="login-password"
              type="password"
              autoComplete="current-password"
              className={inputClass}
              aria-invalid={errors.password ? true : undefined}
              aria-describedby={errors.password ? 'login-password-error' : undefined}
              {...register('password')}
            />
            {errors.password && (
              <p id="login-password-error" role="alert" className="text-sm text-danger">
                {errors.password.message}
              </p>
            )}
          </div>

          {serverError && (
            <p role="alert" className="flex items-start gap-2 text-sm text-danger">
              <CircleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
              {serverError}
            </p>
          )}

          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <LoaderCircle aria-hidden="true" className="animate-spin" />}
            {isSubmitting ? tr.auth.submitting : tr.auth.submit}
          </Button>
        </form>

        <DemoUserList onPick={pickDemoUser} disabled={isSubmitting} />
      </section>
    </main>
  );
}
