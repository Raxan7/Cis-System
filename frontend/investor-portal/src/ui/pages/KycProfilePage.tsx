import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { formatDate, formatDateOnly } from '../../lib/format';
import { getPortalKycProfile } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';
import { StatusBadge } from '../components/StatusBadge';

export function KycProfilePage() {
  const query = useQuery({
    queryKey: ['portal', 'kyc-profile'],
    queryFn: getPortalKycProfile,
  });

  const profile = query.data;

  return (
    <div className="stack">
      <PageIntro
        eyebrow="KYC profile"
        title="Investor KYC and account details"
        description="Review your official KYC record, bank details, phone numbers, and document requirements before updating anything through workflow."
      />
      <ErrorCallout error={query.error} />
      {profile ? (
        <>
          <section className="panel">
            <div className="panel__header panel__header--split">
              <div>
                <h2>KYC status</h2>
                <p>Your withdrawal access remains tied to KYC completion and investor approval.</p>
              </div>
              <StatusBadge value={profile.canRedeem ? 'Redemption enabled' : 'KYC required'} />
            </div>
            {!profile.canRedeem && profile.redemptionBlockedReason ? (
              <div className="callout callout--error">
                <div>
                  <strong>Withdrawal is blocked</strong>
                  <p>{profile.redemptionBlockedReason}</p>
                </div>
              </div>
            ) : null}
            <div className="mini-grid">
              <article className="mini-card"><strong>Investor number</strong><p>{profile.investorNumber}</p></article>
              <article className="mini-card"><strong>Status</strong><p>{profile.investorStatus}</p></article>
              <article className="mini-card"><strong>NIDA / National ID</strong><p>{profile.identityNumber ?? '-'}</p></article>
              <article className="mini-card"><strong>Tax number</strong><p>{profile.taxNumber ?? '-'}</p></article>
              <article className="mini-card"><strong>Primary phone</strong><p>{profile.phoneNumber}</p></article>
              <article className="mini-card"><strong>Other phone</strong><p>{profile.alternatePhoneNumber ?? '-'}</p></article>
            </div>
          </section>
          <section className="panel">
            <div className="panel__header panel__header--split">
              <div>
                <h2>Official profile details</h2>
                <p>These are the current official records. Changes go through approval first.</p>
              </div>
              <div className="inline-actions">
                <Link className="button button--primary" to="/requests/profile-update">Request update</Link>
                <Link className="button" to="/documents">Upload document</Link>
              </div>
            </div>
            <div className="detail-grid">
              <div><span className="eyebrow">Display name</span><strong>{profile.displayName}</strong></div>
              <div><span className="eyebrow">Email</span><strong>{profile.email}</strong></div>
              <div><span className="eyebrow">First name</span><strong>{profile.firstName ?? '-'}</strong></div>
              <div><span className="eyebrow">Last name</span><strong>{profile.lastName ?? '-'}</strong></div>
              <div><span className="eyebrow">Date of birth</span><strong>{formatDateOnly(profile.dateOfBirth)}</strong></div>
              <div><span className="eyebrow">Nationality</span><strong>{profile.nationality ?? '-'}</strong></div>
              <div><span className="eyebrow">Address</span><strong>{profile.addressLine1 ?? '-'}</strong></div>
              <div><span className="eyebrow">Country of tax residence</span><strong>{profile.countryOfTaxResidence ?? '-'}</strong></div>
              <div><span className="eyebrow">Next of kin</span><strong>{profile.nextOfKinName ?? '-'}</strong></div>
              <div><span className="eyebrow">Next of kin phone</span><strong>{profile.nextOfKinPhoneNumber ?? '-'}</strong></div>
              <div><span className="eyebrow">Next of kin relationship</span><strong>{profile.nextOfKinRelationship ?? '-'}</strong></div>
            </div>
          </section>
          <section className="panel">
            <div className="panel__header">
              <h2>Bank details</h2>
              <p>Your registered bank accounts and risk flags.</p>
            </div>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Bank</th>
                    <th>Account</th>
                    <th>Currency</th>
                    <th>Swift</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {profile.bankAccounts.length === 0 ? (
                    <tr><td className="table-empty" colSpan={5}>No bank accounts on file.</td></tr>
                  ) : profile.bankAccounts.map((account) => (
                    <tr key={account.id}>
                      <td><strong>{account.bankName}</strong><div>{account.accountName}</div></td>
                      <td>{account.accountNumber}</td>
                      <td>{account.currency}</td>
                      <td>{account.swiftCode ?? '-'}</td>
                      <td><StatusBadge value={account.highRiskFlag ? 'High risk' : account.isActive ? 'Active' : 'Inactive'} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          <section className="two-column">
            <section className="panel">
              <div className="panel__header">
                <h2>KYC requirements</h2>
                <p>Mandatory documents are checked before approval and withdrawal.</p>
              </div>
              <div className="stack stack--compact">
                {profile.kycRequirements.map((requirement) => (
                  <article className="timeline-entry" key={requirement.id}>
                    <div className="timeline-entry__header">
                      <strong>{requirement.documentType}</strong>
                      <StatusBadge value={requirement.status} />
                    </div>
                    <p>{requirement.isMandatory ? 'Mandatory document' : 'Optional document'}</p>
                  </article>
                ))}
              </div>
            </section>
            <section className="panel">
              <div className="panel__header">
                <h2>Uploaded documents</h2>
                <p>Recent portal and back-office documents linked to your investor record.</p>
              </div>
              <div className="stack stack--compact">
                {profile.kycDocuments.length === 0 ? <p>No documents uploaded yet.</p> : profile.kycDocuments.map((document) => (
                  <article className="timeline-entry" key={document.id}>
                    <div className="timeline-entry__header">
                      <strong>{document.documentType}</strong>
                      <StatusBadge value={document.status} />
                    </div>
                    <p>{document.fileName}</p>
                    <small>Expiry: {formatDateOnly(document.expiryDate)} · Uploaded: {formatDate(document.uploadedAtUtc)}</small>
                  </article>
                ))}
              </div>
            </section>
          </section>
        </>
      ) : null}
    </div>
  );
}
