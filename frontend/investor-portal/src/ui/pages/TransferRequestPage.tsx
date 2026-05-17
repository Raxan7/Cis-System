import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createPortalTransferRequest, getPortalHoldings } from '../../lib/portal-api';
import { formatNumber } from '../../lib/format';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';

const schema = z.object({
  schemeId: z.string().uuid('Select a valid scheme.'),
  schemeClassId: z.string().uuid('Select a valid scheme class.'),
  targetInvestorNumber: z.string().trim().min(3, 'Target investor number is required.').max(50),
  units: z.string().trim().refine((value) => Number(value) > 0, 'Transfer units must be greater than zero.'),
  reason: z.string().trim().min(10, 'Provide a reason for the transfer.').max(1000),
});

type FormValues = z.infer<typeof schema>;

export function TransferRequestPage() {
  const navigate = useNavigate();
  const holdingsQuery = useQuery({
    queryKey: ['portal', 'holdings', 'transfer-form'],
    queryFn: () => getPortalHoldings({ pageNumber: 1, pageSize: 100 }),
  });
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      schemeId: '',
      schemeClassId: '',
      targetInvestorNumber: '',
      units: '',
      reason: '',
    },
  });
  const mutation = useMutation({
    mutationFn: createPortalTransferRequest,
    onSuccess: (request) => {
      rememberTrackedRequest(request);
      void navigate('/requests');
    },
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Transfer request"
        title="Submit a transfer request"
        description="Transfers are workflow-backed and reviewed internally before any official unit-register movement is posted."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Eligible holdings</h2>
          <p>Use redeemable units as your guide when preparing a transfer request.</p>
        </div>
        <div className="mini-grid">
          {(holdingsQuery.data?.data ?? []).map((holding) => (
            <article className="mini-card" key={`${holding.schemeId}:${holding.schemeClassId}`}>
              <strong>{holding.schemeName}</strong>
              <p>{holding.schemeClassName}</p>
              <p>Redeemable units: {formatNumber(holding.redeemableUnits, holding.unitPrecision)}</p>
            </article>
          ))}
        </div>
      </section>
      <section className="panel">
        <div className="panel__header">
          <h2>Transfer details</h2>
          <p>Enter the destination investor number and the units to move.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await mutation.mutateAsync({
              schemeId: values.schemeId,
              schemeClassId: values.schemeClassId,
              targetInvestorNumber: values.targetInvestorNumber.trim().toUpperCase(),
              units: Number(values.units),
              reason: values.reason,
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
                  form.setValue('schemeId', schemeId, { shouldValidate: true });
                  form.setValue('schemeClassId', schemeClassId, { shouldValidate: true });
                }
              }}
            >
              <option value="">Select a holding</option>
              {(holdingsQuery.data?.data ?? []).map((holding) => (
                <option key={`${holding.schemeId}:${holding.schemeClassId}`} value={`${holding.schemeId}|${holding.schemeClassId}`}>
                  {holding.schemeName} / {holding.schemeClassName}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Scheme</span>
            <input {...form.register('schemeId')} />
            <small className="field__error">{form.formState.errors.schemeId?.message}</small>
          </label>
          <label className="field">
            <span>Scheme class</span>
            <input {...form.register('schemeClassId')} />
            <small className="field__error">{form.formState.errors.schemeClassId?.message}</small>
          </label>
          <label className="field">
            <span>Target investor number</span>
            <input placeholder="INV-..." {...form.register('targetInvestorNumber')} />
            <small className="field__error">{form.formState.errors.targetInvestorNumber?.message}</small>
          </label>
          <label className="field">
            <span>Units</span>
            <input step="0.0001" type="number" {...form.register('units')} />
            <small className="field__error">{form.formState.errors.units?.message}</small>
          </label>
          <label className="field field--full">
            <span>Reason</span>
            <textarea rows={4} {...form.register('reason')} />
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
