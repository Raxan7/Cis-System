using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts.Portal;
using Cis.Contracts.Workflows;
using Cis.Domain.Audit;
using Cis.Domain.Dealing;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.NAV;
using Cis.Domain.Portal;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Identity;
using Cis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Portal;

internal sealed class PortalService : IPortalService
{
    private const string ModuleName = "Portal";
    private const string IdentityModule = "Identity";
    private const string SelfRegistrationActor = "portal-self-registration";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CisDbContext _dbContext;
    private readonly IWorkflowService _workflowService;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileSecurityValidator _fileSecurityValidator;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IPasswordPolicyValidator _passwordPolicyValidator;
    private readonly ISmsSender _smsSender;
    private readonly PortalRegistrationOptions _portalRegistrationOptions;

    public PortalService(
        CisDbContext dbContext,
        IWorkflowService workflowService,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IFileSecurityValidator fileSecurityValidator,
        IPasswordHasher<User> passwordHasher,
        IPasswordPolicyValidator passwordPolicyValidator,
        ISmsSender smsSender,
        IOptions<PortalRegistrationOptions> portalRegistrationOptions)
    {
        _dbContext = dbContext;
        _workflowService = workflowService;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _fileSecurityValidator = fileSecurityValidator;
        _passwordHasher = passwordHasher;
        _passwordPolicyValidator = passwordPolicyValidator;
        _smsSender = smsSender;
        _portalRegistrationOptions = portalRegistrationOptions.Value;
    }

    public async Task<PortalSelfRegistrationInitiatedDto> CreateSelfRegistrationAsync(CreatePortalSelfRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        _passwordPolicyValidator.Validate(request.Password, request.Email);

        var now = _dateTimeProvider.UtcNow;
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        await EnsureNoExistingPortalAccountAsync(normalizedEmail, request.PhoneNumber, cancellationToken);

        var registration = await _dbContext.PortalSelfRegistrations
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail || candidate.NormalizedPhoneNumber == normalizedPhoneNumber, cancellationToken);

