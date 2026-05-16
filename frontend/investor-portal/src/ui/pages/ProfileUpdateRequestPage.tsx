import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createPortalProfileUpdateRequest, getPortalProfile } from '../../lib/portal-api';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  displayName: z.string().trim().min(3, 'Display name is required.').max(200),
  email: z.string().trim().email('Enter a valid email address.'),
  phoneNumber: z.string().trim().min(5, 'Phone number is required.').max(50),
  reason: z.string().trim().min(10, 'Explain why the profile update is needed.').max(1000),
});

type FormValues = z.infer<typeof schema>;

export function ProfileUpdateRequestPage() {
  const navigate = useNavigate();
  const profileQuery = useQuery({
    queryKey: ['portal', 'me', 'profile-update'],
    queryFn: getPortalProfile,
  });
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      displayName: profileQuery.data?.displayName ?? '',
      email: profileQuery.data?.email ?? '',
      phoneNumber: profileQuery.data?.phoneNumber ?? '',
      reason: '',
    },
  });
  const mutation = useMutation({
    mutationFn: createPortalProfileUpdateRequest,
    onSuccess: (request) => {
      rememberTrackedRequest(request);
      void navigate('/requests');
    },
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Profile update"
        title="Submit a profile update request"
        description="Profile changes are reviewed internally first; they do not change official investor records until the workflow is approved."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Update request</h2>
          <p>Confirm the new profile details and provide a reason for the change.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await mutation.mutateAsync(values);
          })}
        >
          <label className="field">
            <span>Display name</span>
            <input {...form.register('displayName')} />
            <small className="field__error">{form.formState.errors.displayName?.message}</small>
          </label>
          <label className="field">
            <span>Email</span>
            <input type="email" {...form.register('email')} />
            <small className="field__error">{form.formState.errors.email?.message}</small>
          </label>
          <label className="field">
            <span>Phone number</span>
            <input {...form.register('phoneNumber')} />
            <small className="field__error">{form.formState.errors.phoneNumber?.message}</small>
          </label>
          <label className="field field--full">
            <span>Reason</span>
            <textarea rows={5} {...form.register('reason')} />
            <small className="field__error">{form.formState.errors.reason?.message}</small>
          </label>
          <div className="form-actions">
            <button className="button button--primary" disabled={mutation.isPending} type="submit">
              {mutation.isPending ? 'Submitting...' : 'Submit request'}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
