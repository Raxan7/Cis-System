import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { useEffect } from 'react';
import { z } from 'zod';
import { useAuth } from '../auth';
import { ErrorCallout } from '../components/ErrorCallout';
import { createPortalSelfRegistration, verifyPortalSelfRegistrationOtp, type PortalSelfRegistrationInitiatedDto } from '../../lib/portal-api';

const loginSchema = z.object({
  email: z.string().trim().email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
  mfaCode: z.string().trim().max(20, 'MFA codes are short.').optional(),
});

const registrationSchema = z.object({
  displayName: z.string().trim().min(3, 'Enter your full name.').max(200),
  email: z.string().trim().email('Enter a valid email address.'),
  phoneNumber: z.string().trim().min(5, 'Enter a valid phone number.').max(50),
  password: z.string().min(12, 'Use at least 12 characters.').max(200),
  confirmPassword: z.string().min(12, 'Confirm your password.'),
}).refine((values) => values.password === values.confirmPassword, {
  message: 'Passwords must match.',
  path: ['confirmPassword'],
});

const otpSchema = z.object({
  otpCode: z.string().trim().min(4, 'Enter the OTP we sent you.').max(10),
});

type LoginFormValues = z.infer<typeof loginSchema>;
type RegistrationFormValues = z.infer<typeof registrationSchema>;
type OtpFormValues = z.infer<typeof otpSchema>;

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const nextPath = (location.state as { from?: string } | null)?.from ?? '/';
  const initialMode = (location.pathname === '/register' || new URLSearchParams(location.search).get('mode') === 'register')
    ? 'register'
    : 'login';
  const [mode, setMode] = useState<'login' | 'register' | 'verify'>(initialMode);

  useEffect(() => {
    const shouldBeRegister = (location.pathname === '/register' || new URLSearchParams(location.search).get('mode') === 'register');
    if (shouldBeRegister && mode !== 'register') setMode('register');
    if (!shouldBeRegister && mode === 'register' && location.pathname === '/login') setMode('login');
  }, [location.pathname, location.search]);
  const [pendingRegistration, setPendingRegistration] = useState<{
    registration: PortalSelfRegistrationInitiatedDto;
    email: string;
    password: string;
  } | null>(null);

  const loginForm = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      mfaCode: '',
    },
  });
  const registrationForm = useForm<RegistrationFormValues>({
    resolver: zodResolver(registrationSchema),
    defaultValues: {
      displayName: '',
      email: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
    },
  });
  const otpForm = useForm<OtpFormValues>({
    resolver: zodResolver(otpSchema),
    defaultValues: {
      otpCode: '',
    },
  });

  return (
    <div className="login-shell">
      <section className="login-panel">
        <div className="login-panel__intro">
          <span className="eyebrow">Victory Financial Services Ltd</span>
          <h1>Victory CIS Investor Portal</h1>
          <p>Track your holdings, submit digital service requests, and complete onboarding through a secured self-service workspace.</p>
        </div>
        <div className="inline-actions">
          <button className={`button ${mode === 'login' ? 'button--primary' : ''}`} type="button" onClick={() => setMode('login')}>
            Sign in
          </button>
          <button className={`button ${mode !== 'login' ? 'button--primary' : ''}`} type="button" onClick={() => setMode(pendingRegistration ? 'verify' : 'register')}>
            Create account
          </button>
        </div>

        {mode === 'login' ? (
          <>
            <ErrorCallout error={loginForm.formState.errors.root?.message} />
            <form
              className="login-form"
              onSubmit={loginForm.handleSubmit(async (values) => {
                try {
                  await login(values.email, values.password, values.mfaCode || null);
                  navigate(nextPath, { replace: true });
                } catch (error) {
                  loginForm.setError('root', {
                    message: error instanceof Error ? error.message : 'Login failed.',
                  });
                }
              })}
            >
              <label className="field">
                <span>Email</span>
                <input autoComplete="username" placeholder="investor@example.com" type="email" {...loginForm.register('email')} />
                <small className="field__error">{loginForm.formState.errors.email?.message}</small>
              </label>
              <label className="field">
                <span>Password</span>
                <input autoComplete="current-password" placeholder="Enter your password" type="password" {...loginForm.register('password')} />
                <small className="field__error">{loginForm.formState.errors.password?.message}</small>
              </label>
              <label className="field">
                <span>MFA code</span>
                <input placeholder="Enter your code if prompted" type="text" {...loginForm.register('mfaCode')} />
                <small>Use this when your portal security policy asks for an additional code.</small>
              </label>
              <button className="button button--primary button--full" disabled={loginForm.formState.isSubmitting} type="submit">
                {loginForm.formState.isSubmitting ? 'Signing in...' : 'Sign in'}
              </button>
              <div className="login-help">
                <small>Don't have an account? <Link to="/register">Create an account</Link></small>
              </div>
            </form>
          </>
        ) : null}

        {mode === 'register' ? (
          <>
            <ErrorCallout error={registrationForm.formState.errors.root?.message} />
            <form
              className="login-form"
              onSubmit={registrationForm.handleSubmit(async (values) => {
                try {
                  const registration = await createPortalSelfRegistration({
                    displayName: values.displayName,
                    email: values.email,
                    phoneNumber: values.phoneNumber,
                    password: values.password,
                  });
                  setPendingRegistration({
                    registration,
                    email: values.email,
                    password: values.password,
                  });
                  otpForm.reset({ otpCode: '' });
                  setMode('verify');
                } catch (error) {
                  registrationForm.setError('root', {
                    message: error instanceof Error ? error.message : 'Registration failed.',
                  });
                }
              })}
            >
              <label className="field">
                <span>Full name</span>
                <input autoComplete="name" placeholder="Investor full name" {...registrationForm.register('displayName')} />
                <small className="field__error">{registrationForm.formState.errors.displayName?.message}</small>
              </label>
              <label className="field">
                <span>Email</span>
                <input autoComplete="email" placeholder="investor@example.com" type="email" {...registrationForm.register('email')} />
                <small className="field__error">{registrationForm.formState.errors.email?.message}</small>
              </label>
              <label className="field">
                <span>Mobile number</span>
                <input autoComplete="tel" placeholder="+2547..." {...registrationForm.register('phoneNumber')} />
                <small className="field__error">{registrationForm.formState.errors.phoneNumber?.message}</small>
              </label>
              <label className="field">
                <span>Password</span>
                <input autoComplete="new-password" placeholder="Create a strong password" type="password" {...registrationForm.register('password')} />
                <small className="field__error">{registrationForm.formState.errors.password?.message}</small>
              </label>
              <label className="field">
                <span>Confirm password</span>
                <input autoComplete="new-password" placeholder="Repeat your password" type="password" {...registrationForm.register('confirmPassword')} />
                <small className="field__error">{registrationForm.formState.errors.confirmPassword?.message}</small>
              </label>
              <button className="button button--primary button--full" disabled={registrationForm.formState.isSubmitting} type="submit">
                {registrationForm.formState.isSubmitting ? 'Sending OTP...' : 'Create account'}
              </button>
            </form>
          </>
        ) : null}

        {mode === 'verify' ? (
          <>
            <div className="mini-card">
              <strong>Verify your account</strong>
              <p>
                We sent a one-time code to {pendingRegistration?.registration.maskedPhoneNumber ?? 'your phone'}.
                Enter it below to activate your portal access.
              </p>
            </div>
            <ErrorCallout error={otpForm.formState.errors.root?.message} />
            <form
              className="login-form"
              onSubmit={otpForm.handleSubmit(async (values) => {
                if (!pendingRegistration) {
                  otpForm.setError('root', { message: 'Start registration first.' });
                  return;
                }

                try {
                  await verifyPortalSelfRegistrationOtp({
                    registrationId: pendingRegistration.registration.registrationId,
                    otpCode: values.otpCode,
                  });
                  await login(pendingRegistration.email, pendingRegistration.password, null);
                  navigate(nextPath, { replace: true });
                } catch (error) {
                  otpForm.setError('root', {
                    message: error instanceof Error ? error.message : 'OTP verification failed.',
                  });
                }
              })}
            >
              <label className="field">
                <span>OTP code</span>
                <input autoComplete="one-time-code" inputMode="numeric" placeholder="Enter the verification code" {...otpForm.register('otpCode')} />
                <small className="field__error">{otpForm.formState.errors.otpCode?.message}</small>
              </label>
              <button className="button button--primary button--full" disabled={otpForm.formState.isSubmitting} type="submit">
                {otpForm.formState.isSubmitting ? 'Verifying...' : 'Verify and continue'}
              </button>
              <button className="button button--full" type="button" onClick={() => setMode('register')}>
                Edit registration details
              </button>
            </form>
          </>
        ) : null}
      </section>
    </div>
  );
}
