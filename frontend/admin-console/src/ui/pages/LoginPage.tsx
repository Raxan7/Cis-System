import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuth } from '../auth';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  email: z.string().trim().email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
  mfaCode: z.string().optional(),
});

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const [error, setError] = useState<unknown>(null);
  const [submitting, setSubmitting] = useState(false);
  const nextPath = (location.state as { from?: string } | null)?.from ?? '/';

  return (
    <div className="login-shell">
      <section className="login-panel">
        <div className="login-panel__intro">
          <span className="eyebrow">Victory Financial Services Ltd</span>
          <h1>CIS Management Admin Console</h1>
          <p>
            Operations, approvals, oversight, and audit visibility for regulated collective investment workflows.
          </p>
        </div>
        <ErrorCallout error={error} />
        <form
          className="login-form"
          onSubmit={async (event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            const payload = schema.safeParse({
              email: formData.get('email'),
              password: formData.get('password'),
              mfaCode: formData.get('mfaCode'),
            });

            if (!payload.success) {
              setError(new Error(payload.error.issues[0]?.message ?? 'Login details are invalid.'));
              return;
            }

            try {
              setSubmitting(true);
              setError(null);
              await login(payload.data.email, payload.data.password, payload.data.mfaCode);
              navigate(nextPath, { replace: true });
            } catch (nextError) {
              setError(nextError);
            } finally {
              setSubmitting(false);
            }
          }}
        >
          <label className="field">
            <span>Email</span>
            <input autoComplete="username" name="email" placeholder="admin@victoryfs.local" type="email" />
          </label>
          <label className="field">
            <span>Password</span>
            <input autoComplete="current-password" name="password" placeholder="Enter your password" type="password" />
          </label>
          <label className="field">
            <span>MFA code</span>
            <input name="mfaCode" placeholder="Optional unless policy requires it" type="text" />
          </label>
          <button className="button button--primary button--full" disabled={submitting} type="submit">
            {submitting ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </section>
    </div>
  );
}
