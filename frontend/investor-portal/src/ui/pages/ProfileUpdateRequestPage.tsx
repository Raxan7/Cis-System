import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createPortalProfileUpdateRequest, getPortalKycProfile } from '../../lib/portal-api';
import { rememberTrackedRequest } from '../../lib/request-tracker';
import { ErrorCallout } from '../components/ErrorCallout';
import { PageIntro } from '../components/PageIntro';

const schema = z.object({
  displayName: z.string().trim().min(3, 'Display name is required.').max(200),
  email: z.string().trim().email('Enter a valid email address.'),
  phoneNumber: z.string().trim().min(5, 'Phone number is required.').max(50),
  identityNumber: z.string().trim().max(100).optional().or(z.literal('')),
  firstName: z.string().trim().max(100).optional().or(z.literal('')),
  lastName: z.string().trim().max(100).optional().or(z.literal('')),
  dateOfBirth: z.string().trim().optional().or(z.literal('')),
  nationality: z.string().trim().max(100).optional().or(z.literal('')),
  taxNumber: z.string().trim().max(100).optional().or(z.literal('')),
  countryOfTaxResidence: z.string().trim().max(100).optional().or(z.literal('')),
  addressLine1: z.string().trim().max(500).optional().or(z.literal('')),
  alternatePhoneNumber: z.string().trim().max(50).optional().or(z.literal('')),
  nextOfKinName: z.string().trim().max(200).optional().or(z.literal('')),
  nextOfKinPhoneNumber: z.string().trim().max(50).optional().or(z.literal('')),
  nextOfKinRelationship: z.string().trim().max(100).optional().or(z.literal('')),
  bankName: z.string().trim().max(200).optional().or(z.literal('')),
  bankAccountNumber: z.string().trim().max(100).optional().or(z.literal('')),
  bankAccountName: z.string().trim().max(200).optional().or(z.literal('')),
  bankCurrency: z.string().trim().max(3).optional().or(z.literal('')),
  bankSwiftCode: z.string().trim().max(20).optional().or(z.literal('')),
  reason: z.string().trim().min(10, 'Explain why the profile update is needed.').max(1000),
});

type FormValues = z.infer<typeof schema>;

export function ProfileUpdateRequestPage() {
  const navigate = useNavigate();
  const profileQuery = useQuery({
    queryKey: ['portal', 'kyc-profile', 'profile-update'],
    queryFn: getPortalKycProfile,
  });
  const profile = profileQuery.data;
  const primaryBank = profile?.bankAccounts[0];
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      displayName: profile?.displayName ?? '',
      email: profile?.email ?? '',
      phoneNumber: profile?.phoneNumber ?? '',
      identityNumber: profile?.identityNumber ?? '',
      firstName: profile?.firstName ?? '',
      lastName: profile?.lastName ?? '',
      dateOfBirth: profile?.dateOfBirth ?? '',
      nationality: profile?.nationality ?? '',
      taxNumber: profile?.taxNumber ?? '',
      countryOfTaxResidence: profile?.countryOfTaxResidence ?? '',
      addressLine1: profile?.addressLine1 ?? '',
      alternatePhoneNumber: profile?.alternatePhoneNumber ?? '',
      nextOfKinName: profile?.nextOfKinName ?? '',
      nextOfKinPhoneNumber: profile?.nextOfKinPhoneNumber ?? '',
      nextOfKinRelationship: profile?.nextOfKinRelationship ?? '',
      bankName: primaryBank?.bankName ?? '',
      bankAccountNumber: primaryBank?.accountNumber ?? '',
      bankAccountName: primaryBank?.accountName ?? '',
      bankCurrency: primaryBank?.currency ?? '',
      bankSwiftCode: primaryBank?.swiftCode ?? '',
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
        title="Submit a KYC or profile update request"
        description="Update requests can cover NIDA, address, bank details, other phone number, and next-of-kin details. Official records change only after internal approval."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Update request</h2>
          <p>Review the current details, adjust what needs to change, and provide the reason for the amendment.</p>
        </div>
        <ErrorCallout error={mutation.error} />
        <form
          className="form-grid"
          onSubmit={form.handleSubmit(async (values) => {
            await mutation.mutateAsync({
              ...values,
              dateOfBirth: values.dateOfBirth || null,
              identityNumber: values.identityNumber || null,
              firstName: values.firstName || null,
              lastName: values.lastName || null,
              nationality: values.nationality || null,
              taxNumber: values.taxNumber || null,
              countryOfTaxResidence: values.countryOfTaxResidence || null,
              addressLine1: values.addressLine1 || null,
              alternatePhoneNumber: values.alternatePhoneNumber || null,
              nextOfKinName: values.nextOfKinName || null,
              nextOfKinPhoneNumber: values.nextOfKinPhoneNumber || null,
              nextOfKinRelationship: values.nextOfKinRelationship || null,
              bankName: values.bankName || null,
              bankAccountNumber: values.bankAccountNumber || null,
              bankAccountName: values.bankAccountName || null,
              bankCurrency: values.bankCurrency ? values.bankCurrency.toUpperCase() : null,
              bankSwiftCode: values.bankSwiftCode || null,
            });
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
          <label className="field">
            <span>NIDA / National ID</span>
            <input {...form.register('identityNumber')} />
          </label>
          <label className="field">
            <span>First name</span>
            <input {...form.register('firstName')} />
          </label>
          <label className="field">
            <span>Last name</span>
            <input {...form.register('lastName')} />
          </label>
          <label className="field">
            <span>Date of birth</span>
            <input type="date" {...form.register('dateOfBirth')} />
          </label>
          <label className="field">
            <span>Nationality</span>
            <input {...form.register('nationality')} />
          </label>
          <label className="field">
            <span>Tax number</span>
            <input {...form.register('taxNumber')} />
          </label>
          <label className="field">
            <span>Country of tax residence</span>
            <input {...form.register('countryOfTaxResidence')} />
          </label>
          <label className="field field--full">
            <span>Address</span>
            <textarea rows={3} {...form.register('addressLine1')} />
          </label>
          <label className="field">
            <span>Other phone number</span>
            <input {...form.register('alternatePhoneNumber')} />
          </label>
          <label className="field">
            <span>Next of kin name</span>
            <input {...form.register('nextOfKinName')} />
          </label>
          <label className="field">
            <span>Next of kin phone</span>
            <input {...form.register('nextOfKinPhoneNumber')} />
          </label>
          <label className="field">
            <span>Next of kin relationship</span>
            <input {...form.register('nextOfKinRelationship')} />
          </label>
          <label className="field">
            <span>Bank name</span>
            <input {...form.register('bankName')} />
          </label>
          <label className="field">
            <span>Bank account number</span>
            <input {...form.register('bankAccountNumber')} />
          </label>
          <label className="field">
            <span>Bank account name</span>
            <input {...form.register('bankAccountName')} />
          </label>
          <label className="field">
            <span>Bank currency</span>
            <input maxLength={3} {...form.register('bankCurrency')} />
          </label>
          <label className="field">
            <span>Bank swift code</span>
            <input {...form.register('bankSwiftCode')} />
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
