using Cis.Domain.Investors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class InvestorConfiguration : IEntityTypeConfiguration<Investor>
{
    public void Configure(EntityTypeBuilder<Investor> builder)
    {
        builder.ToTable("investors", "investors");
        builder.HasKey(investor => investor.Id);
        builder.Property(investor => investor.InvestorNumber).HasMaxLength(50).IsRequired();
        builder.Property(investor => investor.InvestorType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(investor => investor.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(investor => investor.Email).HasMaxLength(320).IsRequired();
        builder.Property(investor => investor.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(investor => investor.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(investor => investor.RiskCategory).HasConversion<string>().HasMaxLength(50);
        builder.Property(investor => investor.SubmittedByUserId).HasMaxLength(200);
        builder.Property(investor => investor.ApprovedByUserId).HasMaxLength(200);
        builder.Property(investor => investor.RejectedByUserId).HasMaxLength(200);
        builder.Property(investor => investor.DecisionComment).HasMaxLength(1000);

        builder.OwnsOne(investor => investor.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });

        builder.HasIndex(investor => investor.InvestorNumber).IsUnique();
        builder.HasIndex(investor => investor.Status);
        builder.HasIndex(investor => new { investor.Status, investor.InvestorNumber });
        builder.HasIndex(investor => investor.InvestorType);
        builder.HasIndex(investor => investor.DisplayName);
        builder.HasIndex(investor => investor.Email);
        builder.HasIndex(investor => investor.PhoneNumber);

        builder.Metadata.FindNavigation(nameof(Investor.IndividualProfiles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.CorporateProfiles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.JointProfiles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.GroupProfiles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.BeneficialOwners))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.BankAccounts))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.TaxProfiles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.Contacts))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.Mandates))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.KycDocuments))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.KycRequirements))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.KycReviews))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.AmlScreeningCases))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.RiskClassifications))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.DuplicateDetectionResults))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Investor.ChangeLogs))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InvestorProfileIndividualConfiguration : IEntityTypeConfiguration<InvestorProfileIndividual>
{
    public void Configure(EntityTypeBuilder<InvestorProfileIndividual> builder)
    {
        builder.ToTable("investor_profile_individuals", "investors");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.LastName).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.IdentityNumber).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.Nationality).HasMaxLength(100).IsRequired();
        builder.HasOne(profile => profile.Investor).WithMany(investor => investor.IndividualProfiles).HasForeignKey(profile => profile.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.InvestorId).IsUnique();
        builder.HasIndex(profile => profile.IdentityNumber);
    }
}

internal sealed class InvestorProfileCorporateConfiguration : IEntityTypeConfiguration<InvestorProfileCorporate>
{
    public void Configure(EntityTypeBuilder<InvestorProfileCorporate> builder)
    {
        builder.ToTable("investor_profile_corporates", "investors");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.RegisteredName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.RegistrationNumber).HasMaxLength(100).IsRequired();
        builder.HasOne(profile => profile.Investor).WithMany(investor => investor.CorporateProfiles).HasForeignKey(profile => profile.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.InvestorId).IsUnique();
        builder.HasIndex(profile => profile.RegistrationNumber);
    }
}

internal sealed class InvestorProfileJointConfiguration : IEntityTypeConfiguration<InvestorProfileJoint>
{
    public void Configure(EntityTypeBuilder<InvestorProfileJoint> builder)
    {
        builder.ToTable("investor_profile_joints", "investors");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.JointName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.PrimaryIdentityNumber).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.SecondaryIdentityNumber).HasMaxLength(100).IsRequired();
        builder.HasOne(profile => profile.Investor).WithMany(investor => investor.JointProfiles).HasForeignKey(profile => profile.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.InvestorId).IsUnique();
        builder.HasIndex(profile => profile.PrimaryIdentityNumber);
        builder.HasIndex(profile => profile.SecondaryIdentityNumber);
    }
}

internal sealed class InvestorProfileGroupConfiguration : IEntityTypeConfiguration<InvestorProfileGroup>
{
    public void Configure(EntityTypeBuilder<InvestorProfileGroup> builder)
    {
        builder.ToTable("investor_profile_groups", "investors");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.GroupName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.RegistrationNumber).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.ContactPersonName).HasMaxLength(200).IsRequired();
        builder.HasOne(profile => profile.Investor).WithMany(investor => investor.GroupProfiles).HasForeignKey(profile => profile.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.InvestorId).IsUnique();
        builder.HasIndex(profile => profile.RegistrationNumber);
    }
}

