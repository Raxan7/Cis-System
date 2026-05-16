import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuth } from '../auth';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  email: z.string().trim().email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
  mfaCode: z.string().trim().max(20, 'MFA codes are short.').optional(),
});

type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const nextPath = (location.state as { from?: string } | null)?.from ?? '/';

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: '',
      password: '',
      mfaCode: '',
    },
  });

  return (
    <div className="login-shell">
      <section className="login-panel">
        <div className="login-panel__intro">
          <span className="eyebrow">Victory Financial Services Ltd</span>
          <h1>Victory CIS Investor Portal</h1>
          <p>Track your holdings, submit digital service requests, and download investor documents through a secured self-service workspace.</p>
        </div>
        <ErrorCallout error={form.formState.errors.root?.message} />
        <form
          className="login-form"
          onSubmit={form.handleSubmit(async (values) => {
            try {
              await login(values.email, values.password, values.mfaCode || null);
              navigate(nextPath, { replace: true });
            } catch (error) {
              form.setError('root', {
                message: error instanceof Error ? error.message : 'Login failed.',
              });
            }
          })}
        >
          <label className="field">
            <span>Email</span>
            <input autoComplete="username" placeholder="investor@example.com" type="email" {...form.register('email')} />
            <small className="field__error">{form.formState.errors.email?.message}</small>
          </label>
          <label className="field">
            <span>Password</span>
            <input autoComplete="current-password" placeholder="Enter your password" type="password" {...form.register('password')} />
            <small className="field__error">{form.formState.errors.password?.message}</small>
          </label>
          <label className="field">
            <span>MFA code</span>
            <input placeholder="Enter your code if prompted" type="text" {...form.register('mfaCode')} />
            <small>MFA-ready flow: provide a code whenever your portal policy requires one.</small>
          </label>
          <button className="button button--primary button--full" disabled={form.formState.isSubmitting} type="submit">
            {form.formState.isSubmitting ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </section>
    </div>
  );
}
