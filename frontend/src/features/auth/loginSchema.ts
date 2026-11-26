import { z } from 'zod';
import { tr } from '@/shared/i18n/tr';

/** Client-side check for the user's sake only; the server validates again (docs/security.md). */
export const loginSchema = z.object({
  email: z.string().trim().min(1, tr.auth.required),
  password: z.string().min(1, tr.auth.required),
});

export type LoginForm = z.infer<typeof loginSchema>;