internal sealed class BeneficialOwnerConfiguration : IEntityTypeConfiguration<BeneficialOwner>
{
    public void Configure(EntityTypeBuilder<BeneficialOwner> builder)
    {
        builder.ToTable("beneficial_owners", "investors");
        builder.HasKey(owner => owner.Id);
        builder.Property(owner => owner.FullName).HasMaxLength(200).IsRequired();
        builder.Property(owner => owner.IdentityNumber).HasMaxLength(100).IsRequired();
        builder.HasOne(owner => owner.Investor).WithMany(investor => investor.BeneficialOwners).HasForeignKey(owner => owner.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(owner => new { owner.InvestorId, owner.IdentityNumber });
        builder.HasIndex(owner => owner.IsPoliticallyExposed);
    }
}

internal sealed class InvestorBankAccountConfiguration : IEntityTypeConfiguration<InvestorBankAccount>
{
    public void Configure(EntityTypeBuilder<InvestorBankAccount> builder)
    {
        builder.ToTable("investor_bank_accounts", "investors");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.BankName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.AccountNumber).HasMaxLength(100).IsRequired();
        builder.Property(account => account.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Currency).HasMaxLength(3).IsRequired();
        builder.Property(account => account.SwiftCode).HasMaxLength(20);
        builder.HasOne(account => account.Investor).WithMany(investor => investor.BankAccounts).HasForeignKey(account => account.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(account => new { account.InvestorId, account.AccountNumber, account.Currency }).IsUnique();
        builder.HasIndex(account => account.AccountNumber);
        builder.HasIndex(account => new { account.HighRiskFlag, account.IsActive });
    }
}

internal sealed class InvestorTaxProfileConfiguration : IEntityTypeConfiguration<InvestorTaxProfile>
{
    public void Configure(EntityTypeBuilder<InvestorTaxProfile> builder)
    {
        builder.ToTable("investor_tax_profiles", "investors");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.TaxNumber).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.CountryOfTaxResidence).HasMaxLength(100).IsRequired();
        builder.HasOne(profile => profile.Investor).WithMany(investor => investor.TaxProfiles).HasForeignKey(profile => profile.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.InvestorId).IsUnique();
        builder.HasIndex(profile => profile.TaxNumber);
    }
}

internal sealed class InvestorContactConfiguration : IEntityTypeConfiguration<InvestorContact>
{
    public void Configure(EntityTypeBuilder<InvestorContact> builder)
    {
        builder.ToTable("investor_contacts", "investors");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.ContactType).HasMaxLength(50).IsRequired();
        builder.Property(contact => contact.Value).HasMaxLength(300).IsRequired();
        builder.HasOne(contact => contact.Investor).WithMany(investor => investor.Contacts).HasForeignKey(contact => contact.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(contact => new { contact.ContactType, contact.Value });
        builder.HasIndex(contact => new { contact.InvestorId, contact.IsPrimary });
    }
}

internal sealed class InvestorMandateConfiguration : IEntityTypeConfiguration<InvestorMandate>
{
    public void Configure(EntityTypeBuilder<InvestorMandate> builder)
    {
        builder.ToTable("investor_mandates", "investors");
        builder.HasKey(mandate => mandate.Id);
        builder.Property(mandate => mandate.MandateType).HasMaxLength(100).IsRequired();
        builder.Property(mandate => mandate.SigningAuthority).HasMaxLength(200).IsRequired();
        builder.HasOne(mandate => mandate.Investor).WithMany(investor => investor.Mandates).HasForeignKey(mandate => mandate.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mandate => new { mandate.InvestorId, mandate.IsActive });
    }
}

internal sealed class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.ToTable("kyc_documents", "documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.FileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.StorageReference).HasMaxLength(500).IsRequired();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(document => document.UploadedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne(document => document.Investor).WithMany(investor => investor.KycDocuments).HasForeignKey(document => document.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(document => new { document.InvestorId, document.DocumentType });
        builder.HasIndex(document => document.ExpiryDate);
        builder.HasIndex(document => document.Status);
    }
}

