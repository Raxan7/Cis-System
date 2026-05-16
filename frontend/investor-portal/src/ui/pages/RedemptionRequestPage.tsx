import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createPortalRedemptionRequest, getPortalHoldings } from '../../lib/portal-api';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { formatNumber } from '../../lib/format';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  schemeId: z.string().uuid('Enter a valid scheme id.'),
  schemeClassId: z.string().uuid('Enter a valid scheme class id.'),
  amount: z.string().trim().optional(),
  units: z.string().trim().optional(),
  fullRedemption: z.boolean(),
  currency: z.string().trim().length(3, 'Use a 3-letter currency code.'),
}).superRefine((value, context) => {
  const amount = value.amount ? Number(value.amount) : null;
  const units = value.units ? Number(value.units) : null;

  if (value.amount && (!Number.isFinite(amount) || (amount !== null && amount < 0))) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Amount must be zero or greater.',
      path: ['amount'],
    });
  }

  if (value.units && (!Number.isFinite(units) || (units !== null && units < 0))) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Units must be zero or greater.',
      path: ['units'],
    });
  }

  if (!value.fullRedemption && !value.amount && !value.units) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Enter either an amount or a unit quantity unless this is a full redemption.',
      path: ['amount'],
    });
  }
});

type FormValues = z.infer<typeof schema>;

export function RedemptionRequestPage() {
  const navigate = useNavigate();
  const holdingsQuery = useQuery({
    queryKey: ['portal', 'holdings', 'redemption-form'],
    queryFn: () => getPortalHoldings({ pageNumber: 1, pageSize: 100 }),
  });
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      schemeId: '',
      schemeClassId: '',
      amount: '',
      units: '',
      fullRedemption: false,
      currency: 'KES',
    },
  });
  const mutation = useMutation({
    mutationFn: createPortalRedemptionRequest,
    onSuccess: (request) => {
      rememberTrackedRequest(request);
      void navigate('/requests');
    },
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Redemption request"
        title="Submit a redemption request"
        description="Available units, liened units, and redeemable amount are shown below to help you avoid requests that exceed your current redeemable balance."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Available positions</h2>
          <p>Use these holdings as the source for your redemption request.</p>
        </div>
        <div className="mini-grid">
          {(holdingsQuery.data?.data ?? []).map((holding) => (
            <article className="mini-card" key={`${holding.schemeId}:${holding.schemeClassId}`}>
              <strong>{holding.schemeClassId}</strong>
              <p>Redeemable units: {formatNumber(holding.redeemableUnits, holding.unitPrecision)}</p>
              <p>Redeemable amount: {formatNumber(holding.redeemableAmount)}</p>
            </article>
          ))}
        </div>
      </section>
      <section className="panel">
        <div className="panel__header">
          <h2>Redemption details</h2>
          <p>Amounts are estimates until the dealing workflow and pricing cycle complete.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await mutation.mutateAsync({
              schemeId: values.schemeId,
              schemeClassId: values.schemeClassId,
              amount: values.amount ? Number(values.amount) : null,
              units: values.units ? Number(values.units) : null,
              fullRedemption: values.fullRedemption,
              currency: values.currency.toUpperCase(),
            });
          })}
        >
          <label className="field">
            <span>Holding</span>
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
              <option value="">Select a holding</option>
              {(holdingsQuery.data?.data ?? []).map((holding) => (
                <option key={`${holding.schemeId}:${holding.schemeClassId}`} value={`${holding.schemeId}|${holding.schemeClassId}`}>
                  {holding.schemeClassId} · {formatNumber(holding.redeemableUnits, holding.unitPrecision)} redeemable units
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Scheme id</span>
            <input {...form.register('schemeId')} />
            <small className="field__error">{form.formState.errors.schemeId?.message}</small>
          </label>
          <label className="field">
            <span>Scheme class id</span>
            <input {...form.register('schemeClassId')} />
            <small className="field__error">{form.formState.errors.schemeClassId?.message}</small>
          </label>
          <label className="field">
            <span>Amount</span>
            <input step="0.01" type="number" {...form.register('amount')} />
            <small className="field__error">{form.formState.errors.amount?.message}</small>
          </label>
          <label className="field">
            <span>Units</span>
            <input step="0.0001" type="number" {...form.register('units')} />
            <small className="field__error">{form.formState.errors.units?.message}</small>
          </label>
          <label className="field">
            <span>Currency</span>
            <input maxLength={3} {...form.register('currency')} />
            <small className="field__error">{form.formState.errors.currency?.message}</small>
          </label>
          <label className="field field--checkbox">
            <input type="checkbox" {...form.register('fullRedemption')} />
            <span>Full redemption</span>
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