        if (registration is not null
            && (!string.Equals(registration.NormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(registration.NormalizedPhoneNumber, normalizedPhoneNumber, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("The supplied email address or phone number is already tied to another pending registration.");
        }

        var passwordProbe = User.Create(request.Email, request.DisplayName, "pending", now);
        var passwordHash = _passwordHasher.HashPassword(passwordProbe, request.Password);
        var otpCode = GenerateOtpCode();
        var otpSentAtUtc = now;
        var otpExpiresAtUtc = now.AddMinutes(_portalRegistrationOptions.OtpExpiryMinutes);
        var otpCodeHash = TokenGenerator.Sha256(otpCode);

        var isNewRegistration = registration is null;

        if (registration is null)
        {
            registration = PortalSelfRegistration.Create(
                request.DisplayName,
                request.Email,
                normalizedEmail,
                request.PhoneNumber,
                normalizedPhoneNumber,
                passwordHash,
                otpCodeHash,
                otpSentAtUtc,
                otpExpiresAtUtc,
                SelfRegistrationActor,
                now);
            _dbContext.PortalSelfRegistrations.Add(registration);
        }
        else
        {
            registration.RefreshPendingVerification(
                request.DisplayName,
                request.Email,
                normalizedEmail,
                request.PhoneNumber,
                normalizedPhoneNumber,
                passwordHash,
                otpCodeHash,
                otpSentAtUtc,
                otpExpiresAtUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _smsSender.SendAsync(request.PhoneNumber, BuildOtpMessage(otpCode), cancellationToken);

        var dto = MapSelfRegistration(registration);
        await WriteAuditAsync(
            AuditEventType.Created,
            isNewRegistration ? "PortalSelfRegistrationStarted" : "PortalSelfRegistrationResent",
            nameof(PortalSelfRegistration),
            registration.Id.ToString(),
            null,
            Snapshot(registration),
            "Investor portal self-registration OTP issued.",
            cancellationToken);
        return dto;
    }

    public async Task<PortalSelfRegistrationActivationDto> VerifySelfRegistrationOtpAsync(VerifyPortalSelfRegistrationOtpRequest request, CancellationToken cancellationToken = default)
    {
        var registration = await _dbContext.PortalSelfRegistrations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException("Portal self-registration record was not found.");

        if (registration.Status == PortalSelfRegistrationStatus.Activated && registration.UserId.HasValue && registration.InvestorId.HasValue)
        {
            return await MapActivationAsync(registration, cancellationToken);
        }

        try
        {
            registration.VerifyOtp(TokenGenerator.Sha256(request.OtpCode.Trim()), _dateTimeProvider.UtcNow, _portalRegistrationOptions.MaxFailedOtpAttempts);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                AuditEventType.Rejected,
                "PortalSelfRegistrationOtpRejected",
                nameof(PortalSelfRegistration),
                registration.Id.ToString(),
                null,
                Snapshot(registration),
                exception.Message,
                cancellationToken);
            throw Validation("otpCode", exception.Message);
        }

        await EnsureNoExistingPortalAccountAsync(registration.NormalizedEmail, registration.PhoneNumber, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var role = await _dbContext.Roles.SingleAsync(candidate => candidate.Name == RoleNames.InvestorPortalUser, cancellationToken);
        var user = User.Create(registration.Email, registration.DisplayName, "pending", now);
        user.SetPasswordHash(registration.PasswordHash, requirePasswordChange: false);
        user.AssignRole(role.Id, SelfRegistrationActor, SelfRegistrationActor, now);

        var investor = Investor.Create(GenerateInvestorNumber(), InvestorType.Individual, registration.DisplayName, registration.Email, registration.PhoneNumber, SelfRegistrationActor, now);
        investor.AddDefaultKycRequirements(DefaultKycRequirements());
        investor.AddContact("Email", registration.Email, true, SelfRegistrationActor, now);
        investor.AddContact("Phone", registration.PhoneNumber, true, SelfRegistrationActor, now);

        _dbContext.Users.Add(user);
        _dbContext.Investors.Add(investor);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var profile = PortalUserProfile.Create(user.Id, investor.Id, investor.DisplayName, investor.Email, SelfRegistrationActor, now);
        var mfa = PortalMfaSetting.Create(user.Id, mfaRequired: false, mfaVerified: false, SelfRegistrationActor, now);
        _dbContext.PortalUserProfiles.Add(profile);
        _dbContext.PortalMfaSettings.Add(mfa);
        registration.Activate(user.Id, investor.Id, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteIdentityAuditAsync("UserCreated", "User", user.Id.ToString(), $"Portal self-registration user {user.Email} created.", cancellationToken);
        await WriteIdentityAuditAsync("RoleAssigned", "User", user.Id.ToString(), $"Investor portal role assigned through self-registration for {user.Email}.", cancellationToken);
        await WriteAuditAsync(
            AuditEventType.Created,
            "PortalSelfRegistrationActivated",
            nameof(PortalSelfRegistration),
            registration.Id.ToString(),
            null,
            Snapshot(registration),
            "Investor portal self-registration verified and activated.",
            cancellationToken);

        return new PortalSelfRegistrationActivationDto(
            registration.Id,
            user.Id,
            investor.Id,
            investor.InvestorNumber,
            investor.Status.ToString(),
            profile.Status.ToString(),
            user.Email);
    }

    public async Task<PortalProfileDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        await LogActivityAsync(context, PortalActivityType.ViewedProfile, "Portal profile viewed.", "Investor", context.Investor.Id.ToString(), cancellationToken);
        return MapProfile(context);
    }

    public async Task<IReadOnlyCollection<PortalFundNavDto>> GetFundNavAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var latestPublications = await _dbContext.NavPublications
            .AsNoTracking()
            .OrderByDescending(publication => publication.PublishedAtUtc)
            .ToListAsync(cancellationToken);

        var latestByClass = latestPublications
            .GroupBy(publication => publication.SchemeClassId)
            .Select(group => group.First())
            .ToArray();

        var schemeIds = latestByClass.Select(publication => publication.SchemeId).Distinct().ToArray();
        var classIds = latestByClass.Select(publication => publication.SchemeClassId).Distinct().ToArray();

        var schemes = await _dbContext.Schemes
            .AsNoTracking()
            .Where(scheme => schemeIds.Contains(scheme.Id))
            .Select(scheme => new SchemeSnapshot(scheme.Id, scheme.Code, scheme.Name, scheme.Status))
            .ToListAsync(cancellationToken);

        var schemeClasses = await _dbContext.SchemeClasses
            .AsNoTracking()
            .Where(schemeClass => classIds.Contains(schemeClass.Id))
            .Select(schemeClass => new SchemeClassSnapshot(
                schemeClass.Id,
                schemeClass.SchemeId,
                schemeClass.Code,
                schemeClass.Name,
                schemeClass.Currency,
                schemeClass.ValuationFrequency,
                schemeClass.DealingFrequency))
            .ToListAsync(cancellationToken);

        var schemeMap = schemes.ToDictionary(item => item.Id);
        var classMap = schemeClasses.ToDictionary(item => item.Id);

        var fundNav = latestByClass
            .Where(publication => schemeMap.ContainsKey(publication.SchemeId) && classMap.ContainsKey(publication.SchemeClassId))
            .Select(publication =>
            {
                var scheme = schemeMap[publication.SchemeId];
                var schemeClass = classMap[publication.SchemeClassId];
                return new PortalFundNavDto(
                    scheme.Id,
                    scheme.Code,
                    scheme.Name,
                    schemeClass.Id,
                    schemeClass.Code,
                    schemeClass.Name,
                    schemeClass.Currency,
                    scheme.Status.ToString(),
                    publication.PublishedNav,
                    publication.PublishedUnitPrice,
                    publication.ValuationDate.Value,
                    publication.PublishedAtUtc,
                    schemeClass.ValuationFrequency.ToString(),
                    schemeClass.DealingFrequency.ToString());
            })
            .OrderBy(item => item.SchemeName)
            .ThenBy(item => item.SchemeClassName)
            .ToArray();

        await LogActivityAsync(context, PortalActivityType.ViewedFundNav, "Published fund NAV information viewed.", nameof(NavPublication), null, cancellationToken);
        return fundNav;
    }

    public async Task<PortalPortfolioSummaryDto> GetPortfolioSummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var holdings = await GetHoldingSnapshotsAsync(context.Profile.InvestorId, cancellationToken);
        var dealingInstructions = await _dbContext.DealingInstructions
            .AsNoTracking()
            .Include(instruction => instruction.SubscriptionInstructions)
            .Include(instruction => instruction.RedemptionInstructions)
            .Where(instruction => instruction.InvestorId == context.Profile.InvestorId)
            .ToListAsync(cancellationToken);

        var totalSubscriptions = dealingInstructions
            .Where(instruction => instruction.InstructionType == DealingInstructionType.Subscription
                && instruction.Status == DealingInstructionStatus.Allocated)
            .Select(instruction => instruction.SubscriptionInstructions.Single())
            .Sum(subscription => subscription.Amount ?? ((subscription.AllocatedUnits ?? subscription.Units ?? 0m) * (subscription.ApprovedNavPrice ?? 0m)));

        var totalRedemptions = dealingInstructions
            .Where(instruction => instruction.InstructionType == DealingInstructionType.Redemption
                && instruction.Status == DealingInstructionStatus.Settled)
            .Select(instruction => instruction.RedemptionInstructions.Single().NetPayoutAmount)
            .Sum();

        var totalUnits = holdings.Sum(holding => holding.Units);
        var totalLienedUnits = holdings.Sum(holding => holding.LienedUnits);
        var totalRedeemableUnits = holdings.Sum(holding => holding.RedeemableUnits);
        var totalMarketValue = holdings.Sum(holding => holding.MarketValue ?? 0m);
        var totalRedeemableAmount = holdings.Sum(holding => holding.RedeemableAmount ?? 0m);
        var totalNetContribution = totalSubscriptions - totalRedemptions;
        var estimatedCapitalGain = totalMarketValue - totalNetContribution;
        var latestValuationDate = holdings
            .Where(holding => holding.LatestValuationDate.HasValue)
            .Select(holding => holding.LatestValuationDate!.Value)
            .OrderByDescending(value => value)
            .FirstOrDefault();

        await LogActivityAsync(context, PortalActivityType.ViewedPortfolio, "Investor portfolio summary viewed.", "Investor", context.Investor.Id.ToString(), cancellationToken);
        return new PortalPortfolioSummaryDto(
            context.Investor.Id,
            context.Investor.InvestorNumber,
            context.Investor.Status.ToString(),
            totalUnits,
            totalLienedUnits,
            totalRedeemableUnits,
            totalMarketValue,
            totalRedeemableAmount,
            totalNetContribution,
            estimatedCapitalGain,
            latestValuationDate == default ? null : latestValuationDate,
            holdings.Select(MapPortfolioPosition).ToArray());
    }

    public async Task<PortalKycProfileDto> GetKycProfileAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var investor = await _dbContext.Investors
            .AsNoTracking()
            .Include(candidate => candidate.IndividualProfiles)
            .Include(candidate => candidate.BankAccounts)
            .Include(candidate => candidate.TaxProfiles)
            .Include(candidate => candidate.Contacts)
            .Include(candidate => candidate.KycRequirements)
            .Include(candidate => candidate.KycDocuments)
            .SingleAsync(candidate => candidate.Id == context.Profile.InvestorId, cancellationToken);

        var individual = investor.IndividualProfiles.OrderByDescending(profile => profile.Id).FirstOrDefault();
        var taxProfile = investor.TaxProfiles.OrderByDescending(profile => profile.Id).FirstOrDefault();
        var address = ResolveContactValue(investor.Contacts, "Address");
        var alternatePhone = investor.Contacts
            .Where(contact => string.Equals(contact.ContactType, "Phone", StringComparison.OrdinalIgnoreCase) && !contact.IsPrimary)
            .Select(contact => contact.Value)
            .FirstOrDefault();
        var nextOfKinName = ResolveContactValue(investor.Contacts, "NextOfKinName");
        var nextOfKinPhoneNumber = ResolveContactValue(investor.Contacts, "NextOfKinPhoneNumber");
        var nextOfKinRelationship = ResolveContactValue(investor.Contacts, "NextOfKinRelationship");
        var canRedeem = investor.Status == InvestorStatus.Approved;
        var redemptionBlockedReason = canRedeem ? null : "Complete your KYC and obtain investor approval before submitting a redemption request.";

        await LogActivityAsync(context, PortalActivityType.ViewedKycProfile, "Investor KYC profile viewed.", nameof(Investor), investor.Id.ToString(), cancellationToken);
        return new PortalKycProfileDto(
            investor.Id,
            investor.InvestorNumber,
            investor.Status.ToString(),
            investor.InvestorType.ToString(),
            investor.DisplayName,
            investor.Email,
            investor.PhoneNumber,
            individual?.IdentityNumber,
            individual?.FirstName,
            individual?.LastName,
            individual?.DateOfBirth,
            individual?.Nationality,
            taxProfile?.TaxNumber,
            taxProfile?.CountryOfTaxResidence,
            address,
            alternatePhone,
            nextOfKinName,
            nextOfKinPhoneNumber,
            nextOfKinRelationship,
            canRedeem,
            redemptionBlockedReason,
            investor.BankAccounts.Select(account => new PortalInvestorBankAccountDto(
                account.Id,
                account.BankName,
                account.AccountNumber,
                account.AccountName,
                account.Currency,
                account.SwiftCode,
                account.IsActive,
                account.HighRiskFlag)).ToArray(),
            investor.KycRequirements.Select(requirement => new PortalKycRequirementStatusDto(
                requirement.Id,
                requirement.DocumentType,
                requirement.IsMandatory,
                requirement.Status.ToString(),
                requirement.SatisfiedByDocumentId)).ToArray(),
            investor.KycDocuments
                .OrderByDescending(document => document.UploadedAtUtc)
                .Select(document => new PortalKycDocumentStatusDto(
                    document.Id,
                    document.DocumentType,
                    document.FileName,
                    document.ExpiryDate?.Value,
                    document.Status.ToString(),
                    document.UploadedAtUtc))
                .ToArray());
    }

    public async Task<IReadOnlyCollection<PortalHoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var result = (await GetHoldingSnapshotsAsync(context.Profile.InvestorId, cancellationToken))
            .Select(MapHolding)
            .ToArray();

        await LogActivityAsync(context, PortalActivityType.ViewedHoldings, "Portal holdings viewed.", null, null, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<PortalTransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var ledgerTransactions = await _dbContext.UnitLedgerEntries
            .AsNoTracking()
            .Where(entry => entry.InvestorId == context.Profile.InvestorId)
            .OrderByDescending(entry => entry.ValuationDate)
            .ThenByDescending(entry => entry.PostedAtUtc)
            .Select(entry => new PortalTransactionDto(
                entry.Id,
                "UnitRegister",
                entry.MovementType.ToString(),
                entry.SchemeId,
                entry.SchemeClassId,
                entry.ValuationDate.Value,
                null,
                entry.Units,
                "Posted",
                entry.TransactionReference))
            .ToListAsync(cancellationToken);

        var dealingTransactions = await _dbContext.DealingInstructions
            .AsNoTracking()
            .Include(instruction => instruction.SubscriptionInstructions)
            .Include(instruction => instruction.RedemptionInstructions)
            .Include(instruction => instruction.SwitchInstructions)
            .Where(instruction => instruction.InvestorId == context.Profile.InvestorId)
            .OrderByDescending(instruction => instruction.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        var combined = ledgerTransactions
            .Concat(dealingTransactions.Select(MapDealingTransaction))
            .OrderByDescending(transaction => transaction.BusinessDate)
            .ThenBy(transaction => transaction.Reference)
            .ToArray();
        await LogActivityAsync(context, PortalActivityType.ViewedTransactions, "Portal transactions viewed.", null, null, cancellationToken);
        return combined;
    }

    public async Task<IReadOnlyCollection<PortalStatementDto>> GetStatementsAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var holdings = await _dbContext.UnitHoldings
            .AsNoTracking()
            .Where(holding => holding.InvestorId == context.Profile.InvestorId)
            .ToListAsync(cancellationToken);
        var statement = new PortalStatementDto(
            context.Profile.InvestorId,
            context.Profile.InvestorId,
            DateOnly.FromDateTime(now),
            $"STMT-{context.Investor.InvestorNumber}-{now:yyyyMMdd}",
            holdings.Sum(holding => holding.Units),
            holdings.Count);

        _dbContext.PortalDocumentDownloads.Add(PortalDocumentDownload.Create(context.Profile.InvestorId, context.Profile.UserId, "Statement", statement.StatementReference, now, _currentUserContext.IpAddress));
        await LogActivityAsync(context, PortalActivityType.DownloadedStatement, "Portal statement downloaded.", "PortalStatement", statement.StatementReference, cancellationToken, saveChanges: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Exported, "PortalStatementDownloaded", "PortalStatement", statement.StatementReference, null, Snapshot(statement), "Investor portal statement downloaded.", cancellationToken);
        return [statement];
    }

    public async Task<IReadOnlyCollection<PortalTaxCertificateDto>> GetTaxCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var profiles = await _dbContext.InvestorTaxProfiles
            .AsNoTracking()
            .Where(profile => profile.InvestorId == context.Profile.InvestorId)
            .ToListAsync(cancellationToken);
        var certificates = profiles.Select(profile => new PortalTaxCertificateDto(
            profile.Id,
            profile.InvestorId,
            profile.TaxNumber,
            profile.CountryOfTaxResidence,
            DateOnly.FromDateTime(now),
            $"TAX-{context.Investor.InvestorNumber}-{now:yyyyMMdd}-{profile.TaxNumber}"))
            .ToArray();

        foreach (var certificate in certificates)
        {
            _dbContext.PortalDocumentDownloads.Add(PortalDocumentDownload.Create(context.Profile.InvestorId, context.Profile.UserId, "TaxCertificate", certificate.CertificateReference, now, _currentUserContext.IpAddress));
        }

        await LogActivityAsync(context, PortalActivityType.DownloadedTaxCertificate, "Portal tax certificates downloaded.", "PortalTaxCertificate", context.Profile.InvestorId.ToString(), cancellationToken, saveChanges: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Exported, "PortalTaxCertificatesDownloaded", "PortalTaxCertificate", context.Profile.InvestorId.ToString(), null, Snapshot(certificates), "Investor portal tax certificates downloaded.", cancellationToken);
        return certificates;
    }

