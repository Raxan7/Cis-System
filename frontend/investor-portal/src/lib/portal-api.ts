import { request, requestEnvelope, type PaginationMeta } from './http';

export const portalPermissions = {
  read: 'Portal.Read',
  requestsCreate: 'Portal.Requests.Create',
  documentsUpload: 'Portal.Documents.Upload',
} as const;

export type PaginationQuery = {
  pageNumber: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
};

export type PaginatedResult<T> = {
  data: T[];
  pagination: PaginationMeta | null;
};

export type CreatePortalSelfRegistrationRequest = {
  displayName: string;
  email: string;
  phoneNumber: string;
  password: string;
};

export type VerifyPortalSelfRegistrationOtpRequest = {
  registrationId: string;
  otpCode: string;
};

export type PortalSelfRegistrationInitiatedDto = {
  registrationId: string;
  displayName: string;
  email: string;
  maskedPhoneNumber: string;
  status: string;
  otpSentAtUtc: string;
  otpExpiresAtUtc: string;
};

export type PortalSelfRegistrationActivationDto = {
  registrationId: string;
  userId: string;
  investorId: string;
  investorNumber: string;
  investorStatus: string;
  portalProfileStatus: string;
  email: string;
};

export type PortalProfileDto = {
  userId: string;
  investorId: string;
  investorNumber: string;
  displayName: string;
  email: string;
  phoneNumber: string;
  investorStatus: string;
  mfaRequired: boolean;
  mfaSatisfied: boolean;
};

export type PortalFundNavDto = {
  schemeId: string;
  schemeCode: string;
  schemeName: string;
  schemeClassId: string;
  schemeClassCode: string;
  schemeClassName: string;
  currency: string;
  schemeStatus: string;
  publishedNav: number;
  publishedUnitPrice: number;
  valuationDate: string;
  publishedAtUtc: string;
  valuationFrequency: string;
  dealingFrequency: string;
};

export type PortalHoldingDto = {
  schemeId: string;
  schemeCode: string;
  schemeName: string;
  schemeClassId: string;
  schemeClassCode: string;
  schemeClassName: string;
  currency: string;
  units: number;
  lienedUnits: number;
  redeemableUnits: number;
  unitPrice?: number | null;
  marketValue?: number | null;
  redeemableAmount?: number | null;
  unitPrecision: number;
  latestValuationDate?: string | null;
  lastMovementDate?: string | null;
  lastTransactionReference?: string | null;
};

export type PortalPortfolioPositionDto = {
  schemeId: string;
  schemeCode: string;
  schemeName: string;
  schemeClassId: string;
  schemeClassCode: string;
  schemeClassName: string;
  currency: string;
  units: number;
  lienedUnits: number;
  redeemableUnits: number;
  unitPrice?: number | null;
  marketValue?: number | null;
  redeemableAmount?: number | null;
  unitPrecision: number;
  latestValuationDate?: string | null;
};

export type PortalPortfolioSummaryDto = {
  investorId: string;
  investorNumber: string;
  investorStatus: string;
  totalUnits: number;
  totalLienedUnits: number;
  totalRedeemableUnits: number;
  totalMarketValue: number;
  totalRedeemableAmount: number;
  totalNetContribution: number;
  estimatedCapitalGain: number;
  latestValuationDate?: string | null;
  positions: PortalPortfolioPositionDto[];
};

export type PortalTransactionDto = {
  id: string;
  source: string;
  type: string;
  schemeId: string;
  schemeClassId: string;
  businessDate: string;
  amount?: number | null;
  units?: number | null;
  status?: string | null;
  reference: string;
};

export type PortalStatementDto = {
  id: string;
  investorId: string;
  statementDate: string;
  statementReference: string;
  totalUnits: number;
  holdingCount: number;
};

export type PortalTaxCertificateDto = {
  id: string;
  investorId: string;
  taxNumber: string;
  countryOfTaxResidence: string;
  certificateDate: string;
  certificateReference: string;
};

export type PortalInvestorBankAccountDto = {
  id: string;
  bankName: string;
  accountNumber: string;
  accountName: string;
  currency: string;
  swiftCode?: string | null;
  isActive: boolean;
  highRiskFlag: boolean;
};

export type PortalKycRequirementStatusDto = {
  id: string;
  documentType: string;
  isMandatory: boolean;
  status: string;
  satisfiedByDocumentId?: string | null;
};

export type PortalKycDocumentStatusDto = {
  id: string;
  documentType: string;
  fileName: string;
  expiryDate?: string | null;
  status: string;
  uploadedAtUtc: string;
};

export type PortalKycProfileDto = {
  investorId: string;
  investorNumber: string;
  investorStatus: string;
  investorType: string;
  displayName: string;
  email: string;
  phoneNumber: string;
  identityNumber?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  nationality?: string | null;
  taxNumber?: string | null;
  countryOfTaxResidence?: string | null;
  addressLine1?: string | null;
  alternatePhoneNumber?: string | null;
  nextOfKinName?: string | null;
  nextOfKinPhoneNumber?: string | null;
  nextOfKinRelationship?: string | null;
  canRedeem: boolean;
  redemptionBlockedReason?: string | null;
  bankAccounts: PortalInvestorBankAccountDto[];
  kycRequirements: PortalKycRequirementStatusDto[];
  kycDocuments: PortalKycDocumentStatusDto[];
};

