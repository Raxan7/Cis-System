import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createPortalSubscriptionRequest, getPortalHoldings } from '../../lib/portal-api';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  schemeId: z.string().uuid('Enter a valid scheme id.'),
  schemeClassId: z.string().uuid('Enter a valid scheme class id.'),
  amount: z.string().trim().refine((value) => Number(value) > 0, 'Amount must be greater than zero.'),
  currency: z.string().trim().length(3, 'Use a 3-letter currency code.'),
});

type FormValues = z.infer<typeof schema>;

export function SubscriptionRequestPage() {
  const navigate = useNavigate();
  const holdingsQuery = useQuery({
    queryKey: ['portal', 'holdings', 'subscription-form'],
    queryFn: () => getPortalHoldings({ pageNumber: 1, pageSize: 100 }),
  });
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      schemeId: '',
      schemeClassId: '',
      amount: '',
      currency: 'KES',
    },
  });
  const mutation = useMutation({
    mutationFn: createPortalSubscriptionRequest,
    onSuccess: (request) => {
      rememberTrackedRequest(request);
      void navigate('/requests');
    },
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Subscription request"
        title="Submit a subscription request"
        description="Requests are routed into workflow approval immediately and will remain pending until the internal dealing process clears them."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Subscription details</h2>
          <p>Choose a known scheme/class from your portfolio history or enter the destination identifiers provided by your relationship manager.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await mutation.mutateAsync({
              schemeId: values.schemeId,
              schemeClassId: values.schemeClassId,
              amount: Number(values.amount),
              currency: values.currency.toUpperCase(),
            });
          })}
        >
          <label className="field">
            <span>Known holdings</span>
            <select
              defaultValue=""
              onChange={(event) => {
                const [schemeId, schemeClassId] = event.target.value.split('|');
                if (schemeId && schemeClassId) {
                  form.setValue('schemeId', schemeId, { shouldValidate: true });
                  form.setValue('schemeClassId', schemeClassId, { shouldValidate: true });
                }
              }}
            >
              <option value="">Select a known scheme/class</option>
              {(holdingsQuery.data?.data ?? []).map((holding) => (
                <option key={`${holding.schemeId}:${holding.schemeClassId}`} value={`${holding.schemeId}|${holding.schemeClassId}`}>
                  {holding.schemeClassId} ({holding.schemeId})
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Scheme id</span>
            <input placeholder="Scheme UUID" {...form.register('schemeId')} />
            <small className="field__error">{form.formState.errors.schemeId?.message}</small>
          </label>
          <label className="field">
            <span>Scheme class id</span>
            <input placeholder="Scheme class UUID" {...form.register('schemeClassId')} />
            <small className="field__error">{form.formState.errors.schemeClassId?.message}</small>
          </label>
          <label className="field">
            <span>Amount</span>
            <input step="0.01" type="number" {...form.register('amount')} />
            <small className="field__error">{form.formState.errors.amount?.message}</small>
          </label>
          <label className="field">
            <span>Currency</span>
            <input maxLength={3} {...form.register('currency')} />
            <small className="field__error">{form.formState.errors.currency?.message}</small>
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
