using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts;
using Cis.Contracts.Investors;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Investors;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Investors;

internal sealed class InvestorService : IInvestorService, IKycQueryService, IAmlQueryService
{
    private const string InvestorsModule = "Investors";
    private const string KycModule = "KYC";
    private const string AmlModule = "AML";
    private const string DocumentsModule = "Documents";

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAmlScreeningProvider _amlScreeningProvider;
    private readonly IFileSecurityValidator _fileSecurityValidator;

    public InvestorService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IAmlScreeningProvider amlScreeningProvider,
        IFileSecurityValidator fileSecurityValidator)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _amlScreeningProvider = amlScreeningProvider;
        _fileSecurityValidator = fileSecurityValidator;
    }

    public async Task<InvestorDto> CreateAsync(CreateInvestorRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var investorType = ParseInvestorType(request.InvestorType);
        var investor = Investor.Create(GenerateInvestorNumber(), investorType, request.DisplayName, request.Email, request.PhoneNumber, actor, now);
        ApplyProfile(investor, request);
        investor.AddDefaultKycRequirements(DefaultKycRequirements(investorType));

        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            investor.SetTaxProfile(request.TaxNumber, request.CountryOfTaxResidence ?? "Kenya");
        }

        investor.AddContact("Email", request.Email, true, actor, now);
        investor.AddContact("Phone", request.PhoneNumber, true, actor, now);
        if (!string.IsNullOrWhiteSpace(request.AddressLine1))
        {
            investor.AddContact("Address", request.AddressLine1, true, actor, now);
        }

        if (!string.IsNullOrWhiteSpace(request.MandateType) || !string.IsNullOrWhiteSpace(request.SigningAuthority))
        {
            investor.AddMandate(
                request.MandateType ?? "Standard",
                request.SigningAuthority ?? request.DisplayName,
                BusinessDate.From(request.MandateEffectiveFrom ?? DateOnly.FromDateTime(now)),
                actor,
                now);
        }

        foreach (var beneficialOwner in request.BeneficialOwners ?? [])
        {
            investor.AddBeneficialOwner(
                beneficialOwner.FullName,
                beneficialOwner.IdentityNumber,
                beneficialOwner.OwnershipPercentage,
                beneficialOwner.IsPoliticallyExposed);
        }

        investor.AssignRiskClassification(ParseRiskCategory(request.RiskCategory), request.RiskReason, actor, now);
        await AddDuplicateWarningsAsync(investor, cancellationToken);

        _dbContext.Investors.Add(investor);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInvestorAsync(investor.Id, cancellationToken);
        await WriteAuditAsync(InvestorsModule, AuditEventType.Created, "InvestorCreated", "Investor", dto.Id.ToString(), null, Snapshot(dto), "Investor draft created.", cancellationToken);
        return dto;
    }

    public async Task<PagedResult<InvestorDto>> GetAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var query = ApplyInvestorListQuery(_dbContext.Investors.AsNoTracking(), pagination);
        var totalCount = await query.CountAsync(cancellationToken);
        var ids = await ApplyInvestorListSorting(query, pagination)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(investor => investor.Id)
            .ToListAsync(cancellationToken);

        var investors = new List<InvestorDto>();
        foreach (var id in ids)
        {
            investors.Add(await MapInvestorAsync(id, cancellationToken));
        }

        return new PagedResult<InvestorDto>(investors, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public Task<InvestorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return MapInvestorAsync(id, cancellationToken);
    }

    private static IQueryable<Investor> ApplyInvestorListQuery(IQueryable<Investor> query, PaginationRequest pagination)
    {
        if (string.IsNullOrWhiteSpace(pagination.Search))
        {
            return query;
        }

        var search = pagination.Search.Trim();
        return query.Where(investor =>
            investor.InvestorNumber.Contains(search) ||
            investor.DisplayName.Contains(search) ||
            investor.Email.Contains(search) ||
            investor.PhoneNumber.Contains(search));
    }

    private static IQueryable<Investor> ApplyInvestorListSorting(IQueryable<Investor> query, PaginationRequest pagination)
    {
        var descending = pagination.IsDescending;
        return (pagination.SortBy ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "DISPLAYNAME" => descending ? query.OrderByDescending(investor => investor.DisplayName).ThenBy(investor => investor.InvestorNumber) : query.OrderBy(investor => investor.DisplayName).ThenBy(investor => investor.InvestorNumber),
            "EMAIL" => descending ? query.OrderByDescending(investor => investor.Email).ThenBy(investor => investor.InvestorNumber) : query.OrderBy(investor => investor.Email).ThenBy(investor => investor.InvestorNumber),
            "STATUS" => descending ? query.OrderByDescending(investor => investor.Status).ThenBy(investor => investor.InvestorNumber) : query.OrderBy(investor => investor.Status).ThenBy(investor => investor.InvestorNumber),
            "CREATEDATUTC" => descending ? query.OrderByDescending(investor => investor.Audit.CreatedAtUtc).ThenBy(investor => investor.InvestorNumber) : query.OrderBy(investor => investor.Audit.CreatedAtUtc).ThenBy(investor => investor.InvestorNumber),
            _ => descending ? query.OrderByDescending(investor => investor.InvestorNumber) : query.OrderBy(investor => investor.InvestorNumber)
        };
    }

    public Task<InvestorDto> UpdateAsync(Guid id, UpdateInvestorRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            InvestorsModule,
            "InvestorUpdated",
            AuditEventType.Updated,
            investor =>
            {
                var actor = CurrentUserIdOrThrow();
                var now = _dateTimeProvider.UtcNow;
                investor.UpdateContactSummary(request.DisplayName, request.Email, request.PhoneNumber, actor, now, request.Reason);
                if (!string.IsNullOrWhiteSpace(request.MandateType) || !string.IsNullOrWhiteSpace(request.SigningAuthority))
                {
                    investor.AddMandate(
                        request.MandateType ?? "Standard",
                        request.SigningAuthority ?? request.DisplayName,
                        BusinessDate.From(request.MandateEffectiveFrom ?? DateOnly.FromDateTime(now)),
                        actor,
                        now);
                }
            },
            request.Reason ?? "Investor updated.",
            cancellationToken);
    }

    public Task<InvestorDto> AddDocumentAsync(Guid id, AddKycDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return AddDocumentInternalAsync(id, request, cancellationToken);
    }

    private async Task<InvestorDto> AddDocumentInternalAsync(Guid id, AddKycDocumentRequest request, CancellationToken cancellationToken)
    {
        await _fileSecurityValidator.ValidateAsync(
            new FileUploadDescriptor(
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.StorageReference,
                "InvestorKycDocument"),
            cancellationToken);

        return await ChangeInvestorAsync(
            id,
            DocumentsModule,
            "KycDocumentUploaded",
            AuditEventType.Created,
            investor =>
            {
                investor.AddDocument(
                    request.DocumentType,
                    request.FileName,
                    request.ContentType,
                    request.SizeBytes,
                    request.StorageReference,
                    request.IssueDate.HasValue ? BusinessDate.From(request.IssueDate.Value) : null,
                    request.ExpiryDate.HasValue ? BusinessDate.From(request.ExpiryDate.Value) : null,
                    CurrentUserIdOrThrow(),
                    _dateTimeProvider.UtcNow,
                    Today());
            },
            "KYC document metadata uploaded.",
            cancellationToken);
    }

    public Task<InvestorDto> AddBankAccountAsync(Guid id, AddInvestorBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            InvestorsModule,
            "InvestorBankAccountChanged",
            AuditEventType.Updated,
            investor =>
            {
                investor.AddBankAccount(
                    request.BankName,
                    request.AccountNumber,
                    request.AccountName,
                    request.Currency,
                    request.SwiftCode,
                    request.HighRiskFlag,
                    CurrentUserIdOrThrow(),
                    _dateTimeProvider.UtcNow,
                    request.Reason);
            },
            request.Reason ?? "Investor bank account changed.",
            cancellationToken);
    }

    public Task<InvestorDto> SubmitKycAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            KycModule,
            "KycSubmitted",
            AuditEventType.Submitted,
            investor =>
            {
                AddDuplicateWarningsAsync(investor, cancellationToken).GetAwaiter().GetResult();
                investor.SubmitKyc(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow);
            },
            "Investor KYC submitted for approval.",
            cancellationToken);
    }

    public Task<InvestorDto> ApproveAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            InvestorsModule,
            "InvestorApproved",
            AuditEventType.Approved,
            investor => investor.Approve(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, Today(), request.Comment),
            request.Comment ?? "Investor approved.",
            cancellationToken);
    }

    public Task<InvestorDto> RejectAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            InvestorsModule,
            "InvestorRejected",
            AuditEventType.Rejected,
            investor => investor.Reject(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, request.Comment),
            request.Comment ?? "Investor rejected.",
            cancellationToken);
    }

    public Task<InvestorDto> SuspendAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            InvestorsModule,
            "InvestorSuspended",
            AuditEventType.Updated,
            investor => investor.Suspend(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, request.Comment),
            request.Comment ?? "Investor suspended.",
            cancellationToken);
    }

    public Task<InvestorDto> CloseAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInvestorAsync(
            id,
            InvestorsModule,
            "InvestorClosed",
            AuditEventType.DeletedSoft,
            investor => investor.Close(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, request.Comment),
            request.Comment ?? "Investor closed.",
            cancellationToken);
    }

    public async Task<InvestorDto> StartAmlScreeningAsync(Guid id, StartAmlScreeningRequest request, CancellationToken cancellationToken = default)
    {
        var investor = await GetInvestorAggregateAsync(id, cancellationToken);
        var providerRequest = new AmlScreeningProviderRequest(
            investor.Id,
            investor.InvestorNumber,
            investor.DisplayName,
            request.ProviderName,
            request.ScreeningReference,
            (request.ManualHits ?? [])
                .Select(hit => new AmlScreeningProviderHit(hit.ListName, hit.MatchedName, hit.RiskLevel, hit.Notes))
                .ToArray());

        var providerResult = await _amlScreeningProvider.ScreenAsync(providerRequest, cancellationToken);
        var existingChildIds = InvestorChildIds(investor);
        var beforeJson = Snapshot(investor);
        investor.AddAmlScreeningCase(
            providerResult.ProviderName,
            providerResult.ScreeningReference,
            providerResult.Hits.Select(hit => (hit.ListName, hit.MatchedName, ParseAmlRiskLevel(hit.RiskLevel), hit.Notes)),
            _dateTimeProvider.UtcNow);
        MarkNewInvestorChildrenAsAdded(investor, existingChildIds);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInvestorAsync(id, cancellationToken);
        await WriteAuditAsync(AmlModule, AuditEventType.Created, "AmlScreeningPerformed", "Investor", id.ToString(), beforeJson, Snapshot(dto), "AML screening performed.", cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<KycDocumentDto>> GetExpiredDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var asOf = Today();
        var documents = await _dbContext.KycDocuments
            .AsNoTracking()
            .Include(document => document.Investor)
            .Where(document => document.ExpiryDate != null)
            .OrderBy(document => document.ExpiryDate)
            .ToListAsync(cancellationToken);

        return documents
            .Where(document => document.IsExpiredOn(asOf))
            .Select(MapKycDocument)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<IncompleteKycInvestorDto>> GetIncompleteInvestorsAsync(CancellationToken cancellationToken = default)
    {
        var asOf = Today();
        var investors = await _dbContext.Investors
            .Include(investor => investor.KycRequirements)
            .Include(investor => investor.KycDocuments)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return investors
            .Select(investor => new
            {
                Investor = investor,
                Missing = investor.MissingRequiredDocumentTypes(asOf)
            })
            .Where(item => item.Missing.Count > 0)
            .Select(item => new IncompleteKycInvestorDto(
                item.Investor.Id,
                item.Investor.InvestorNumber,
                item.Investor.DisplayName,
                item.Investor.Status.ToString(),
                item.Missing))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<AmlExceptionDto>> GetExceptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AmlScreeningHits
            .AsNoTracking()
            .Include(hit => hit.AmlScreeningCase)
            .ThenInclude(screeningCase => screeningCase.Investor)
            .Where(hit => hit.RiskLevel == AmlHitRiskLevel.High && !hit.IsResolved)
            .OrderBy(hit => hit.MatchedName)
            .Select(hit => new AmlExceptionDto(
                hit.AmlScreeningCase.InvestorId,
                hit.AmlScreeningCase.Investor.InvestorNumber,
                hit.AmlScreeningCaseId,
                hit.AmlScreeningCase.ProviderName,
                hit.MatchedName,
                hit.RiskLevel.ToString(),
                hit.Notes))
            .ToListAsync(cancellationToken);
    }

    private async Task<InvestorDto> ChangeInvestorAsync(
        Guid id,
        string module,
        string auditAction,
        AuditEventType auditEventType,
        Action<Investor> change,
        string reason,
        CancellationToken cancellationToken)
    {
        var investor = await GetInvestorAggregateAsync(id, cancellationToken);
        var existingChildIds = InvestorChildIds(investor);
        var beforeJson = Snapshot(investor);
        try
        {
            change(investor);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["investor"] = [exception.Message]
            });
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "investor"] = [exception.Message]
            });
        }

        MarkNewInvestorChildrenAsAdded(investor, existingChildIds);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInvestorAsync(id, cancellationToken);
        await WriteAuditAsync(module, auditEventType, auditAction, "Investor", id.ToString(), beforeJson, Snapshot(dto), reason, cancellationToken);
        return dto;
    }

    private async Task AddDuplicateWarningsAsync(Investor investor, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var candidateFields = DuplicateFields(investor).ToArray();
        if (candidateFields.Length == 0)
        {
            return;
        }

        var existingInvestors = await _dbContext.Investors
            .AsNoTracking()
            .Include(candidate => candidate.IndividualProfiles)
            .Include(candidate => candidate.CorporateProfiles)
            .Include(candidate => candidate.JointProfiles)
            .Include(candidate => candidate.GroupProfiles)
            .Include(candidate => candidate.TaxProfiles)
            .Include(candidate => candidate.Contacts)
            .Include(candidate => candidate.BankAccounts)
            .AsSplitQuery()
            .Where(candidate => candidate.Id != investor.Id)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingInvestors)
        {
            foreach (var existingField in DuplicateFields(existing))
            {
                foreach (var candidateField in candidateFields.Where(field =>
                             string.Equals(field.MatchType, existingField.MatchType, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(field.Value, existingField.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    investor.AddDuplicateWarning(candidateField.MatchType, candidateField.Value, existing.Id, existing.InvestorNumber, now);
                }
            }
        }
    }

    private static IEnumerable<(string MatchType, string Value)> DuplicateFields(Investor investor)
    {
        yield return ("Email", investor.Email);
        yield return ("Phone", investor.PhoneNumber);

        foreach (var profile in investor.IndividualProfiles)
        {
            yield return ("IdentityNumber", profile.IdentityNumber);
        }

        foreach (var profile in investor.CorporateProfiles)
        {
            yield return ("CorporateRegistrationNumber", profile.RegistrationNumber);
        }

        foreach (var profile in investor.JointProfiles)
        {
            yield return ("IdentityNumber", profile.PrimaryIdentityNumber);
            yield return ("IdentityNumber", profile.SecondaryIdentityNumber);
        }

        foreach (var profile in investor.GroupProfiles)
        {
            yield return ("CorporateRegistrationNumber", profile.RegistrationNumber);
        }

        foreach (var taxProfile in investor.TaxProfiles)
        {
            yield return ("TaxNumber", taxProfile.TaxNumber);
        }

        foreach (var contact in investor.Contacts.Where(contact => contact.ContactType is "Email" or "Phone"))
        {
            yield return (contact.ContactType, contact.Value);
        }

        foreach (var bankAccount in investor.BankAccounts)
        {
            yield return ("BankAccount", $"{bankAccount.Currency}:{bankAccount.AccountNumber}");
        }
    }

    private async Task<Investor> GetInvestorAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Investors
            .Include(investor => investor.IndividualProfiles)
            .Include(investor => investor.CorporateProfiles)
            .Include(investor => investor.JointProfiles)
            .Include(investor => investor.GroupProfiles)
            .Include(investor => investor.BeneficialOwners)
            .Include(investor => investor.BankAccounts)
            .Include(investor => investor.TaxProfiles)
            .Include(investor => investor.Contacts)
            .Include(investor => investor.Mandates)
            .Include(investor => investor.KycDocuments)
            .Include(investor => investor.KycRequirements)
            .Include(investor => investor.KycReviews)
            .Include(investor => investor.AmlScreeningCases).ThenInclude(screeningCase => screeningCase.Hits)
            .Include(investor => investor.RiskClassifications)
            .Include(investor => investor.DuplicateDetectionResults)
            .Include(investor => investor.ChangeLogs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(investor => investor.Id == id, cancellationToken)
            ?? throw new NotFoundException("Investor was not found.");
    }

    private async Task<InvestorDto> MapInvestorAsync(Guid id, CancellationToken cancellationToken)
    {
        var investor = await _dbContext.Investors
            .AsNoTracking()
            .Include(candidate => candidate.IndividualProfiles)
            .Include(candidate => candidate.CorporateProfiles)
            .Include(candidate => candidate.JointProfiles)
            .Include(candidate => candidate.GroupProfiles)
            .Include(candidate => candidate.BeneficialOwners)
            .Include(candidate => candidate.BankAccounts)
            .Include(candidate => candidate.TaxProfiles)
            .Include(candidate => candidate.Contacts)
            .Include(candidate => candidate.Mandates)
            .Include(candidate => candidate.KycDocuments)
            .Include(candidate => candidate.KycRequirements)
            .Include(candidate => candidate.KycReviews)
            .Include(candidate => candidate.AmlScreeningCases).ThenInclude(screeningCase => screeningCase.Hits)
            .Include(candidate => candidate.RiskClassifications)
            .Include(candidate => candidate.DuplicateDetectionResults)
            .Include(candidate => candidate.ChangeLogs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Investor was not found.");

        return MapInvestor(investor);
    }

    private static InvestorDto MapInvestor(Investor investor)
    {
        return new InvestorDto(
            investor.Id,
            investor.InvestorNumber,
            investor.InvestorType.ToString(),
            investor.DisplayName,
            investor.Email,
            investor.PhoneNumber,
            investor.Status.ToString(),
            investor.RiskCategory?.ToString(),
            investor.SubmittedByUserId,
            investor.ApprovedByUserId,
            investor.IndividualProfiles.Select(profile => new InvestorProfileIndividualDto(profile.Id, profile.FirstName, profile.LastName, profile.IdentityNumber, profile.DateOfBirth, profile.Nationality)).ToArray(),
            investor.CorporateProfiles.Select(profile => new InvestorProfileCorporateDto(profile.Id, profile.RegisteredName, profile.RegistrationNumber, profile.IncorporationDate)).ToArray(),
            investor.JointProfiles.Select(profile => new InvestorProfileJointDto(profile.Id, profile.JointName, profile.PrimaryIdentityNumber, profile.SecondaryIdentityNumber)).ToArray(),
            investor.GroupProfiles.Select(profile => new InvestorProfileGroupDto(profile.Id, profile.GroupName, profile.RegistrationNumber, profile.ContactPersonName)).ToArray(),
            investor.BeneficialOwners.Select(owner => new BeneficialOwnerDto(owner.Id, owner.FullName, owner.IdentityNumber, owner.OwnershipPercentage, owner.IsPoliticallyExposed)).ToArray(),
            investor.BankAccounts.Select(account => new InvestorBankAccountDto(account.Id, account.BankName, account.AccountNumber, account.AccountName, account.Currency, account.SwiftCode, account.IsActive, account.HighRiskFlag)).ToArray(),
            investor.TaxProfiles.Select(tax => new InvestorTaxProfileDto(tax.Id, tax.TaxNumber, tax.CountryOfTaxResidence)).ToArray(),
            investor.Contacts.Select(contact => new InvestorContactDto(contact.Id, contact.ContactType, contact.Value, contact.IsPrimary)).ToArray(),
            investor.Mandates.Select(mandate => new InvestorMandateDto(mandate.Id, mandate.MandateType, mandate.SigningAuthority, mandate.EffectiveFrom.Value, mandate.IsActive)).ToArray(),
            investor.KycDocuments.Select(MapKycDocument).ToArray(),
            investor.KycRequirements.Select(requirement => new KycRequirementDto(requirement.Id, requirement.DocumentType, requirement.IsMandatory, requirement.Status.ToString(), requirement.SatisfiedByDocumentId)).ToArray(),
            investor.KycReviews.Select(review => new KycReviewDto(review.Id, review.Status.ToString(), review.PerformedByUserId, review.PerformedAtUtc, review.Comment)).ToArray(),
            investor.AmlScreeningCases.Select(screeningCase => new AmlScreeningCaseDto(
                screeningCase.Id,
                screeningCase.ProviderName,
                screeningCase.ScreeningReference,
                screeningCase.ScreenedAtUtc,
                screeningCase.Status.ToString(),
                screeningCase.Hits.Select(hit => new AmlScreeningHitDto(hit.Id, hit.ListName, hit.MatchedName, hit.RiskLevel.ToString(), hit.Notes, hit.IsResolved)).ToArray())).ToArray(),
            investor.RiskClassifications.Select(classification => new InvestorRiskClassificationDto(
                classification.Id,
                classification.RiskCategory.ToString(),
                classification.Reason,
                classification.Status.ToString(),
                classification.AssignedByUserId,
                classification.AssignedAtUtc,
                classification.ApprovedByUserId)).ToArray(),
            investor.DuplicateDetectionResults.Select(result => new DuplicateDetectionResultDto(
                result.Id,
                result.MatchType,
                result.MatchedValue,
                result.MatchedInvestorId,
                result.MatchedInvestorNumber,
                result.Status.ToString(),
                result.DetectedAtUtc)).ToArray(),
            investor.ChangeLogs.OrderBy(log => log.ChangedAtUtc).Select(log => new InvestorChangeLogDto(
                log.Id,
                log.ChangeType,
                log.BeforeJson,
                log.AfterJson,
                log.HighRiskFlag,
                log.ChangedByUserId,
                log.ChangedAtUtc,
                log.Reason)).ToArray());
    }

    private static KycDocumentDto MapKycDocument(KycDocument document)
    {
        return new KycDocumentDto(
            document.Id,
            document.InvestorId,
            document.Investor?.InvestorNumber ?? string.Empty,
            document.DocumentType,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.StorageReference,
            document.IssueDate?.Value,
            document.ExpiryDate?.Value,
            document.Status.ToString(),
            document.UploadedAtUtc);
    }

    private static IReadOnlyCollection<string> DefaultKycRequirements(InvestorType investorType)
    {
        return investorType switch
        {
            InvestorType.Individual => ["NationalId", "TaxCertificate", "ProofOfAddress"],
            InvestorType.Corporate => ["CertificateOfIncorporation", "TaxCertificate", "BoardResolution", "BeneficialOwnershipDeclaration"],
            InvestorType.Joint => ["PrimaryNationalId", "SecondaryNationalId", "TaxCertificate", "ProofOfAddress"],
            InvestorType.Group => ["RegistrationCertificate", "TaxCertificate", "Mandate"],
            _ => throw new ArgumentOutOfRangeException(nameof(investorType), investorType, "Unsupported investor type.")
        };
    }

    private static void ApplyProfile(Investor investor, CreateInvestorRequest request)
    {
        try
        {
            switch (investor.InvestorType)
            {
                case InvestorType.Individual:
                    investor.AttachIndividualProfile(
                        request.FirstName ?? string.Empty,
                        request.LastName ?? string.Empty,
                        request.IdentityNumber ?? string.Empty,
                        request.DateOfBirth ?? throw new ArgumentException("dateOfBirth is required for individual investors.", nameof(request.DateOfBirth)),
                        request.Nationality ?? string.Empty);
                    break;
                case InvestorType.Corporate:
                    investor.AttachCorporateProfile(
                        request.RegisteredName ?? request.DisplayName,
                        request.CorporateRegistrationNumber ?? string.Empty,
                        request.IncorporationDate ?? throw new ArgumentException("incorporationDate is required for corporate investors.", nameof(request.IncorporationDate)));
                    break;
                case InvestorType.Joint:
                    investor.AttachJointProfile(
                        request.DisplayName,
                        request.JointPrimaryIdentityNumber ?? string.Empty,
                        request.JointSecondaryIdentityNumber ?? string.Empty);
                    break;
                case InvestorType.Group:
                    investor.AttachGroupProfile(
                        request.GroupName ?? request.DisplayName,
                        request.CorporateRegistrationNumber ?? string.Empty,
                        request.ContactPersonName ?? string.Empty);
                    break;
            }
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "investor"] = [exception.Message]
            });
        }
    }

    private static InvestorType ParseInvestorType(string value)
    {
        if (Enum.TryParse<InvestorType>(value, ignoreCase: true, out var investorType))
        {
            return investorType;
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["investorType"] = ["Unsupported investor type."]
        });
    }

    private static InvestorRiskCategory ParseRiskCategory(string value)
    {
        if (Enum.TryParse<InvestorRiskCategory>(value, ignoreCase: true, out var riskCategory))
        {
            return riskCategory;
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["riskCategory"] = ["Unsupported risk category."]
        });
    }

    private static AmlHitRiskLevel ParseAmlRiskLevel(string value)
    {
        if (Enum.TryParse<AmlHitRiskLevel>(value, ignoreCase: true, out var riskLevel))
        {
            return riskLevel;
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["riskLevel"] = ["Unsupported AML risk level."]
        });
    }

    private static string Snapshot(Investor investor)
    {
        return JsonSerializer.Serialize(new
        {
            investor.Id,
            investor.InvestorNumber,
            Type = investor.InvestorType.ToString(),
            investor.DisplayName,
            investor.Email,
            investor.PhoneNumber,
            Status = investor.Status.ToString(),
            RiskCategory = investor.RiskCategory?.ToString(),
            KycDocumentCount = investor.KycDocuments.Count,
            AmlCaseCount = investor.AmlScreeningCases.Count,
            DuplicateWarningCount = investor.DuplicateDetectionResults.Count
        });
    }

    private static string Snapshot(InvestorDto investor)
    {
        return JsonSerializer.Serialize(new
        {
            investor.Id,
            investor.InvestorNumber,
            investor.InvestorType,
            investor.DisplayName,
            investor.Email,
            investor.PhoneNumber,
            investor.Status,
            investor.RiskCategory,
            KycDocumentCount = investor.KycDocuments.Count,
            AmlCaseCount = investor.AmlScreeningCases.Count,
            DuplicateWarningCount = investor.DuplicateDetectionResults.Count
        });
    }

    private async Task SaveHandlingValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConflictException("The investor change conflicts with an existing record.");
        }
    }

    private Task WriteAuditAsync(
        string module,
        AuditEventType eventType,
        string action,
        string entityName,
        string entityId,
        string? beforeJson,
        string afterJson,
        string reason,
        CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            module,
            action,
            entityName,
            entityId,
            EventType: eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason), cancellationToken);
    }

    private static HashSet<Guid> InvestorChildIds(Investor investor)
    {
        return InvestorChildren(investor).Select(child => child.Id).ToHashSet();
    }

    private void MarkNewInvestorChildrenAsAdded(Investor investor, HashSet<Guid> existingChildIds)
    {
        _dbContext.ChangeTracker.DetectChanges();
        foreach (var child in InvestorChildren(investor).Where(child => !existingChildIds.Contains(child.Id)))
        {
            var entry = _dbContext.Entry(child);
            if (entry.State is EntityState.Detached or EntityState.Modified or EntityState.Unchanged)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    private static IEnumerable<Entity> InvestorChildren(Investor investor)
    {
        return investor.IndividualProfiles.Cast<Entity>()
            .Concat(investor.CorporateProfiles)
            .Concat(investor.JointProfiles)
            .Concat(investor.GroupProfiles)
            .Concat(investor.BeneficialOwners)
            .Concat(investor.BankAccounts)
            .Concat(investor.TaxProfiles)
            .Concat(investor.Contacts)
            .Concat(investor.Mandates)
            .Concat(investor.KycDocuments)
            .Concat(investor.KycRequirements)
            .Concat(investor.KycReviews)
            .Concat(investor.AmlScreeningCases)
            .Concat(investor.AmlScreeningCases.SelectMany(screeningCase => screeningCase.Hits))
            .Concat(investor.RiskClassifications)
            .Concat(investor.DuplicateDetectionResults)
            .Concat(investor.ChangeLogs);
    }

    private BusinessDate Today()
    {
        return BusinessDate.From(DateOnly.FromDateTime(_dateTimeProvider.UtcNow));
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    private static string GenerateInvestorNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..31].ToUpperInvariant();
    }
}
