namespace Cis.Domain.Workflows;

public enum WorkflowType
{
    SchemeClassSetupOrAmendment = 1,
    InvestorApprovalRiskClassificationChange = 2,
    ManualFeeWaiverPricingOverride = 3,
    BackdatedPostingCorrectionJournal = 4,
    LargeExceptionalRedemption = 5,
    NavPreparationApprovalPublication = 6,
    UserAccessChange = 7,
    ReportPublicationRegulatorPackIssuance = 8,
    PortalDigitalServiceRequest = 9
}