internal sealed class KycRequirementConfiguration : IEntityTypeConfiguration<KycRequirement>
{
    public void Configure(EntityTypeBuilder<KycRequirement> builder)
    {
        builder.ToTable("kyc_requirements", "kyc");
        builder.HasKey(requirement => requirement.Id);
        builder.Property(requirement => requirement.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(requirement => requirement.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(requirement => requirement.Investor).WithMany(investor => investor.KycRequirements).HasForeignKey(requirement => requirement.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(requirement => new { requirement.InvestorId, requirement.DocumentType }).IsUnique();
        builder.HasIndex(requirement => requirement.Status);
    }
}

internal sealed class KycReviewConfiguration : IEntityTypeConfiguration<KycReview>
{
    public void Configure(EntityTypeBuilder<KycReview> builder)
    {
        builder.ToTable("kyc_reviews", "kyc");
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(review => review.PerformedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(review => review.Comment).HasMaxLength(1000);
        builder.HasOne(review => review.Investor).WithMany(investor => investor.KycReviews).HasForeignKey(review => review.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(review => new { review.InvestorId, review.PerformedAtUtc });
    }
}

internal sealed class AmlScreeningCaseConfiguration : IEntityTypeConfiguration<AmlScreeningCase>
{
    public void Configure(EntityTypeBuilder<AmlScreeningCase> builder)
    {
        builder.ToTable("aml_screening_cases", "aml");
        builder.HasKey(screeningCase => screeningCase.Id);
        builder.Property(screeningCase => screeningCase.ProviderName).HasMaxLength(100).IsRequired();
        builder.Property(screeningCase => screeningCase.ScreeningReference).HasMaxLength(100).IsRequired();
        builder.Property(screeningCase => screeningCase.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(screeningCase => screeningCase.Investor).WithMany(investor => investor.AmlScreeningCases).HasForeignKey(screeningCase => screeningCase.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(screeningCase => new { screeningCase.InvestorId, screeningCase.ScreenedAtUtc });
        builder.HasIndex(screeningCase => screeningCase.Status);
        builder.Metadata.FindNavigation(nameof(AmlScreeningCase.Hits))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AmlScreeningHitConfiguration : IEntityTypeConfiguration<AmlScreeningHit>
{
    public void Configure(EntityTypeBuilder<AmlScreeningHit> builder)
    {
        builder.ToTable("aml_screening_hits", "aml");
        builder.HasKey(hit => hit.Id);
        builder.Property(hit => hit.ListName).HasMaxLength(100).IsRequired();
        builder.Property(hit => hit.MatchedName).HasMaxLength(200).IsRequired();
        builder.Property(hit => hit.RiskLevel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(hit => hit.Notes).HasMaxLength(1000);
        builder.HasOne(hit => hit.AmlScreeningCase).WithMany(screeningCase => screeningCase.Hits).HasForeignKey(hit => hit.AmlScreeningCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(hit => new { hit.RiskLevel, hit.IsResolved });
    }
}

internal sealed class InvestorRiskClassificationConfiguration : IEntityTypeConfiguration<InvestorRiskClassification>
{
    public void Configure(EntityTypeBuilder<InvestorRiskClassification> builder)
    {
        builder.ToTable("investor_risk_classifications", "investors");
        builder.HasKey(classification => classification.Id);
        builder.Property(classification => classification.RiskCategory).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(classification => classification.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(classification => classification.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(classification => classification.AssignedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(classification => classification.ApprovedByUserId).HasMaxLength(200);
        builder.HasOne(classification => classification.Investor).WithMany(investor => investor.RiskClassifications).HasForeignKey(classification => classification.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(classification => new { classification.InvestorId, classification.Status });
        builder.HasIndex(classification => classification.RiskCategory);
    }
}

internal sealed class DuplicateDetectionResultConfiguration : IEntityTypeConfiguration<DuplicateDetectionResult>
{
    public void Configure(EntityTypeBuilder<DuplicateDetectionResult> builder)
    {
        builder.ToTable("duplicate_detection_results", "investors");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.MatchType).HasMaxLength(100).IsRequired();
        builder.Property(result => result.MatchedValue).HasMaxLength(300).IsRequired();
        builder.Property(result => result.MatchedInvestorNumber).HasMaxLength(100).IsRequired();
        builder.Property(result => result.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(result => result.Investor).WithMany(investor => investor.DuplicateDetectionResults).HasForeignKey(result => result.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(result => new { result.InvestorId, result.MatchType, result.MatchedValue });
        builder.HasIndex(result => result.Status);
    }
}

internal sealed class InvestorChangeLogConfiguration : IEntityTypeConfiguration<InvestorChangeLog>
{
    public void Configure(EntityTypeBuilder<InvestorChangeLog> builder)
    {
        builder.ToTable("investor_change_logs", "investors");
        builder.HasKey(changeLog => changeLog.Id);
        builder.Property(changeLog => changeLog.ChangeType).HasMaxLength(100).IsRequired();
        builder.Property(changeLog => changeLog.BeforeJson).HasColumnType("jsonb").IsRequired();
        builder.Property(changeLog => changeLog.AfterJson).HasColumnType("jsonb").IsRequired();
        builder.Property(changeLog => changeLog.ChangedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(changeLog => changeLog.Reason).HasMaxLength(1000);
        builder.HasOne(changeLog => changeLog.Investor).WithMany(investor => investor.ChangeLogs).HasForeignKey(changeLog => changeLog.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(changeLog => new { changeLog.InvestorId, changeLog.ChangedAtUtc });
        builder.HasIndex(changeLog => changeLog.HighRiskFlag);
    }
}
