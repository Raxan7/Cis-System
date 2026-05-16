import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createPortalSwitchRequest, getPortalHoldings } from '../../lib/portal-api';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  sourceSchemeId: z.string().uuid('Enter a valid source scheme id.'),
  sourceSchemeClassId: z.string().uuid('Enter a valid source scheme class id.'),
  targetSchemeId: z.string().uuid('Enter a valid target scheme id.'),
  targetSchemeClassId: z.string().uuid('Enter a valid target scheme class id.'),
  amount: z.string().trim().optional(),
  units: z.string().trim().optional(),
  feeAmount: z.string().trim(),
}).superRefine((value, context) => {
  const amount = value.amount ? Number(value.amount) : null;
  const units = value.units ? Number(value.units) : null;
  const feeAmount = Number(value.feeAmount);

  if (!Number.isFinite(feeAmount) || feeAmount < 0) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Fee amount must be zero or greater.',
      path: ['feeAmount'],
    });
  }

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

  if (!value.amount && !value.units) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Enter either an amount or a unit quantity for the switch.',
      path: ['amount'],
    });
  }
});

type FormValues = z.infer<typeof schema>;

export function SwitchRequestPage() {
  const navigate = useNavigate();
  const holdingsQuery = useQuery({
    queryKey: ['portal', 'holdings', 'switch-form'],
    queryFn: () => getPortalHoldings({ pageNumber: 1, pageSize: 100 }),
  });
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      sourceSchemeId: '',
      sourceSchemeClassId: '',
      targetSchemeId: '',
      targetSchemeClassId: '',
      amount: '',
      units: '',
      feeAmount: '0',
    },
  });
  const mutation = useMutation({
    mutationFn: createPortalSwitchRequest,
    onSuccess: (request) => {
      rememberTrackedRequest(request);
      void navigate('/requests');
    },
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Switch request"
        title="Submit a switch request"
        description="Switches create a digital request only. Final dealing, pricing, and approval remain under controlled workflow on the backend."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Switch details</h2>
          <p>Choose the source holding from your current portfolio, then provide the destination scheme/class identifiers.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await mutation.mutateAsync({
              sourceSchemeId: values.sourceSchemeId,
              sourceSchemeClassId: values.sourceSchemeClassId,
              targetSchemeId: values.targetSchemeId,
              targetSchemeClassId: values.targetSchemeClassId,
              amount: values.amount ? Number(values.amount) : null,
              units: values.units ? Number(values.units) : null,
              feeAmount: Number(values.feeAmount),
            });
          })}
        >
          <label className="field">
            <span>Source holding</span>
            <select
              defaultValue=""
              onChange={(event) => {
                const [schemeId, schemeClassId] = event.target.value.split('|');
                if (schemeId && schemeClassId) {
                  form.setValue('sourceSchemeId', schemeId, { shouldValidate: true });
                  form.setValue('sourceSchemeClassId', schemeClassId, { shouldValidate: true });
                }
              }}
            >
              <option value="">Select a source holding</option>
              {(holdingsQuery.data?.data ?? []).map((holding) => (
                <option key={`${holding.schemeId}:${holding.schemeClassId}`} value={`${holding.schemeId}|${holding.schemeClassId}`}>
                  {holding.schemeClassId} ({holding.schemeId})
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Source scheme id</span>
            <input {...form.register('sourceSchemeId')} />
            <small className="field__error">{form.formState.errors.sourceSchemeId?.message}</small>
          </label>
          <label className="field">
            <span>Source scheme class id</span>
            <input {...form.register('sourceSchemeClassId')} />
            <small className="field__error">{form.formState.errors.sourceSchemeClassId?.message}</small>
          </label>
          <label className="field">
            <span>Target scheme id</span>
            <input {...form.register('targetSchemeId')} />
            <small className="field__error">{form.formState.errors.targetSchemeId?.message}</small>
          </label>
          <label className="field">
            <span>Target scheme class id</span>
            <input {...form.register('targetSchemeClassId')} />
            <small className="field__error">{form.formState.errors.targetSchemeClassId?.message}</small>
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
            <span>Estimated fee amount</span>
            <input step="0.01" type="number" {...form.register('feeAmount')} />
            <small className="field__error">{form.formState.errors.feeAmount?.message}</small>
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