    public async Task<IReadOnlyCollection<InvestorNoticeDto>> GetNoticesAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var notices = await _dbContext.InvestorNotices
            .AsNoTracking()
            .Where(notice => notice.Status == InvestorNoticeStatus.Published && (notice.InvestorId == null || notice.InvestorId == context.Profile.InvestorId))
            .OrderByDescending(notice => notice.PublishedDate)
            .Select(notice => new InvestorNoticeDto(notice.Id, notice.Title, notice.Body, notice.PublishedDate.Value, notice.PublishedAtUtc))
            .ToListAsync(cancellationToken);

        await LogActivityAsync(context, PortalActivityType.ViewedNotices, "Portal notices viewed.", null, null, cancellationToken);
        return notices;
    }

    public Task<DigitalServiceRequestDto> CreateSubscriptionRequestAsync(CreatePortalSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0m)
        {
            throw Validation("amount", "Subscription amount must be greater than zero.");
        }

        return CreateDigitalRequestAsync(DigitalServiceRequestType.SubscriptionRequest, request, cancellationToken);
    }

    public async Task<DigitalServiceRequestDto> CreateRedemptionRequestAsync(CreatePortalRedemptionRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.FullRedemption && (!request.Amount.HasValue || request.Amount <= 0m) && (!request.Units.HasValue || request.Units <= 0m))
        {
            throw Validation("redemption", "Partial redemption requires a positive amount or units.");
        }

        var context = await GetPortalContextAsync(cancellationToken);
        EnsureInvestorCanRedeem(context.Investor);
        return await CreateDigitalRequestAsync(context, DigitalServiceRequestType.RedemptionRequest, request, cancellationToken);
    }

    public Task<DigitalServiceRequestDto> CreateSwitchRequestAsync(CreatePortalSwitchRequest request, CancellationToken cancellationToken = default)
    {
        if ((!request.Amount.HasValue || request.Amount <= 0m) && (!request.Units.HasValue || request.Units <= 0m))
        {
            throw Validation("switch", "Switch request requires a positive amount or units.");
        }

        return CreateDigitalRequestAsync(DigitalServiceRequestType.SwitchRequest, request, cancellationToken);
    }

    public async Task<DigitalServiceRequestDto> CreateTransferRequestAsync(CreatePortalTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Units <= 0m)
        {
            throw Validation("units", "Transfer units must be greater than zero.");
        }

        var context = await GetPortalContextAsync(cancellationToken);
        EnsureInvestorCanRedeem(context.Investor);

        var targetInvestorNumber = request.TargetInvestorNumber.Trim().ToUpperInvariant();
        var targetInvestor = await _dbContext.Investors
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.InvestorNumber == targetInvestorNumber, cancellationToken)
            ?? throw Validation("targetInvestorNumber", "Target investor number was not found.");

        if (targetInvestor.Id == context.Investor.Id)
        {
            throw Validation("targetInvestorNumber", "You cannot transfer units to the same investor account.");
        }

        if (targetInvestor.Status != InvestorStatus.Approved)
        {
            throw Validation("targetInvestorNumber", "Target investor must be approved before a transfer request can be submitted.");
        }

        return await CreateDigitalRequestAsync(
            context,
            DigitalServiceRequestType.TransferRequest,
            new
            {
                request.SchemeId,
                request.SchemeClassId,
                TargetInvestorNumber = targetInvestor.InvestorNumber,
                request.Units,
                request.Reason
            },
            cancellationToken);
    }

    public Task<DigitalServiceRequestDto> CreateProfileUpdateRequestAsync(CreatePortalProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return CreateDigitalRequestAsync(DigitalServiceRequestType.ProfileUpdateRequest, request, cancellationToken);
    }

    public Task<DigitalServiceRequestDto> UploadDocumentAsync(UploadPortalDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return UploadDocumentInternalAsync(request, cancellationToken);
    }

    private async Task<DigitalServiceRequestDto> UploadDocumentInternalAsync(UploadPortalDocumentRequest request, CancellationToken cancellationToken)
    {
        await _fileSecurityValidator.ValidateAsync(
            new FileUploadDescriptor(
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.StorageReference,
                "PortalDocumentUpload"),
            cancellationToken);

        return await CreateDigitalRequestAsync(DigitalServiceRequestType.DocumentUploadRequest, request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PortalActivityLogDto>> GetActivityAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var activity = await _dbContext.PortalActivityLogs
            .AsNoTracking()
            .Where(log => log.InvestorId == context.Profile.InvestorId && log.UserId == context.Profile.UserId)
            .OrderByDescending(log => log.OccurredAtUtc)
            .Select(log => new PortalActivityLogDto(log.Id, log.ActivityType.ToString(), log.Summary, log.EntityType, log.EntityId, log.OccurredAtUtc))
            .Take(200)
            .ToListAsync(cancellationToken);
        await LogActivityAsync(context, PortalActivityType.ViewedProfile, "Portal activity history viewed.", null, null, cancellationToken);
        return activity;
    }

    private async Task<DigitalServiceRequestDto> CreateDigitalRequestAsync<TRequest>(DigitalServiceRequestType requestType, TRequest request, CancellationToken cancellationToken)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        return await CreateDigitalRequestAsync(context, requestType, request, cancellationToken);
    }

    private async Task<DigitalServiceRequestDto> CreateDigitalRequestAsync<TRequest>(PortalContext context, DigitalServiceRequestType requestType, TRequest request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var payload = Snapshot(new { context.Profile.InvestorId, Request = request });
        var digitalRequest = DigitalServiceRequest.Create(context.Profile.InvestorId, context.Profile.UserId, requestType, payload, context.Profile.UserId.ToString(), now);
        _dbContext.DigitalServiceRequests.Add(digitalRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var workflow = await _workflowService.CreateAsync(new CreateWorkflowRequest(
            WorkflowType.PortalDigitalServiceRequest.ToString(),
            nameof(DigitalServiceRequest),
            digitalRequest.Id.ToString(),
            $"Investor portal {requestType} for {context.Investor.InvestorNumber}",
            $"Investor portal {requestType} submitted."), cancellationToken);

        digitalRequest.LinkWorkflow(workflow.Id);
        await LogActivityAsync(
            context,
            requestType == DigitalServiceRequestType.DocumentUploadRequest ? PortalActivityType.UploadedDocument : PortalActivityType.SubmittedDigitalRequest,
            $"Portal {requestType} submitted.",
            nameof(DigitalServiceRequest),
            digitalRequest.Id.ToString(),
            cancellationToken,
            saveChanges: false);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapDigitalRequest(digitalRequest);
        await WriteAuditAsync(AuditEventType.Submitted, "PortalDigitalServiceRequestSubmitted", nameof(DigitalServiceRequest), digitalRequest.Id.ToString(), null, Snapshot(dto), $"Portal {requestType} submitted for approval workflow.", cancellationToken, workflow.Id);
        return dto;
    }

    private async Task<PortalContext> GetPortalContextAsync(CancellationToken cancellationToken)
    {
        var userIdText = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("An authenticated portal user is required.");
        if (!Guid.TryParse(userIdText, out var userId))
        {
            throw new UnauthorizedAccessException("The authenticated user id is invalid.");
        }

        var profile = await _dbContext.PortalUserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Portal profile was not found.");
        if (profile.Status != PortalProfileStatus.Active)
        {
            throw new UnauthorizedAccessException("Portal profile is not active.");
        }

        var investor = await _dbContext.Investors
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == profile.InvestorId, cancellationToken)
            ?? throw new NotFoundException("Linked investor record was not found.");
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == profile.UserId, cancellationToken);
        var mfa = await _dbContext.PortalMfaSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == profile.UserId, cancellationToken);
        var mfaRequired = mfa?.MfaRequired ?? false;
        var mfaSatisfied = !mfaRequired || mfa?.MfaVerified == true || user.MfaEnabled;
        if (!mfaSatisfied)
        {
            throw new UnauthorizedAccessException("MFA is required before using investor portal endpoints.");
        }

        var session = await GetOrCreateSessionAsync(profile.UserId, profile.InvestorId, mfaSatisfied, cancellationToken);
        return new PortalContext(profile, investor, session, mfaRequired, mfaSatisfied);
    }

    private async Task<PortalSession> GetOrCreateSessionAsync(Guid userId, Guid investorId, bool mfaSatisfied, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var session = await _dbContext.PortalSessions
            .Where(candidate => candidate.UserId == userId && candidate.Status == PortalSessionStatus.Active)
            .OrderByDescending(candidate => candidate.LastSeenAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            session = PortalSession.Start(userId, investorId, _currentUserContext.IpAddress, _currentUserContext.UserAgent, mfaSatisfied, now);
            _dbContext.PortalSessions.Add(session);
        }
        else
        {
            session.Touch(now);
        }

        return session;
    }

    private async Task LogActivityAsync(PortalContext context, PortalActivityType activityType, string summary, string? entityType, string? entityId, CancellationToken cancellationToken, bool saveChanges = true)
    {
        var now = _dateTimeProvider.UtcNow;
        _dbContext.PortalActivityLogs.Add(PortalActivityLog.Create(context.Profile.UserId, context.Profile.InvestorId, context.Session.Id, activityType, summary, entityType, entityId, _currentUserContext.IpAddress, _currentUserContext.CorrelationId, now));
        if (saveChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static PortalProfileDto MapProfile(PortalContext context)
    {
        return new PortalProfileDto(
            context.Profile.UserId,
            context.Profile.InvestorId,
            context.Investor.InvestorNumber,
            context.Investor.DisplayName,
            context.Investor.Email,
            context.Investor.PhoneNumber,
            context.Investor.Status.ToString(),
            context.MfaRequired,
            context.MfaSatisfied);
    }

    private static PortalHoldingDto MapHolding(PortalHoldingSnapshot holding)
    {
        return new PortalHoldingDto(
            holding.SchemeId,
            holding.SchemeCode,
            holding.SchemeName,
            holding.SchemeClassId,
            holding.SchemeClassCode,
            holding.SchemeClassName,
            holding.Currency,
            holding.Units,
            holding.LienedUnits,
            holding.RedeemableUnits,
            holding.UnitPrice,
            holding.MarketValue,
            holding.RedeemableAmount,
            holding.UnitPrecision,
            holding.LatestValuationDate,
            holding.LastMovementDate,
            holding.LastTransactionReference);
    }

    private static PortalPortfolioPositionDto MapPortfolioPosition(PortalHoldingSnapshot holding)
    {
        return new PortalPortfolioPositionDto(
            holding.SchemeId,
            holding.SchemeCode,
            holding.SchemeName,
            holding.SchemeClassId,
            holding.SchemeClassCode,
            holding.SchemeClassName,
            holding.Currency,
            holding.Units,
            holding.LienedUnits,
            holding.RedeemableUnits,
            holding.UnitPrice,
            holding.MarketValue,
            holding.RedeemableAmount,
            holding.UnitPrecision,
            holding.LatestValuationDate);
    }

    private static PortalTransactionDto MapDealingTransaction(DealingInstruction instruction)
    {
        decimal? amount = instruction.InstructionType switch
        {
            DealingInstructionType.Subscription => instruction.SubscriptionInstructions.Single().Amount,
            DealingInstructionType.Redemption => instruction.RedemptionInstructions.Single().Amount,
            DealingInstructionType.Switch => instruction.SwitchInstructions.Single().Amount,
            _ => null
        };
        decimal? units = instruction.InstructionType switch
        {
            DealingInstructionType.Subscription => instruction.SubscriptionInstructions.Single().Units,
            DealingInstructionType.Redemption => instruction.RedemptionInstructions.Single().Units,
            DealingInstructionType.Switch => instruction.SwitchInstructions.Single().Units,
            _ => null
        };

        return new PortalTransactionDto(
            instruction.Id,
            "Dealing",
            instruction.InstructionType.ToString(),
            instruction.SchemeId,
            instruction.SchemeClassId,
            instruction.BusinessDate.Value,
            amount,
            units,
            instruction.Status.ToString(),
            instruction.InstructionNumber);
    }

    private static DigitalServiceRequestDto MapDigitalRequest(DigitalServiceRequest request)
    {
        return new DigitalServiceRequestDto(
            request.Id,
            request.InvestorId,
            request.UserId,
            request.RequestType.ToString(),
            request.Status.ToString(),
            request.WorkflowId,
            request.RequestPayloadJson,
            request.SubmittedAtUtc);
    }

    private async Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken, Guid? workflowId = null)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason,
            WorkflowId: workflowId), cancellationToken);
    }

    private static string Snapshot<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string Snapshot(PortalSelfRegistration registration)
    {
        return JsonSerializer.Serialize(new
        {
            registration.Id,
            registration.DisplayName,
            registration.Email,
            MaskedPhoneNumber = MaskPhoneNumber(registration.PhoneNumber),
            Status = registration.Status.ToString(),
            registration.OtpSentAtUtc,
            registration.OtpExpiresAtUtc,
            registration.FailedOtpAttemptCount,
            registration.ActivatedAtUtc,
            registration.UserId,
            registration.InvestorId
        }, JsonOptions);
    }

    private async Task<IReadOnlyCollection<PortalHoldingSnapshot>> GetHoldingSnapshotsAsync(Guid investorId, CancellationToken cancellationToken)
    {
        var holdings = await _dbContext.UnitHoldings
            .AsNoTracking()
            .Where(holding => holding.InvestorId == investorId)
            .OrderBy(holding => holding.SchemeId)
            .ThenBy(holding => holding.SchemeClassId)
            .ToListAsync(cancellationToken);

        var schemeClassIds = holdings.Select(holding => holding.SchemeClassId).Distinct().ToArray();
        var schemeIds = holdings.Select(holding => holding.SchemeId).Distinct().ToArray();
        var latestNavByClass = await GetLatestPublishedNavByClassAsync(schemeClassIds, cancellationToken);

        var schemeMap = await _dbContext.Schemes
            .AsNoTracking()
            .Where(scheme => schemeIds.Contains(scheme.Id))
            .Select(scheme => new SchemeSnapshot(scheme.Id, scheme.Code, scheme.Name, scheme.Status))
            .ToDictionaryAsync(scheme => scheme.Id, cancellationToken);

        var schemeClassMap = await _dbContext.SchemeClasses
            .AsNoTracking()
            .Where(schemeClass => schemeClassIds.Contains(schemeClass.Id))
            .Select(schemeClass => new SchemeClassSnapshot(
                schemeClass.Id,
                schemeClass.SchemeId,
                schemeClass.Code,
                schemeClass.Name,
                schemeClass.Currency,
                schemeClass.ValuationFrequency,
                schemeClass.DealingFrequency))
            .ToDictionaryAsync(schemeClass => schemeClass.Id, cancellationToken);

        return holdings.Select(holding =>
        {
            latestNavByClass.TryGetValue(holding.SchemeClassId, out var navSnapshot);
            var scheme = schemeMap[holding.SchemeId];
            var schemeClass = schemeClassMap[holding.SchemeClassId];
            var unitPrice = navSnapshot?.UnitPrice;
            return new PortalHoldingSnapshot(
                holding.SchemeId,
                scheme.Code,
                scheme.Name,
                holding.SchemeClassId,
                schemeClass.Code,
                schemeClass.Name,
                schemeClass.Currency,
                holding.Units,
                holding.LienedUnits,
                holding.RedeemableUnits,
                unitPrice,
                unitPrice is null ? null : decimal.Round(holding.Units * unitPrice.Value, holding.UnitPrecision, MidpointRounding.AwayFromZero),
                unitPrice is null ? null : decimal.Round(holding.RedeemableUnits * unitPrice.Value, holding.UnitPrecision, MidpointRounding.AwayFromZero),
                holding.UnitPrecision,
                navSnapshot?.ValuationDate,
                holding.LastMovementDate?.Value,
                holding.LastTransactionReference);
        }).ToArray();
    }

    private async Task<Dictionary<Guid, PublishedNavSnapshot>> GetLatestPublishedNavByClassAsync(IReadOnlyCollection<Guid> schemeClassIds, CancellationToken cancellationToken)
    {
        if (schemeClassIds.Count == 0)
        {
            return [];
        }

        var publications = await _dbContext.NavPublications
            .AsNoTracking()
            .Where(publication => schemeClassIds.Contains(publication.SchemeClassId))
            .OrderByDescending(publication => publication.PublishedAtUtc)
            .ToListAsync(cancellationToken);

        return publications
            .GroupBy(publication => publication.SchemeClassId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var publication = group.First();
                    return new PublishedNavSnapshot(
                        publication.SchemeClassId,
                        publication.PublishedNav,
                        publication.PublishedUnitPrice,
                        publication.ValuationDate.Value,
                        publication.PublishedAtUtc);
                });
    }

    private async Task<PortalSelfRegistrationActivationDto> MapActivationAsync(PortalSelfRegistration registration, CancellationToken cancellationToken)
    {
        var investor = await _dbContext.Investors
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == registration.InvestorId!.Value, cancellationToken);
        var profile = await _dbContext.PortalUserProfiles
            .AsNoTracking()
            .SingleAsync(candidate => candidate.UserId == registration.UserId!.Value, cancellationToken);
        return new PortalSelfRegistrationActivationDto(
            registration.Id,
            registration.UserId!.Value,
            registration.InvestorId!.Value,
            investor.InvestorNumber,
            investor.Status.ToString(),
            profile.Status.ToString(),
            registration.Email);
    }

    private static PortalSelfRegistrationInitiatedDto MapSelfRegistration(PortalSelfRegistration registration)
    {
        return new PortalSelfRegistrationInitiatedDto(
            registration.Id,
            registration.DisplayName,
            registration.Email,
            MaskPhoneNumber(registration.PhoneNumber),
            registration.Status.ToString(),
            registration.OtpSentAtUtc,
            registration.OtpExpiresAtUtc);
    }

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]> { [field] = [message] });
    }

    private async Task EnsureNoExistingPortalAccountAsync(string normalizedEmail, string phoneNumber, CancellationToken cancellationToken)
    {
        if (await _dbContext.Users.AsNoTracking().AnyAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A portal account with the same email address already exists.");
        }

        var normalizedInputPhone = phoneNumber.Trim();
        if (await _dbContext.Investors.AsNoTracking().AnyAsync(candidate => candidate.Email.ToUpper() == normalizedEmail || candidate.PhoneNumber == normalizedInputPhone, cancellationToken))
        {
            throw new ConflictException("An investor record with the same email address or phone number already exists.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw Validation("email", "Email is required.");
        }

        var normalized = email.Trim();
        if (normalized.Length > 320)
        {
            throw Validation("email", "Email cannot exceed 320 characters.");
        }

        return normalized.ToUpperInvariant();
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw Validation("phoneNumber", "Phone number is required.");
        }

        var trimmed = phoneNumber.Trim();
        if (trimmed.Length > 50)
        {
            throw Validation("phoneNumber", "Phone number cannot exceed 50 characters.");
        }

        var normalized = new string(trimmed
            .Where(character => char.IsDigit(character))
            .ToArray());
        if (normalized.Length < 5)
        {
            throw Validation("phoneNumber", "Phone number is invalid.");
        }

        return normalized;
    }

    private string GenerateOtpCode()
    {
        if (!string.IsNullOrWhiteSpace(_portalRegistrationOptions.FixedOtpCode))
        {
            return _portalRegistrationOptions.FixedOtpCode.Trim();
        }

        var digits = new char[Math.Max(4, _portalRegistrationOptions.OtpLength)];
        for (var index = 0; index < digits.Length; index++)
        {
            digits[index] = (char)('0' + Random.Shared.Next(0, 10));
        }

        return new string(digits);
    }

    private string BuildOtpMessage(string otpCode)
    {
        return $"Victory CIS verification code: {otpCode}. It expires in {_portalRegistrationOptions.OtpExpiryMinutes} minutes.";
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
    }

    private static string GenerateInvestorNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..31].ToUpperInvariant();
    }

    private static IReadOnlyCollection<string> DefaultKycRequirements()
    {
        return ["NationalId", "TaxCertificate", "ProofOfAddress"];
    }

    private static void EnsureInvestorCanRedeem(Investor investor)
    {
        if (investor.Status != InvestorStatus.Approved)
        {
            throw Validation("redemption", "Complete your KYC and obtain investor approval before submitting a redemption request.");
        }
    }

    private static string? ResolveContactValue(IEnumerable<InvestorContact> contacts, string contactType)
    {
        return contacts
            .Where(contact => string.Equals(contact.ContactType, contactType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contact => contact.IsPrimary)
            .Select(contact => contact.Value)
            .FirstOrDefault();
    }

    private Task WriteIdentityAuditAsync(string action, string entityName, string entityId, string summary, CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            IdentityModule,
            action,
            entityName,
            entityId,
            AuditEventType.Created,
            Summary: summary,
            Reason: summary), cancellationToken);
    }

    private sealed record PortalContext(PortalUserProfile Profile, Domain.Investors.Investor Investor, PortalSession Session, bool MfaRequired, bool MfaSatisfied);
    private sealed record SchemeSnapshot(Guid Id, string Code, string Name, SchemeStatus Status);
    private sealed record SchemeClassSnapshot(Guid Id, Guid SchemeId, string Code, string Name, string Currency, SchemeFrequency ValuationFrequency, SchemeFrequency DealingFrequency);
    private sealed record PublishedNavSnapshot(Guid SchemeClassId, decimal PublishedNav, decimal UnitPrice, DateOnly ValuationDate, DateTime PublishedAtUtc);
    private sealed record PortalHoldingSnapshot(
        Guid SchemeId,
        string SchemeCode,
        string SchemeName,
        Guid SchemeClassId,
        string SchemeClassCode,
        string SchemeClassName,
        string Currency,
        decimal Units,
        decimal LienedUnits,
        decimal RedeemableUnits,
        decimal? UnitPrice,
        decimal? MarketValue,
        decimal? RedeemableAmount,
        int UnitPrecision,
        DateOnly? LatestValuationDate,
        DateOnly? LastMovementDate,
        string? LastTransactionReference);
}