export type InvestorNoticeDto = {
  id: string;
  title: string;
  body: string;
  publishedDate: string;
  publishedAtUtc: string;
};

export type DigitalServiceRequestDto = {
  id: string;
  investorId: string;
  userId: string;
  requestType: string;
  status: string;
  workflowId?: string | null;
  requestPayloadJson: string;
  submittedAtUtc: string;
};

export type PortalActivityLogDto = {
  id: string;
  activityType: string;
  summary: string;
  entityType?: string | null;
  entityId?: string | null;
  occurredAtUtc: string;
};

export type CreatePortalSubscriptionRequest = {
  schemeId: string;
  schemeClassId: string;
  amount: number;
  currency: string;
};

export type CreatePortalRedemptionRequest = {
  schemeId: string;
  schemeClassId: string;
  amount?: number | null;
  units?: number | null;
  fullRedemption: boolean;
  currency: string;
};

export type CreatePortalSwitchRequest = {
  sourceSchemeId: string;
  sourceSchemeClassId: string;
  targetSchemeId: string;
  targetSchemeClassId: string;
  amount?: number | null;
  units?: number | null;
  feeAmount: number;
};

export type CreatePortalTransferRequest = {
  schemeId: string;
  schemeClassId: string;
  targetInvestorNumber: string;
  units: number;
  reason: string;
};

export type CreatePortalProfileUpdateRequest = {
  displayName: string;
  email: string;
  phoneNumber: string;
  identityNumber?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  nationality?: string | null;
  taxNumber?: string | null;
  countryOfTaxResidence?: string | null;
  addressLine1?: string | null;
  alternatePhoneNumber?: string | null;
  nextOfKinName?: string | null;
  nextOfKinPhoneNumber?: string | null;
  nextOfKinRelationship?: string | null;
  bankName?: string | null;
  bankAccountNumber?: string | null;
  bankAccountName?: string | null;
  bankCurrency?: string | null;
  bankSwiftCode?: string | null;
  reason: string;
};

export type UploadPortalDocumentRequest = {
  documentType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  storageReference: string;
};

async function getPaged<T>(path: string, query: PaginationQuery) {
  const response = await requestEnvelope<T[]>(path, {
    query: {
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
      search: query.search,
      sortBy: query.sortBy,
      sortDirection: query.sortDirection,
    },
  });

  return {
    data: response.data,
    pagination: response.meta.pagination ?? null,
  } satisfies PaginatedResult<T>;
}

export function getPortalProfile() {
  return request<PortalProfileDto>('/api/portal/me');
}

export function getPortalFundNav(query: PaginationQuery) {
  return getPaged<PortalFundNavDto>('/api/portal/fund-nav', query);
}

export function getPortalPortfolio() {
  return request<PortalPortfolioSummaryDto>('/api/portal/portfolio');
}

export function getPortalKycProfile() {
  return request<PortalKycProfileDto>('/api/portal/kyc');
}

export function createPortalSelfRegistration(body: CreatePortalSelfRegistrationRequest) {
  return request<PortalSelfRegistrationInitiatedDto>('/api/portal/self-registration', { method: 'POST', body }, false);
}

export function verifyPortalSelfRegistrationOtp(body: VerifyPortalSelfRegistrationOtpRequest) {
  return request<PortalSelfRegistrationActivationDto>('/api/portal/self-registration/verify-otp', { method: 'POST', body }, false);
}

export function getPortalHoldings(query: PaginationQuery) {
  return getPaged<PortalHoldingDto>('/api/portal/holdings', query);
}

export function getPortalTransactions(query: PaginationQuery) {
  return getPaged<PortalTransactionDto>('/api/portal/transactions', query);
}

export function getPortalStatements(query: PaginationQuery) {
  return getPaged<PortalStatementDto>('/api/portal/statements', query);
}

export function getPortalTaxCertificates(query: PaginationQuery) {
  return getPaged<PortalTaxCertificateDto>('/api/portal/tax-certificates', query);
}

export function getPortalNotices(query: PaginationQuery) {
  return getPaged<InvestorNoticeDto>('/api/portal/notices', query);
}

export function getPortalActivity(query: PaginationQuery) {
  return getPaged<PortalActivityLogDto>('/api/portal/activity', query);
}

export function createPortalSubscriptionRequest(body: CreatePortalSubscriptionRequest) {
  return request<DigitalServiceRequestDto>('/api/portal/requests/subscription', { method: 'POST', body });
}

export function createPortalRedemptionRequest(body: CreatePortalRedemptionRequest) {
  return request<DigitalServiceRequestDto>('/api/portal/requests/redemption', { method: 'POST', body });
}

export function createPortalSwitchRequest(body: CreatePortalSwitchRequest) {
  return request<DigitalServiceRequestDto>('/api/portal/requests/switch', { method: 'POST', body });
}

export function createPortalTransferRequest(body: CreatePortalTransferRequest) {
  return request<DigitalServiceRequestDto>('/api/portal/requests/transfer', { method: 'POST', body });
}

export function createPortalProfileUpdateRequest(body: CreatePortalProfileUpdateRequest) {
  return request<DigitalServiceRequestDto>('/api/portal/requests/profile-update', { method: 'POST', body });
}

export function uploadPortalDocument(body: UploadPortalDocumentRequest) {
  return request<DigitalServiceRequestDto>('/api/portal/documents', { method: 'POST', body });
}
