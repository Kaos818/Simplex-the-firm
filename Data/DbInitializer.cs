using SimplexLawFirm.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context, bool isDevelopment = false, string? kaoticPortalPassword = null, string? sharedPassword = null)
        {
            if (!context.LegalAuthorities.Any())
            {
                context.LegalAuthorities.AddRange(
                    new LegalAuthority { Citation="Constitution of the Republic of South Africa, 1996",Subject="Constitutional law",Summary="Supreme binding law governing rights, legality and fair process.",SearchText="rights equality dignity fair hearing administrative justice",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw },
                    new LegalAuthority { Citation="Barkhuizen v Napier 2007 (5) SA 323 (CC)",Subject="Contract law",Summary="Public policy and constitutional fairness in enforcement of contractual time bars.",SearchText="contract public policy fairness time bar",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw },
                    new LegalAuthority { Citation="Sidumo v Rustenburg Platinum Mines Ltd 2008 (2) SA 24 (CC)",Subject="Labour law",Summary="Sets the constitutional standard for review of arbitration awards.",SearchText="labour dismissal arbitration review fairness reasonableness",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw },
                    new LegalAuthority { Citation="S v Makwanyane 1995 (3) SA 391 (CC)",Subject="Constitutional law",Summary="Leading authority on dignity, life and proportional constitutional reasoning.",SearchText="constitutional dignity life rights proportionality",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw },
                    new LegalAuthority { Citation="Legacy procedural guidance (superseded)",Subject="Civil procedure",Summary="Historic procedural guidance retained with a warning that later rules have overtaken it.",SearchText="civil procedure hearing filing deadline court",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.Superseded },
                    new LegalAuthority { Citation="Simplex internal commercial precedent collection",Subject="Commercial law",Summary="Firm precedent fallback only; external-source availability must be verified before reliance.",SearchText="contract shareholder commercial litigation evidence procedure",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,IsInternalFallback=true },
                    new LegalAuthority { Citation="Simplex internal employment precedent collection",Subject="Labour law",Summary="Internal fallback on dismissal and CCMA process; verify against external authority.",SearchText="labour dismissal ccma arbitration fairness",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,IsInternalFallback=true },
                    new LegalAuthority { Citation="Simplex internal civil procedure precedent collection",Subject="Civil procedure",Summary="Internal fallback on hearings, filing and court process; verify against external authority.",SearchText="civil procedure hearing filing deadline court",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,IsInternalFallback=true });
                context.SaveChanges();
            }
            // Ensure database is created
            if (!context.BeneficiaryDocumentRequirements.Any())
            {
                context.BeneficiaryDocumentRequirements.AddRange(
                    new BeneficiaryDocumentRequirement { Code = "SA_ID", DisplayName = "South African identity document", Description = "A clear identity document.", IsRequired = true, RequiresCertifiedCopy = true, DisplayOrder = 1, IsActive = true },
                    new BeneficiaryDocumentRequirement { Code = "PROOF_OF_ADDRESS", DisplayName = "Proof of address", Description = "A recent proof of residential address.", IsRequired = true, RequiresCertifiedCopy = false, MaximumAgeDays = 90, DisplayOrder = 2, IsActive = true },
                    new BeneficiaryDocumentRequirement { Code = "BANK_CONFIRMATION", DisplayName = "Bank confirmation", Description = "A bank-issued account confirmation.", RequiresCertifiedCopy = false, DisplayOrder = 3, IsActive = true },
                    new BeneficiaryDocumentRequirement { Code = "BIRTH_CERTIFICATE", DisplayName = "Birth certificate", Description = "Applicable where requested.", RequiresCertifiedCopy = true, DisplayOrder = 4, IsActive = true },
                    new BeneficiaryDocumentRequirement { Code = "GUARDIANSHIP_DOCUMENT", DisplayName = "Guardianship document", Description = "Applicable guardianship authority.", RequiresCertifiedCopy = true, DisplayOrder = 5, IsActive = true },
                    new BeneficiaryDocumentRequirement { Code = "TRUST_SUPPORTING_DOCUMENT", DisplayName = "Trust supporting document", Description = "Applicable supporting trust record.", RequiresCertifiedCopy = true, DisplayOrder = 6, IsActive = true });
                context.SaveChanges();
            }
            if (!context.CaseDocumentRequirements.Any())
            {
                context.CaseDocumentRequirements.AddRange(
                    new CaseDocumentRequirement { CaseType = "General", Code = "CLIENT_MANDATE", Name = "Client mandate", Description = "Signed authority to act.", Category = DocumentCategory.Contracts, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 1 },
                    new CaseDocumentRequirement { CaseType = "General", Code = "IDENTITY", Name = "Client identity document", Description = "Identity record for the represented client.", Category = DocumentCategory.ClientDocuments, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 2 },
                    new CaseDocumentRequirement { CaseType = "General", Code = "CORE_EVIDENCE", Name = "Core supporting documents", Description = "Documents supporting the pleaded facts.", Category = DocumentCategory.Evidence, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 3 },
                    new CaseDocumentRequirement { CaseType = "General", Code = "CORRESPONDENCE", Name = "Material correspondence", Description = "Relevant correspondence between the parties.", Category = DocumentCategory.Correspondence, Importance = DocumentRequirementImportance.Advisory, DisplayOrder = 4 },
                    new CaseDocumentRequirement { CaseType = "Personal Injury", Code = "MEDICAL_REPORT", Name = "Medical report", Description = "Current medical assessment supporting injury allegations.", Category = DocumentCategory.Evidence, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 10 },
                    new CaseDocumentRequirement { CaseType = "Family Law", Code = "FINANCIAL_DISCLOSURE", Name = "Financial disclosure", Description = "Current financial disclosure where maintenance or patrimonial relief is sought.", Category = DocumentCategory.Evidence, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 10 },
                    new CaseDocumentRequirement { CaseType = "Commercial", Code = "GOVERNING_CONTRACT", Name = "Governing contract", Description = "Executed agreement governing the dispute.", Category = DocumentCategory.Contracts, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 10 });
                context.SaveChanges();
            }

            // ========== SEED USERS ==========
            if (!context.Users.Any())
            {
                var seedPassword = GetSeedPassword();
                var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword);

                context.Users.AddRange(
                    // Director
                    new ApplicationUser
                    {
                        FullName = "Simplex Director",
                        Email = "director@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Director,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-6)
                    },
                    // Lawyer 1 - Naledi Khumalo (Personal Injury)
                    new ApplicationUser
                    {
                        FullName = "Naledi Khumalo",
                        Email = "naledi.khumalo@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Lawyer,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-12)
                    },
                    // Lawyer 2 - Sipho Nkosi (Family Law)
                    new ApplicationUser
                    {
                        FullName = "Sipho Nkosi",
                        Email = "sipho.nkosi@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Lawyer,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-10)
                    },
                    // Lawyer 3 - David Pillay (Business/Commercial)
                    new ApplicationUser
                    {
                        FullName = "David Pillay",
                        Email = "david.pillay@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Lawyer,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-8)
                    },
                    // Paralegal 1 - Nomsa Zulu
                    new ApplicationUser
                    {
                        FullName = "Nomsa Zulu",
                        Email = "nomsa.zulu@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Paralegal,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-5)
                    },
                    // Paralegal 2 - Lerato Mokoena
                    new ApplicationUser
                    {
                        FullName = "Lerato Mokoena",
                        Email = "lerato.mokoena@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Paralegal,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-4)
                    },
                    // Paralegal 3 - Sizwe Dube
                    new ApplicationUser
                    {
                        FullName = "Sizwe Dube",
                        Email = "sizwe.dube@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Paralegal,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-3)
                    },
                    // Accountant
                    new ApplicationUser
                    {
                        FullName = "Mike Accountant",
                        Email = "accountant@simplex.com",
                        PasswordHash = defaultPasswordHash,
                        Role = UserRole.Accountant,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-6)
                    }
                );

                context.SaveChanges();
            }

            // ========== SEED CLIENTS ==========
            if (!context.Clients.Any())
            {
                context.Clients.AddRange(
                    // Client 1: Thabo Mthembu (Personal Injury - Motor Vehicle Accident)
                    new Client
                    {
                        FirstName = "Thabo",
                        LastName = "Mthembu",
                        Email = "thabo.mthembu@example.com",
                        Phone = "0825551234",
                        SAIDNumber = "8001015009088",
                        IsBusiness = false,
                        CreatedAt = DateTime.Now.AddMonths(-2),
                        IsActive = true
                    },
                    // Client 2: Ayanda Dlamini (Divorce and Custody)
                    new Client
                    {
                        FirstName = "Ayanda",
                        LastName = "Dlamini",
                        Email = "ayanda.dlamini@example.com",
                        Phone = "0834445678",
                        SAIDNumber = "8505056009088",
                        IsBusiness = false,
                        CreatedAt = DateTime.Now.AddMonths(-3),
                        IsActive = true
                    },
                    // Client 3: Lerato Naidoo (Business Contract Dispute)
                    new Client
                    {
                        FirstName = "Lerato",
                        LastName = "Naidoo",
                        Email = "lerato.naidoo@example.com",
                        Phone = "0843337890",
                        SAIDNumber = "9009095009088",
                        IsBusiness = true,
                        CompanyName = "Naidoo Enterprises (Pty) Ltd",
                        RegistrationNumber = "2023/123456/07",
                        CreatedAt = DateTime.Now.AddMonths(-4),
                        IsActive = true
                    }
                );

                context.SaveChanges();
            }

            // Create corresponding ApplicationUser entries for clients if they don't exist
            if (!context.Users.Any(u => u.Email == "thabo.mthembu@example.com"))
            {
                var clientPasswordHash = BCrypt.Net.BCrypt.HashPassword(GetSeedPassword());
                context.Users.AddRange(
                    new ApplicationUser
                    {
                        FullName = "Thabo Mthembu",
                        Email = "thabo.mthembu@example.com",
                        PasswordHash = clientPasswordHash,
                        Role = UserRole.Client,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-2)
                    },
                    new ApplicationUser
                    {
                        FullName = "Ayanda Dlamini",
                        Email = "ayanda.dlamini@example.com",
                        PasswordHash = clientPasswordHash,
                        Role = UserRole.Client,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-3)
                    },
                    new ApplicationUser
                    {
                        FullName = "Lerato Naidoo",
                        Email = "lerato.naidoo@example.com",
                        PasswordHash = clientPasswordHash,
                        Role = UserRole.Client,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now.AddMonths(-4)
                    }
                );
                context.SaveChanges();
            }

            // ========== SEED RETAINER TEMPLATES ==========
            if (!context.RetainerTemplates.Any())
            {
                context.RetainerTemplates.AddRange(
                    new RetainerTemplate
                    {
                        Name = "Personal Injury Claim - Road Accident Fund",
                        Description = "Legal representation for motor vehicle accident claims against the Road Accident Fund (RAF). Includes case assessment, document preparation, and negotiation.",
                        Inclusions = "Case assessment, Police report retrieval, Medical report collection, RAF claim submission, Negotiation with RAF, Settlement agreement drafting",
                        Exclusions = "Court appearance fees, Expert witness fees, Appeals process, Medical assessments beyond initial report",
                        BasePrice = 25000,
                        PriceDisplay = "R25,000 - R50,000",
                        Type = RetainerType.CaseBased,
                        IncludedHours = 20,
                        OverageRate = 1500,
                        BillingCycle = "One-time",
                        IsPublic = true,
                        Category = "Personal Injury",
                        DisplayOrder = 1,
                        IsActive = true,
                        EstimatedDuration = "3-6 months",
                        RequiresUpfrontPayment = true,
                        UpfrontPercentage = 30,
                        AllowInstallments = true,
                        MaxInstallments = 3,
                        TermsAndConditions = "Standard RAF claim terms apply. Success fee may apply."
                    },
                    new RetainerTemplate
                    {
                        Name = "Divorce & Custody Mediation",
                        Description = "Legal assistance for divorce proceedings and child custody arrangements through mediation.",
                        Inclusions = "Initial consultation, Mediation sessions (up to 3), Draft custody agreement, Parenting plan development, Court document preparation",
                        Exclusions = "Court representation, Property valuation services, Forensic accounting, International custody matters",
                        BasePrice = 15000,
                        PriceDisplay = "R15,000 - R25,000",
                        Type = RetainerType.Hybrid,
                        IncludedHours = 15,
                        OverageRate = 1200,
                        BillingCycle = "One-time",
                        IsPublic = true,
                        Category = "Family Law",
                        DisplayOrder = 2,
                        IsActive = true,
                        EstimatedDuration = "2-4 months",
                        RequiresUpfrontPayment = true,
                        UpfrontPercentage = 50,
                        AllowInstallments = true,
                        MaxInstallments = 2,
                        TermsAndConditions = "Standard family-law mediation terms apply. Court representation requires a separate agreement."
                    },
                    new RetainerTemplate
                    {
                        Name = "Business Contract Dispute Resolution",
                        Description = "Legal representation for breach of contract and commercial disputes.",
                        Inclusions = "Contract review, Demand letter drafting, Negotiation with opposing party, Settlement negotiation, Alternative dispute resolution",
                        Exclusions = "Full litigation/court representation, Appeals, International arbitration, Forensic investigation",
                        BasePrice = 35000,
                        PriceDisplay = "From R35,000",
                        Type = RetainerType.CaseBased,
                        IncludedHours = 25,
                        OverageRate = 1800,
                        BillingCycle = "One-time",
                        IsPublic = true,
                        Category = "Commercial Litigation",
                        DisplayOrder = 3,
                        IsActive = true,
                        EstimatedDuration = "3-6 months",
                        RequiresUpfrontPayment = true,
                        UpfrontPercentage = 40,
                        AllowInstallments = true,
                        MaxInstallments = 4,
                        TermsAndConditions = "Standard commercial dispute-resolution terms apply. Litigation requires a separate agreement."
                    },
                    new RetainerTemplate
                    {
                        Name = "Corporate Legal Compliance Package",
                        Description = "Comprehensive corporate legal services including compliance, contract review, and legal advice.",
                        Inclusions = "Contract review (up to 5/month), Compliance checks, Legal advice (up to 10 hours), Document drafting, POPIA compliance support",
                        Exclusions = "Litigation services, Debt collection, Major transactions, Intellectual property registration",
                        BasePrice = 45000,
                        PriceDisplay = "R45,000/month",
                        Type = RetainerType.Subscription,
                        IncludedHours = 25,
                        OverageRate = 2000,
                        BillingCycle = "Monthly",
                        IsPublic = true,
                        Category = "Corporate",
                        DisplayOrder = 4,
                        IsActive = true,
                        EstimatedDuration = "Ongoing",
                        RequiresUpfrontPayment = false,
                        AllowInstallments = false,
                        TermsAndConditions = "Standard monthly corporate legal-service terms apply, subject to the stated service limits."
                    }
                );

                context.SaveChanges();
            }

            // ========== GET REFERENCE DATA ==========
            var thaboClient = context.Clients.FirstOrDefault(c => c.Email == "thabo.mthembu@example.com");
            var ayandaClient = context.Clients.FirstOrDefault(c => c.Email == "ayanda.dlamini@example.com");
            var leratoClient = context.Clients.FirstOrDefault(c => c.Email == "lerato.naidoo@example.com");

            var nalediLawyer = context.Users.FirstOrDefault(u => u.Email == "naledi.khumalo@simplex.com");
            var siphoLawyer = context.Users.FirstOrDefault(u => u.Email == "sipho.nkosi@simplex.com");
            var davidLawyer = context.Users.FirstOrDefault(u => u.Email == "david.pillay@simplex.com");

            var nomsaParalegal = context.Users.FirstOrDefault(u => u.Email == "nomsa.zulu@simplex.com");
            var leratoParalegal = context.Users.FirstOrDefault(u => u.Email == "lerato.mokoena@simplex.com");
            var sizweParalegal = context.Users.FirstOrDefault(u => u.Email == "sizwe.dube@simplex.com");

            var piTemplate = context.RetainerTemplates.FirstOrDefault(t => t.Name.Contains("Personal Injury"));
            var divorceTemplate = context.RetainerTemplates.FirstOrDefault(t => t.Name.Contains("Divorce"));
            var contractTemplate = context.RetainerTemplates.FirstOrDefault(t => t.Name.Contains("Contract Dispute"));

            // ========== SEED CASES ==========
            if (!context.Cases.Any())
            {
                // Case 1: Thabo Mthembu - Personal Injury (Motor Vehicle Accident)
                if (thaboClient != null && nalediLawyer != null)
                {
                    context.Cases.Add(new Case
                    {
                        Title = "Mthembu vs Road Accident Fund",
                        Status = CaseStatus.Active,
                        ClientId = thaboClient.Id,
                        LawyerId = nalediLawyer.Id,
                        CreatedAt = new DateTime(2026, 3, 12),
                        CaseNumber = "PI-2026-0001",
                        Description = "Motor vehicle accident claim against Road Accident Fund. Client was involved in a collision in Durban on 10 March 2026. Seeking compensation for medical expenses, loss of income, and general damages."
                    });
                }

                // Case 2: Ayanda Dlamini - Divorce and Custody
                if (ayandaClient != null && siphoLawyer != null)
                {
                    context.Cases.Add(new Case
                    {
                        Title = "Dlamini Divorce Proceedings",
                        Status = CaseStatus.Active,
                        ClientId = ayandaClient.Id,
                        LawyerId = siphoLawyer.Id,
                        CreatedAt = new DateTime(2026, 2, 5),
                        CaseNumber = "FM-2026-0002",
                        Description = "Divorce and child custody proceedings. Client seeking divorce and primary custody of minor children. Mediation phase ongoing."
                    });
                }

                // Case 3: Lerato Naidoo - Business Contract Dispute
                if (leratoClient != null && davidLawyer != null)
                {
                    context.Cases.Add(new Case
                    {
                        Title = "Naidoo vs Apex Supplies (Breach of Contract)",
                        Status = CaseStatus.Active,
                        ClientId = leratoClient.Id,
                        LawyerId = davidLawyer.Id,
                        CreatedAt = new DateTime(2026, 1, 18),
                        CaseNumber = "BC-2026-0003",
                        Description = "Breach of contract dispute with supplier. Client claims supplier failed to deliver goods as per signed agreement. Litigation phase."
                    });
                }

                context.SaveChanges();
            }

            // ========== GET CASES ==========
            var piCase = context.Cases.FirstOrDefault(c => c.CaseNumber == "PI-2026-0001");
            var divorceCase = context.Cases.FirstOrDefault(c => c.CaseNumber == "FM-2026-0002");
            var contractCase = context.Cases.FirstOrDefault(c => c.CaseNumber == "BC-2026-0003");

            // ========== SEED RETAINERS ==========
            if (!context.Retainers.Any())
            {
                // Retainer 1: Thabo Mthembu - Personal Injury
                if (thaboClient != null && piCase != null && piTemplate != null)
                {
                    var retainer1 = new Retainer
                    {
                        ClientId = thaboClient.Id,
                        CaseId = piCase.Id,
                        TemplateId = piTemplate.Id,
                        Title = "Legal Retainer - Mthembu vs Road Accident Fund",
                        ScopeOfWork = "Full legal representation for motor vehicle accident claim against Road Accident Fund. Includes case assessment, document preparation, RAF claim submission, negotiation, and settlement agreement drafting.",
                        SpecialTerms = "Success fee of 15% applies if case is successful. Client to cover expert witness fees if required.",
                        Type = RetainerType.CaseBased,
                        Status = RetainerStatus.Active,
                        Amount = 35000,
                        IncludedHours = 25,
                        OverageRate = 1500,
                        BillingCycle = "One-time",
                        StartDate = new DateTime(2026, 3, 15),
                        CreatedDate = new DateTime(2026, 3, 12),
                        SubmittedForApprovalDate = new DateTime(2026, 3, 12),
                        ApprovedDate = new DateTime(2026, 3, 13),
                        ApprovedByUserId = nalediLawyer?.Id,
                        SignedDate = new DateTime(2026, 3, 14),
                        ClientSignatureName = "Thabo Mthembu",
                        AmountPaid = 1500,
                        SignatureToken = GenerateToken(),
                        SignatureTokenExpiry = DateTime.Now.AddDays(30),
                        IsDeleted = false
                    };
                    context.Retainers.Add(retainer1);
                    context.SaveChanges();

                    // Add payment for retainer 1
                    context.RetainerPayments.Add(new RetainerPayment
                    {
                        RetainerId = retainer1.Id,
                        Amount = 1500,
                        PaymentDate = new DateTime(2026, 3, 15),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5001",
                        Notes = "Initial consultation payment",
                        IsDepositedToTrust = true
                    });
                }

                // Retainer 2: Ayanda Dlamini - Divorce
                if (ayandaClient != null && divorceCase != null && divorceTemplate != null)
                {
                    var retainer2 = new Retainer
                    {
                        ClientId = ayandaClient.Id,
                        CaseId = divorceCase.Id,
                        TemplateId = divorceTemplate.Id,
                        Title = "Legal Retainer - Dlamini Divorce Proceedings",
                        ScopeOfWork = "Legal assistance for divorce proceedings and child custody arrangements through mediation. Includes initial consultation, mediation sessions, draft custody agreement, parenting plan development, and court document preparation.",
                        SpecialTerms = "Mediation fees are separate and will be billed as incurred. Client responsible for court filing fees.",
                        Type = RetainerType.Hybrid,
                        Status = RetainerStatus.Active,
                        Amount = 15000,
                        IncludedHours = 15,
                        OverageRate = 1200,
                        BillingCycle = "One-time",
                        StartDate = new DateTime(2026, 2, 10),
                        CreatedDate = new DateTime(2026, 2, 5),
                        SubmittedForApprovalDate = new DateTime(2026, 2, 5),
                        ApprovedDate = new DateTime(2026, 2, 6),
                        ApprovedByUserId = siphoLawyer?.Id,
                        SignedDate = new DateTime(2026, 2, 7),
                        ClientSignatureName = "Ayanda Dlamini",
                        AmountPaid = 5000,
                        SignatureToken = GenerateToken(),
                        SignatureTokenExpiry = DateTime.Now.AddDays(30),
                        IsDeleted = false
                    };
                    context.Retainers.Add(retainer2);
                    context.SaveChanges();

                    // Add payments for retainer 2
                    context.RetainerPayments.Add(new RetainerPayment
                    {
                        RetainerId = retainer2.Id,
                        Amount = 5000,
                        PaymentDate = new DateTime(2026, 2, 8),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5005",
                        Notes = "Full retainer payment",
                        IsDepositedToTrust = true
                    });
                }

                // Retainer 3: Lerato Naidoo - Business Contract Dispute
                if (leratoClient != null && contractCase != null && contractTemplate != null)
                {
                    var retainer3 = new Retainer
                    {
                        ClientId = leratoClient.Id,
                        CaseId = contractCase.Id,
                        TemplateId = contractTemplate.Id,
                        Title = "Legal Retainer - Naidoo vs Apex Supplies",
                        ScopeOfWork = "Legal representation for breach of contract dispute with supplier. Includes contract review, demand letter drafting, negotiation with opposing party, settlement negotiation, and alternative dispute resolution.",
                        SpecialTerms = "Additional fees apply for full litigation if settlement is not reached. Client to be billed separately for court appearances.",
                        Type = RetainerType.CaseBased,
                        Status = RetainerStatus.Active,
                        Amount = 50000,
                        IncludedHours = 30,
                        OverageRate = 1800,
                        BillingCycle = "One-time",
                        StartDate = new DateTime(2026, 1, 22),
                        CreatedDate = new DateTime(2026, 1, 18),
                        SubmittedForApprovalDate = new DateTime(2026, 1, 18),
                        ApprovedDate = new DateTime(2026, 1, 19),
                        ApprovedByUserId = davidLawyer?.Id,
                        SignedDate = new DateTime(2026, 1, 20),
                        ClientSignatureName = "Lerato Naidoo",
                        AmountPaid = 12000,
                        SignatureToken = GenerateToken(),
                        SignatureTokenExpiry = DateTime.Now.AddDays(30),
                        IsDeleted = false
                    };
                    context.Retainers.Add(retainer3);
                    context.SaveChanges();

                    // Add payments for retainer 3
                    context.RetainerPayments.Add(new RetainerPayment
                    {
                        RetainerId = retainer3.Id,
                        Amount = 10000,
                        PaymentDate = new DateTime(2026, 1, 20),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5020",
                        Notes = "Upfront retainer payment",
                        IsDepositedToTrust = true
                    });
                }

                context.SaveChanges();
            }

            // ========== SEED INVOICES ==========
            if (!context.Invoices.Any())
            {
                var thaboRetainer = context.Retainers.FirstOrDefault(r => r.Client.Email == "thabo.mthembu@example.com");
                var ayandaRetainer = context.Retainers.FirstOrDefault(r => r.Client.Email == "ayanda.dlamini@example.com");
                var leratoRetainer = context.Retainers.FirstOrDefault(r => r.Client.Email == "lerato.naidoo@example.com");

                // Invoice 1: Thabo Mthembu - Initial Consultation
                if (thaboClient != null && thaboRetainer != null)
                {
                    context.Invoices.Add(new Invoice
                    {
                        ClientId = thaboClient.Id,
                        RetainerId = thaboRetainer.Id,
                        CaseId = piCase?.Id,
                        InvoiceNumber = "INV-1001",
                        Amount = 2500,
                        TaxAmount = 0,
                        TotalAmount = 2500,
                        IssueDate = new DateTime(2026, 3, 13),
                        DueDate = new DateTime(2026, 4, 12),
                        Status = InvoiceStatus.PartiallyPaid,
                        CreatedAt = new DateTime(2026, 3, 13),
                        Description = "Initial consultation fee - Motor vehicle accident claim assessment"
                    });
                }

                // Invoice 2: Thabo Mthembu - Document Preparation
                if (thaboClient != null && thaboRetainer != null)
                {
                    context.Invoices.Add(new Invoice
                    {
                        ClientId = thaboClient.Id,
                        RetainerId = thaboRetainer.Id,
                        CaseId = piCase?.Id,
                        InvoiceNumber = "INV-1005",
                        Amount = 3000,
                        TaxAmount = 0,
                        TotalAmount = 3000,
                        IssueDate = new DateTime(2026, 3, 20),
                        DueDate = new DateTime(2026, 4, 19),
                        Status = InvoiceStatus.Sent,
                        CreatedAt = new DateTime(2026, 3, 20),
                        Description = "Document preparation and RAF claim submission"
                    });
                }

                // Invoice 3: Ayanda Dlamini - Retainer Invoice
                if (ayandaClient != null && ayandaRetainer != null)
                {
                    context.Invoices.Add(new Invoice
                    {
                        ClientId = ayandaClient.Id,
                        RetainerId = ayandaRetainer.Id,
                        CaseId = divorceCase?.Id,
                        InvoiceNumber = "INV-1010",
                        Amount = 5000,
                        TaxAmount = 0,
                        TotalAmount = 5000,
                        IssueDate = new DateTime(2026, 2, 6),
                        DueDate = new DateTime(2026, 3, 8),
                        Status = InvoiceStatus.Paid,
                        CreatedAt = new DateTime(2026, 2, 6),
                        Description = "Divorce retainer invoice"
                    });
                }

                // Invoice 4: Ayanda Dlamini - Legal Hours
                if (ayandaClient != null && ayandaRetainer != null)
                {
                    context.Invoices.Add(new Invoice
                    {
                        ClientId = ayandaClient.Id,
                        RetainerId = ayandaRetainer.Id,
                        CaseId = divorceCase?.Id,
                        InvoiceNumber = "INV-1015",
                        Amount = 2200,
                        TaxAmount = 0,
                        TotalAmount = 2200,
                        IssueDate = new DateTime(2026, 2, 25),
                        DueDate = new DateTime(2026, 3, 27),
                        Status = InvoiceStatus.PartiallyPaid,
                        CreatedAt = new DateTime(2026, 2, 25),
                        Description = "Legal hours - Mediation and document preparation"
                    });
                }

                // Invoice 5: Lerato Naidoo - Upfront Retainer
                if (leratoClient != null && leratoRetainer != null)
                {
                    context.Invoices.Add(new Invoice
                    {
                        ClientId = leratoClient.Id,
                        RetainerId = leratoRetainer.Id,
                        CaseId = contractCase?.Id,
                        InvoiceNumber = "INV-1020",
                        Amount = 10000,
                        TaxAmount = 0,
                        TotalAmount = 10000,
                        IssueDate = new DateTime(2026, 1, 19),
                        DueDate = new DateTime(2026, 2, 18),
                        Status = InvoiceStatus.Paid,
                        CreatedAt = new DateTime(2026, 1, 19),
                        Description = "Upfront retainer for contract dispute"
                    });
                }

                // Invoice 6: Lerato Naidoo - Additional Expenses
                if (leratoClient != null && leratoRetainer != null)
                {
                    context.Invoices.Add(new Invoice
                    {
                        ClientId = leratoClient.Id,
                        RetainerId = leratoRetainer.Id,
                        CaseId = contractCase?.Id,
                        InvoiceNumber = "INV-1025",
                        Amount = 4500,
                        TaxAmount = 0,
                        TotalAmount = 4500,
                        IssueDate = new DateTime(2026, 2, 20),
                        DueDate = new DateTime(2026, 3, 22),
                        Status = InvoiceStatus.PartiallyPaid,
                        CreatedAt = new DateTime(2026, 2, 20),
                        Description = "Additional expenses - Filing fees and consultation charges"
                    });
                }

                context.SaveChanges();
            }

            // ========== SEED PAYMENTS ==========
            if (!context.Payments.Any())
            {
                var thaboInvoice1 = context.Invoices.FirstOrDefault(i => i.InvoiceNumber == "INV-1001");
                var ayandaInvoice2 = context.Invoices.FirstOrDefault(i => i.InvoiceNumber == "INV-1015");
                var leratoInvoice2 = context.Invoices.FirstOrDefault(i => i.InvoiceNumber == "INV-1025");

                // Payment 1: Thabo Mthembu - Partial payment for consultation
                if (thaboInvoice1 != null && thaboClient != null)
                {
                    context.Payments.Add(new Payment
                    {
                        InvoiceId = thaboInvoice1.Id,
                        ClientId = thaboClient.Id,
                        Amount = 1500,
                        PaymentDate = new DateTime(2026, 3, 15),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5001",
                        Notes = "Partial payment for initial consultation",
                        IsTrustAccountDeposit = true,
                        CreatedAt = new DateTime(2026, 3, 15)
                    });
                }

                // Payment 2: Ayanda Dlamini - Full retainer payment
                var ayandaInvoice1 = context.Invoices.FirstOrDefault(i => i.InvoiceNumber == "INV-1010");
                if (ayandaInvoice1 != null && ayandaClient != null)
                {
                    context.Payments.Add(new Payment
                    {
                        InvoiceId = ayandaInvoice1.Id,
                        ClientId = ayandaClient.Id,
                        Amount = 5000,
                        PaymentDate = new DateTime(2026, 2, 8),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5005",
                        Notes = "Full retainer payment",
                        IsTrustAccountDeposit = true,
                        CreatedAt = new DateTime(2026, 2, 8)
                    });
                }

                // Payment 3: Ayanda Dlamini - Partial payment for legal hours
                if (ayandaInvoice2 != null && ayandaClient != null)
                {
                    context.Payments.Add(new Payment
                    {
                        InvoiceId = ayandaInvoice2.Id,
                        ClientId = ayandaClient.Id,
                        Amount = 1200,
                        PaymentDate = new DateTime(2026, 2, 25),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5010",
                        Notes = "Partial payment for legal hours",
                        IsTrustAccountDeposit = true,
                        CreatedAt = new DateTime(2026, 2, 25)
                    });
                }

                // Payment 4: Lerato Naidoo - Full retainer payment
                var leratoInvoice1 = context.Invoices.FirstOrDefault(i => i.InvoiceNumber == "INV-1020");
                if (leratoInvoice1 != null && leratoClient != null)
                {
                    context.Payments.Add(new Payment
                    {
                        InvoiceId = leratoInvoice1.Id,
                        ClientId = leratoClient.Id,
                        Amount = 10000,
                        PaymentDate = new DateTime(2026, 1, 20),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5020",
                        Notes = "Upfront retainer payment",
                        IsTrustAccountDeposit = true,
                        CreatedAt = new DateTime(2026, 1, 20)
                    });
                }

                // Payment 5: Lerato Naidoo - Partial payment for additional expenses
                if (leratoInvoice2 != null && leratoClient != null)
                {
                    context.Payments.Add(new Payment
                    {
                        InvoiceId = leratoInvoice2.Id,
                        ClientId = leratoClient.Id,
                        Amount = 2000,
                        PaymentDate = new DateTime(2026, 2, 25),
                        PaymentMethod = PaymentMethod.EFT,
                        TransactionReference = "PAY-5030",
                        Notes = "Partial payment for additional expenses",
                        IsTrustAccountDeposit = true,
                        CreatedAt = new DateTime(2026, 2, 25)
                    });
                }

                context.SaveChanges();
            }

            // ========== SEED CALENDAR EVENTS ==========
            if (!context.CalendarEvents.Any())
            {
                // Court filing deadline for Thabo Mthembu case
                if (piCase != null && nalediLawyer != null)
                {
                    var filingEvent = new CalendarEvent
                    {
                        Title = "Court Filing Deadline - Mthembu vs RAF",
                        Description = "Submit all court documents for RAF claim",
                        StartDateTime = new DateTime(2026, 4, 30, 16, 0, 0),
                        EndDateTime = new DateTime(2026, 4, 30, 17, 0, 0),
                        Location = "Durban High Court",
                        Type = EventType.Deadline,
                        Status = EventStatus.Scheduled,
                        CreatedByUserId = nalediLawyer.Id,
                        CreatedAt = DateTime.Now,
                        IsAllDay = true,
                        CaseId = piCase.Id
                    };
                    context.CalendarEvents.Add(filingEvent);
                }

                // Mediation session for Ayanda Dlamini case
                if (divorceCase != null && siphoLawyer != null)
                {
                    var mediationEvent = new CalendarEvent
                    {
                        Title = "Divorce Mediation Session - Dlamini",
                        Description = "Mediation session for divorce and custody arrangements",
                        StartDateTime = new DateTime(2026, 2, 20, 10, 0, 0),
                        EndDateTime = new DateTime(2026, 2, 20, 13, 0, 0),
                        Location = "Family Court - Mediation Centre",
                        Type = EventType.Meeting,
                        Status = EventStatus.Scheduled,
                        CreatedByUserId = siphoLawyer.Id,
                        CreatedAt = DateTime.Now,
                        IsAllDay = false,
                        CaseId = divorceCase.Id
                    };
                    context.CalendarEvents.Add(mediationEvent);
                }

                // Court hearing for Lerato Naidoo case
                if (contractCase != null && davidLawyer != null)
                {
                    var courtEvent = new CalendarEvent
                    {
                        Title = "Court Hearing - Naidoo vs Apex Supplies",
                        Description = "Preliminary court hearing for breach of contract dispute",
                        StartDateTime = new DateTime(2026, 3, 10, 9, 0, 0),
                        EndDateTime = new DateTime(2026, 3, 10, 12, 0, 0),
                        Location = "Johannesburg High Court - Courtroom 4",
                        Type = EventType.CourtAppearance,
                        Status = EventStatus.Scheduled,
                        CreatedByUserId = davidLawyer.Id,
                        CreatedAt = DateTime.Now,
                        IsAllDay = false,
                        CaseId = contractCase.Id
                    };
                    context.CalendarEvents.Add(courtEvent);
                }

                context.SaveChanges();
            }

            // ========== SEED EVENT ATTENDEES ==========
            if (!context.EventAttendees.Any())
            {
                var filingEvent = context.CalendarEvents.FirstOrDefault(e => e.Title.Contains("Filing Deadline"));
                var mediationEvent = context.CalendarEvents.FirstOrDefault(e => e.Title.Contains("Mediation"));
                var courtEvent = context.CalendarEvents.FirstOrDefault(e => e.Title.Contains("Court Hearing"));

                // Filing deadline attendees
                if (filingEvent != null && nalediLawyer != null && nomsaParalegal != null)
                {
                    context.EventAttendees.AddRange(
                        new EventAttendee { EventId = filingEvent.Id, UserId = nalediLawyer.Id, ResponseStatus = "Accepted" },
                        new EventAttendee { EventId = filingEvent.Id, UserId = nomsaParalegal.Id, ResponseStatus = "Accepted" }
                    );
                }

                // Mediation session attendees
                if (mediationEvent != null && siphoLawyer != null && leratoParalegal != null && ayandaClient != null)
                {
                    var ayandaUser = context.Users.FirstOrDefault(u => u.Email == "ayanda.dlamini@example.com");
                    context.EventAttendees.AddRange(
                        new EventAttendee { EventId = mediationEvent.Id, UserId = siphoLawyer.Id, ResponseStatus = "Accepted" },
                        new EventAttendee { EventId = mediationEvent.Id, UserId = leratoParalegal.Id, ResponseStatus = "Accepted" }
                    );
                    if (ayandaUser != null)
                    {
                        context.EventAttendees.Add(new EventAttendee { EventId = mediationEvent.Id, UserId = ayandaUser.Id, ResponseStatus = "Pending" });
                    }
                }

                // Court hearing attendees
                if (courtEvent != null && davidLawyer != null && sizweParalegal != null)
                {
                    context.EventAttendees.AddRange(
                        new EventAttendee { EventId = courtEvent.Id, UserId = davidLawyer.Id, ResponseStatus = "Accepted" },
                        new EventAttendee { EventId = courtEvent.Id, UserId = sizweParalegal.Id, ResponseStatus = "Accepted" }
                    );
                }

                context.SaveChanges();
            }

            // ========== SEED EVENT REMINDERS ==========
            if (!context.EventReminders.Any())
            {
                var filingEvent = context.CalendarEvents.FirstOrDefault(e => e.Title.Contains("Filing Deadline"));
                var mediationEvent = context.CalendarEvents.FirstOrDefault(e => e.Title.Contains("Mediation"));
                var courtEvent = context.CalendarEvents.FirstOrDefault(e => e.Title.Contains("Court Hearing"));

                if (filingEvent != null)
                {
                    context.EventReminders.AddRange(
                        new EventReminder { EventId = filingEvent.Id, ReminderMinutesBefore = 10080, IsSent = false }, // 7 days
                        new EventReminder { EventId = filingEvent.Id, ReminderMinutesBefore = 1440, IsSent = false }     // 24 hours
                    );
                }

                if (mediationEvent != null)
                {
                    context.EventReminders.AddRange(
                        new EventReminder { EventId = mediationEvent.Id, ReminderMinutesBefore = 1440, IsSent = false },  // 24 hours
                        new EventReminder { EventId = mediationEvent.Id, ReminderMinutesBefore = 60, IsSent = false }    // 1 hour
                    );
                }

                if (courtEvent != null)
                {
                    context.EventReminders.AddRange(
                        new EventReminder { EventId = courtEvent.Id, ReminderMinutesBefore = 10080, IsSent = false }, // 7 days
                        new EventReminder { EventId = courtEvent.Id, ReminderMinutesBefore = 1440, IsSent = false },  // 24 hours
                        new EventReminder { EventId = courtEvent.Id, ReminderMinutesBefore = 60, IsSent = false }     // 1 hour
                    );
                }

                context.SaveChanges();
            }

            // ========== SEED TRUST ACCOUNTS ==========
            if (!context.TrustAccounts.Any())
            {
                if (thaboClient != null)
                {
                    context.TrustAccounts.Add(new TrustAccount
                    {
                        ClientId = thaboClient.Id,
                        Balance = 1500,
                        TotalDeposited = 1500,
                        TotalWithdrawn = 0,
                        LastUpdated = DateTime.Now
                    });
                }

                if (ayandaClient != null)
                {
                    context.TrustAccounts.Add(new TrustAccount
                    {
                        ClientId = ayandaClient.Id,
                        Balance = 6200,
                        TotalDeposited = 6200,
                        TotalWithdrawn = 0,
                        LastUpdated = DateTime.Now
                    });
                }

                if (leratoClient != null)
                {
                    context.TrustAccounts.Add(new TrustAccount
                    {
                        ClientId = leratoClient.Id,
                        Balance = 12000,
                        TotalDeposited = 12000,
                        TotalWithdrawn = 0,
                        LastUpdated = DateTime.Now
                    });
                }

                context.SaveChanges();
            }

            // ========== SEED TRUST TRANSACTIONS ==========
            if (!context.TrustTransactions.Any())
            {
                var thaboTrust = context.TrustAccounts.FirstOrDefault(t => t.Client.Email == "thabo.mthembu@example.com");
                var ayandaTrust = context.TrustAccounts.FirstOrDefault(t => t.Client.Email == "ayanda.dlamini@example.com");
                var leratoTrust = context.TrustAccounts.FirstOrDefault(t => t.Client.Email == "lerato.naidoo@example.com");

                if (thaboTrust != null)
                {
                    context.TrustTransactions.Add(new TrustTransaction
                    {
                        TrustAccountId = thaboTrust.Id,
                        Type = TransactionType.Deposit,
                        Amount = 1500,
                        Description = "Initial consultation payment - Mthembu vs RAF",
                        Reference = "PAY-5001",
                        TransactionDate = new DateTime(2026, 3, 15)
                    });
                }

                if (ayandaTrust != null)
                {
                    context.TrustTransactions.AddRange(
                        new TrustTransaction
                        {
                            TrustAccountId = ayandaTrust.Id,
                            Type = TransactionType.Deposit,
                            Amount = 5000,
                            Description = "Retainer payment - Dlamini Divorce",
                            Reference = "PAY-5005",
                            TransactionDate = new DateTime(2026, 2, 8)
                        },
                        new TrustTransaction
                        {
                            TrustAccountId = ayandaTrust.Id,
                            Type = TransactionType.Deposit,
                            Amount = 1200,
                            Description = "Legal hours payment - Dlamini Divorce",
                            Reference = "PAY-5010",
                            TransactionDate = new DateTime(2026, 2, 25)
                        }
                    );
                }

                if (leratoTrust != null)
                {
                    context.TrustTransactions.AddRange(
                        new TrustTransaction
                        {
                            TrustAccountId = leratoTrust.Id,
                            Type = TransactionType.Deposit,
                            Amount = 10000,
                            Description = "Upfront retainer - Naidoo vs Apex Supplies",
                            Reference = "PAY-5020",
                            TransactionDate = new DateTime(2026, 1, 20)
                        },
                        new TrustTransaction
                        {
                            TrustAccountId = leratoTrust.Id,
                            Type = TransactionType.Deposit,
                            Amount = 2000,
                            Description = "Additional expenses payment - Naidoo vs Apex Supplies",
                            Reference = "PAY-5030",
                            TransactionDate = new DateTime(2026, 2, 25)
                        }
                    );
                }

                context.SaveChanges();
            }

            if (isDevelopment && !string.IsNullOrWhiteSpace(kaoticPortalPassword))
                SeedKaoticBeneficiaryVerificationScenario(context, kaoticPortalPassword);

            EnsureBeneficiaryDocumentAssignments(context);
            EnsureDirectorIdentity(context);
            EnsureCostEstimatorSeedData(context);
            EnsureShowcaseUseCaseData(context);
            ApplyRequestedDevelopmentPasswords(context, sharedPassword);
        }

        private static void EnsureShowcaseUseCaseData(ApplicationDbContext context)
        {
            var director = context.Users.FirstOrDefault(x => x.Email == "director@simplex.com");
            var naledi = context.Users.FirstOrDefault(x => x.Email == "naledi.khumalo@simplex.com");
            var sipho = context.Users.FirstOrDefault(x => x.Email == "sipho.nkosi@simplex.com");
            var david = context.Users.FirstOrDefault(x => x.Email == "david.pillay@simplex.com");
            var thabo = context.Clients.FirstOrDefault(x => x.Email == "thabo.mthembu@example.com");
            var ayanda = context.Clients.FirstOrDefault(x => x.Email == "ayanda.dlamini@example.com");
            var lerato = context.Clients.FirstOrDefault(x => x.Email == "lerato.naidoo@example.com");
            var pi = context.Cases.FirstOrDefault(x => x.CaseNumber == "PI-2026-0001");
            var family = context.Cases.FirstOrDefault(x => x.CaseNumber == "FM-2026-0002");
            var commercial = context.Cases.FirstOrDefault(x => x.CaseNumber == "BC-2026-0003");
            if (director is null || naledi is null || sipho is null || david is null || thabo is null || ayanda is null || lerato is null || pi is null || family is null || commercial is null) return;

            pi.CaseType = "Personal Injury"; pi.EvidenceStrength = .78m; pi.MatterValue = 1_250_000m;
            family.CaseType = "Family Law"; family.EvidenceStrength = .62m; family.MatterValue = 420_000m;
            commercial.CaseType = "Commercial"; commercial.EvidenceStrength = .71m; commercial.MatterValue = 2_800_000m;

            var scenarios = new[]
            {
                (Type: "Personal Injury", Prefix: "PI", Client: thabo, Lawyer: naledi),
                (Type: "Family Law", Prefix: "FM", Client: ayanda, Lawyer: sipho),
                (Type: "Commercial", Prefix: "CM", Client: lerato, Lawyer: david)
            };
            foreach (var scenario in scenarios)
            {
                for (var i = 1; i <= 3; i++)
                {
                    var number = $"DEMO-{scenario.Prefix}-HIST-{i:00}";
                    if (context.Cases.Any(x => x.CaseNumber == number)) continue;
                    var outcome = i == 2 ? ForecastResult.PartlySuccessful : ForecastResult.Successful;
                    context.Cases.Add(new Case
                    {
                        CaseNumber = number, Title = $"Closed {scenario.Type} comparable {i}",
                        Description = "Showcase precedent matter with a recorded outcome, cost history and measurable duration.",
                        CaseType = scenario.Type, ClientId = scenario.Client.Id, LawyerId = scenario.Lawyer.Id,
                        Status = CaseStatus.Closed, RecordedOutcome = outcome,
                        OutcomeSummary = outcome == ForecastResult.Successful ? "Relief obtained substantially as instructed." : "Negotiated resolution achieved with shared concessions.",
                        EvidenceStrength = .55m + (i * .1m), MatterValue = 300_000m * i,
                        SettlementAmount = 180_000m * i, CreatedAt = DateTime.UtcNow.AddMonths(-(15 - i)), UpdatedAt = DateTime.UtcNow.AddMonths(-(8 - i))
                    });
                }
            }
            context.SaveChanges();

            var closedPi = context.Cases.Where(x => x.CaseNumber.StartsWith("DEMO-PI-HIST-")).OrderBy(x => x.CaseNumber).ToList();
            foreach (var (matter, index) in closedPi.Select((x, i) => (x, i)))
                if (!context.CaseForecasts.Any(x => x.CaseId == matter.Id))
                    context.CaseForecasts.Add(new CaseForecast
                    {
                        CaseId = matter.Id, AttorneyId = naledi.Id, Status = ForecastStatus.Scored,
                        Probability = .82m + index * .04m, ProbabilityBand = "Strong prospects", ConfidenceLevel = "Developing", ComparableCount = 3,
                        FactorsJson = "[{\"name\":\"Evidence profile\",\"weight\":0.35}]", ComparableCasesJson = "[]",
                        AttorneyAssessment = .90m + index * .02m, AttorneyAgrees = true, AttorneyNotes = "Professional assessment committed before outcome.",
                        RequestedAtUtc = DateTime.UtcNow.AddMonths(-(12 - index)), LockedAtUtc = DateTime.UtcNow.AddMonths(-(12 - index)).AddDays(1),
                        ActualOutcome = index == 1 ? ForecastResult.PartlySuccessful : ForecastResult.Successful,
                        AccuracyScore = index == 1 ? .36m : .86m - index * .04m, ScoredAtUtc = DateTime.UtcNow.AddMonths(-(7 - index))
                    });
            if (!context.ForecastCalibrations.Any(x => x.AttorneyId == naledi.Id))
                context.ForecastCalibrations.Add(new ForecastCalibration { AttorneyId = naledi.Id, ForecastCount = 3, MeanAccuracy = .66m, MeanBias = .19m, OptimisticForecastCount = 1, UpdatedAtUtc = DateTime.UtcNow.AddDays(-2) });
            if (!context.ForecastCalibrations.Any(x => x.AttorneyId == null))
                context.ForecastCalibrations.Add(new ForecastCalibration { ForecastCount = 3, MeanAccuracy = .66m, MeanBias = .19m, OptimisticForecastCount = 1, UpdatedAtUtc = DateTime.UtcNow.AddDays(-2) });

            if (!context.ClientForecastRequests.Any(x => x.CaseId == commercial.Id))
                context.ClientForecastRequests.Add(new ClientForecastRequest { CaseId = commercial.Id, ClientId = lerato.Id, Status = ForecastRequestStatus.Pending, ClientMessage = "Please explain the prospects before we commit to trial.", RequestedAtUtc = DateTime.UtcNow.AddDays(-1) });

            if (!context.ServiceComplaints.Any(x => x.ReferenceNumber == "SLC-DEMO-2026-001"))
                context.ServiceComplaints.Add(new ServiceComplaint
                {
                    ReferenceNumber = "SLC-DEMO-2026-001", CaseId = family.Id, ClientId = ayanda.Id,
                    Category = ComplaintCategory.Communication, Description = "I need a clear update on mediation and the parenting-plan timetable.",
                    Status = ComplaintStatus.Submitted, RoutedToUserId = director.Id, RestrictedUserIds = sipho.Id.ToString(),
                    SubmittedAtUtc = DateTime.UtcNow.AddDays(-2), ResponseDueAtUtc = DateTime.UtcNow.AddDays(8)
                });

            if (!context.ClientCorrespondence.Any(x => x.CaseId == family.Id && x.Subject == "Urgent parenting-plan update requested"))
                context.ClientCorrespondence.Add(new ClientCorrespondence { CaseId = family.Id, Subject = "Urgent parenting-plan update requested", ReceivedAtUtc = DateTime.UtcNow.AddDays(-3) });

            if (!context.CaseDocumentRequests.Any(x => x.CaseId == pi.Id && x.Title == "Updated medico-legal evidence pack"))
            {
                var request = new CaseDocumentRequest { CaseId = pi.Id, ClientId = thabo.Id, RequestedByAttorneyId = naledi.Id, Title = "Updated medico-legal evidence pack", RequiredDocuments = "Latest specialist report\nProof of transport expenses\nUpdated employment-loss schedule", Instructions = "Upload clear PDF copies. Reports must show the practitioner's name and date.", DueAtUtc = DateTime.UtcNow.AddDays(7), Status = CaseDocumentRequestStatus.Requested };
                context.CaseDocumentRequests.Add(request); context.SaveChanges();
                var clientUser = context.Users.FirstOrDefault(x => x.Email == thabo.Email);
                if (clientUser is not null && !context.SystemNotifications.Any(x => x.DeduplicationKey == $"seed-case-doc-request-{request.Id}")) context.SystemNotifications.Add(new SystemNotification { UserId = clientUser.Id, Type = "CaseDocuments", Title = $"Documents requested for {pi.CaseNumber}", Message = request.RequiredDocuments, ActionUrl = $"/CaseDocumentRequest/Respond/{request.Id}", DeduplicationKey = $"seed-case-doc-request-{request.Id}" });
            }

            if (!context.CaseHandoverRequests.Any(x => x.CaseId == pi.Id))
            {
                var request = new CaseHandoverRequest { CaseId = pi.Id, RequestedByAttorneyId = naledi.Id, Reason = "Court commitments overlap with the next medico-legal deadline; a Director should assess capacity and continuity risk.", Status = HandoverRequestStatus.PendingDirector, RequestedAtUtc = DateTime.UtcNow.AddHours(-6) };
                context.CaseHandoverRequests.Add(request); context.SaveChanges();
                if (!context.SystemNotifications.Any(x => x.DeduplicationKey == $"seed-handover-request-{request.Id}")) context.SystemNotifications.Add(new SystemNotification { UserId = director.Id, Type = "HandoverRequest", Title = $"Handover requested for {pi.CaseNumber}", Message = request.Reason, ActionUrl = $"/Practice/ReviewHandoverRequest/{request.Id}", DeduplicationKey = $"seed-handover-request-{request.Id}" });
            }

            if (!context.CaseHandovers.Any(x => x.CaseId == family.Id))
            {
                var reassignment = new CaseReassignment { CaseId = family.Id, OutgoingAttorneyId = sipho.Id, ReceivingAttorneyId = david.Id, ApprovedByUserId = director.Id, Reason = "Capacity balancing ahead of the mediation deadline.", Status = ReassignmentStatus.HandoverPreparing, ApprovedAtUtc = DateTime.UtcNow.AddDays(-1) };
                context.CaseReassignments.Add(reassignment); context.SaveChanges();
                var handover = new CaseHandover { CaseId = family.Id, OutgoingAttorneyId = sipho.Id, ReceivingAttorneyId = david.Id, Status = HandoverStatus.Preparing, DueAtUtc = DateTime.UtcNow.AddDays(1), CreatedAtUtc = DateTime.UtcNow.AddDays(-1), CaseReassignmentId = reassignment.Id, UrgentMatters = "Mediation brief and parenting-plan correspondence require immediate attention." };
                handover.Items.Add(new HandoverItem { Type = "Client correspondence", Description = "1 client message remains unanswered", IsMandatory = true, IsResolved = false, SourceFingerprint = "demo-unanswered-1" });
                handover.Items.Add(new HandoverItem { Type = "Appointments", Description = "Upcoming mediation must be briefed to the receiving attorney", IsMandatory = false, IsResolved = false, SourceFingerprint = "demo-mediation-1" });
                context.CaseHandovers.Add(handover);
            }

            // Repair older/partially seeded deployments as well as fresh databases.
            // The live refresh will update counts and fingerprints when the pack is
            // opened; all five required checklist categories must exist beforehand.
            context.SaveChanges();
            var seededHandover = context.CaseHandovers.Include(x => x.Items)
                .FirstOrDefault(x => x.CaseId == family.Id && x.Status != HandoverStatus.Accepted);
            var checklist = new[]
            {
                ("Deadlines", "Open deadlines require a recorded position", true),
                ("Unbilled time", "Unbilled time must be billed or explained", true),
                ("Signatures", "Documents awaiting signature must be addressed", true),
                ("Appointments", "Scheduled appointments must be briefed", false),
                ("Client correspondence", "Unanswered client correspondence must be addressed", true)
            };
            if (seededHandover is not null)
            {
                if (seededHandover.Status is HandoverStatus.Preparing or HandoverStatus.Overdue or HandoverStatus.Ready)
                    family.LawyerId = seededHandover.OutgoingAttorneyId;
                foreach (var (type, description, mandatory) in checklist)
                    if (!seededHandover.Items.Any(x => x.Type == type))
                        seededHandover.Items.Add(new HandoverItem { Type = type, Description = description, IsMandatory = mandatory, SourceFingerprint = "seed-pending-refresh" });
            }

            if (!context.VulnerableClientFlags.Any(x => x.ClientId == thabo.Id && x.Safeguard == ClientSafeguard.Interpreter))
                context.VulnerableClientFlags.Add(new VulnerableClientFlag { ClientId = thabo.Id, Safeguard = ClientSafeguard.Interpreter, LanguageRequired = "isiZulu", Reason = "Client requested that complex medico-legal advice be interpreted before consent.", Status = VulnerableFlagStatus.PendingReview, RaisedByAttorneyId = naledi.Id, RaisedAtUtc = DateTime.UtcNow.AddDays(-1), ReviewDueAtUtc = DateTime.UtcNow.AddDays(1), NextReviewAtUtc = DateTime.UtcNow.AddMonths(3), LastChangedAtUtc = DateTime.UtcNow.AddDays(-1) });

            if (!context.LitigationStrategyDecisions.Any(x => x.CaseId == commercial.Id && x.Status == StrategyDecisionStatus.PendingDirectorAuthorisation))
                context.LitigationStrategyDecisions.Add(new LitigationStrategyDecision { CaseId = commercial.Id, AttorneyId = david.Id, Strategy = LitigationStrategyType.Trial, Reasoning = "Documentary evidence is strong, but trial cost exceeds the governance threshold and requires an explicit commercial decision.", LowProspectsJustification = "Not applicable; review is triggered by projected cost relative to value.", CostsIncurredSnapshot = 185_000m, ProjectedCostToCompletion = 1_050_000m, MatterValueSnapshot = commercial.MatterValue, ProspectsSnapshot = .71m, ComparableSettlementLow = 900_000m, ComparableSettlementHigh = 1_650_000m, ComparableMatterCount = 3, ExpectedDurationDays = 300, CostAuthorisationRequired = true, Status = StrategyDecisionStatus.PendingDirectorAuthorisation, RecordedAtUtc = DateTime.UtcNow.AddDays(-2), ReviewDueAtUtc = DateTime.UtcNow.AddDays(1) });

            var legacyAuthority = context.LegalAuthorities.FirstOrDefault(x => x.Citation == "Legacy procedural guidance (superseded)");
            if (legacyAuthority is not null && !context.CaseAuthorityReliances.Any(x => x.CaseId == commercial.Id && x.LegalAuthorityId == legacyAuthority.Id))
                context.CaseAuthorityReliances.Add(new CaseAuthorityReliance { CaseId = commercial.Id, LegalAuthorityId = legacyAuthority.Id, AttorneyId = david.Id, RelevanceReason = "Demonstrates the adverse-treatment warning; current procedural rules must be preferred.", AdverseTreatmentConfirmed = true, RecordedAtUtc = DateTime.UtcNow.AddDays(-4) });

            if (!context.AttorneyWhereabouts.Any(x => x.AttorneyId == naledi.Id && x.CheckedOutAtUtc == null))
                context.AttorneyWhereabouts.Add(new AttorneyWhereabout { AttorneyId = naledi.Id, Venue = "Durban High Court – RAF case management", CheckedInAtUtc = DateTime.UtcNow.AddHours(-3), ExpectedReturnAtUtc = DateTime.UtcNow.AddHours(-1), Status = WhereaboutStatus.DirectorEscalated, AlertedAtUtc = DateTime.UtcNow.AddMinutes(-50), DirectorEscalatedAtUtc = DateTime.UtcNow.AddMinutes(-20), ContactOutcome = "Two contact attempts recorded; awaiting confirmation." });

            foreach (var policy in new[] { (ReimbursementExpenseType.Travel, 1500m, ExpenseClassification.ClientRecoverable), (ReimbursementExpenseType.CourtFiling, 5000m, ExpenseClassification.ClientRecoverable), (ReimbursementExpenseType.Meals, 350m, ExpenseClassification.FirmOverhead) })
                if (!context.ExpensePolicies.Any(x => x.ExpenseType == policy.Item1)) context.ExpensePolicies.Add(new ExpensePolicy { ExpenseType = policy.Item1, PerItemLimit = policy.Item2, DelegatedApprovalLimit = 1000m, DefaultClassification = policy.Item3, IsActive = true });
            var acceptedAppointment = context.CalendarEvents.FirstOrDefault(x => x.Title == "Accepted medico-legal consultation – reimbursement demo");
            if (acceptedAppointment is null)
            {
                acceptedAppointment = new CalendarEvent { Title = "Accepted medico-legal consultation – reimbursement demo", Description = "Client-approved concluded consultation used to demonstrate appointment billing and attorney reimbursement.", Location = "Simplex consultation room", StartDateTime = DateTime.UtcNow.AddDays(-2).AddHours(-1), EndDateTime = DateTime.UtcNow.AddDays(-2), Type = EventType.Consultation, Status = EventStatus.Completed, CreatedByUserId = naledi.Id, AssignedToUserId = naledi.Id, CaseId = pi.Id, ClientId = pi.ClientId, RetainerId = context.Retainers.Where(x => x.CaseId == pi.Id && x.Status == RetainerStatus.Active).Select(x => (int?)x.Id).FirstOrDefault(), AppointmentFee = 950m, PaymentDueDays = 7, LatePenaltyType = LatePenaltyType.Percentage, LatePenaltyValue = 5m, LatePenaltyGraceDays = 3, ClientResponseStatus = AppointmentResponseStatus.Accepted, LawyerApprovalStatus = AppointmentApprovalStatus.Approved, ClientRespondedAtUtc = DateTime.UtcNow.AddDays(-3), ActualStartTime = DateTime.UtcNow.AddDays(-2).AddHours(-1), ActualEndTime = DateTime.UtcNow.AddDays(-2), CreatedAt = DateTime.UtcNow.AddDays(-4), Color = "#c2410c", CompletionNotes = "Consultation completed with client approval.", MeetingLink = "", RecurrenceRule = "" };
                context.CalendarEvents.Add(acceptedAppointment); context.SaveChanges();
            }
            if (!context.ReimbursementClaims.Any(x => x.ClaimNumber == "RMB-DEMO-001"))
            {
                var claim = new ReimbursementClaim { ClaimNumber = "RMB-DEMO-001", CaseId = pi.Id, Case = pi, AttorneyId = naledi.Id, Attorney = naledi, ExpenseType = ReimbursementExpenseType.Travel, ExpenseDate = DateTime.UtcNow.Date.AddDays(-2), Amount = 1280m, Description = "Travel to the client-approved medico-legal consultation.", Status = ReimbursementStatus.PendingDirector, MatchedActivityType = ReimbursementActivityType.Appointment, MatchedActivityId = acceptedAppointment.Id, PolicyLimitSnapshot = 1500m, DelegatedLimitSnapshot = 1000m, ExceedsPolicyLimit = false, Classification = ExpenseClassification.ClientRecoverable, ClassificationReason = "Matter-related travel is recoverable under the retainer.", ProofOriginalFileName = "demo-travel-receipt.pdf", ProofRelativePath = "showcase/demo-travel-receipt.pdf", ProofContentType = "application/pdf", SubmittedAtUtc = DateTime.UtcNow.AddDays(-1) };
                var bytes = new RecordPdfService().CreateReimbursement(claim); var relative = claim.ProofRelativePath.Replace('/', Path.DirectorySeparatorChar); var path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "SecureReimbursementProofs", relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); claim.ProofSizeBytes = bytes.Length; claim.ProofSha256Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)); context.ReimbursementClaims.Add(claim);
            }
            else
            {
                var demoClaim = context.ReimbursementClaims.Single(x => x.ClaimNumber == "RMB-DEMO-001");
                if (demoClaim.Status == ReimbursementStatus.PendingDirector)
                {
                    demoClaim.MatchedActivityType = ReimbursementActivityType.Appointment;
                    demoClaim.MatchedActivityId = acceptedAppointment.Id;
                    demoClaim.Description = "Travel to the client-approved medico-legal consultation.";
                }
            }

            EnsureShowcasePrecedentData(context, director.Id);
            context.SaveChanges();
        }

        private static void EnsureShowcasePrecedentData(ApplicationDbContext context, int directorId)
        {
            foreach (var seed in new[] { ("Civil Procedure", "filing hearing deadline court rule"), ("Commercial Law", "contract breach damages supplier"), ("Family Law", "custody parenting mediation maintenance"), ("Data Protection", "POPIA privacy information regulator") })
                if (!context.LegalSubjects.Any(x => x.Name == seed.Item1)) context.LegalSubjects.Add(new LegalSubject { Name = seed.Item1, Keywords = seed.Item2, IsActive = true });
            context.SaveChanges();
            var commercial = context.LegalSubjects.Single(x => x.Name == "Commercial Law");
            var librarySeeds = new[]
            {
                ("Civil Procedure", "Pleadings amendment checklist", "A practical checklist covering notice, prejudice, timing and the proposed amended pages."),
                ("Civil Procedure", "Urgent application evidence guide", "Evidence should establish urgency, absence of adequate alternative relief and a precise timetable."),
                ("Civil Procedure", "Discovery readiness memorandum", "A matter-level guide for relevance review, privilege logging, document preservation and indexed production."),
                ("Commercial Law", "Contract damages assessment note", "Forecast damages by separating direct loss, mitigation, causation and reasonably foreseeable consequential loss."),
                ("Commercial Law", "Shareholder dispute early assessment", "Review governance documents, voting history, fiduciary allegations and the availability of statutory remedies."),
                ("Commercial Law", "Supplier breach settlement framework", "A settlement framework comparing cure, replacement performance, price reduction, termination and quantified damages."),
                ("Family Law", "Parenting plan evidence checklist", "Collect caregiving history, schooling, healthcare, stability and child-focused proposals before mediation."),
                ("Family Law", "Maintenance affordability schedule", "Compare verified income, reasonable needs, dependants, assets and recurring obligations using source documents."),
                ("Family Law", "Mediation preparation guide", "Identify agreed facts, disputed issues, settlement ranges and safeguards for a durable family-law resolution."),
                ("Data Protection", "POPIA breach response playbook", "Preserve evidence, contain exposure, assess notification duties and document remedial controls and communications."),
                ("Data Protection", "Data-subject request checklist", "Verify identity, locate relevant records, assess lawful exemptions and record the response within required timelines."),
                ("Data Protection", "Operator agreement review guide", "Review processing instructions, confidentiality, security controls, incident reporting, deletion and audit provisions.")
            };
            for (var index = 0; index < librarySeeds.Length; index++)
            {
                var seed = librarySeeds[index];
                var hash = $"DEMO-LIBRARY-{index + 1:00}";
                if (context.PrecedentItems.Any(x => x.ContentHash == hash)) continue;
                var subject = context.LegalSubjects.Single(x => x.Name == seed.Item1);
                context.PrecedentItems.Add(new PrecedentItem
                {
                    SourceType = PrecedentSourceType.KnowledgeArticle,
                    SourceId = 9100 + index,
                    ContentHash = hash,
                    Title = seed.Item2,
                    SourceText = seed.Item3,
                    LegalSubjectId = subject.Id,
                    IsCurrent = true,
                    SourceDateUtc = DateTime.UtcNow.AddDays(-45 - index),
                    IndexedAtUtc = DateTime.UtcNow.AddDays(-44 - index)
                });
            }
            if (!context.PrecedentItems.Any(x => x.ContentHash == "DEMO-COMMERCIAL-OLD"))
                context.PrecedentItems.Add(new PrecedentItem { SourceType = PrecedentSourceType.KnowledgeArticle, SourceId = 9001, ContentHash = "DEMO-COMMERCIAL-OLD", Title = "Legacy supplier cancellation clause guidance", SourceText = "Earlier internal guidance treated cancellation clauses as automatically enforceable.", LegalSubjectId = commercial.Id, IsCurrent = true, SourceDateUtc = DateTime.UtcNow.AddYears(-3), IndexedAtUtc = DateTime.UtcNow.AddYears(-3) });
            if (!context.PrecedentItems.Any(x => x.ContentHash == "DEMO-COMMERCIAL-NEW"))
                context.PrecedentItems.Add(new PrecedentItem { SourceType = PrecedentSourceType.CaseOutcome, SourceId = 9002, ContentHash = "DEMO-COMMERCIAL-NEW", Title = "Current fairness review of cancellation clauses", SourceText = "Recent outcome requires proportionality and public-policy scrutiny before enforcement.", LegalSubjectId = commercial.Id, IsCurrent = true, SourceDateUtc = DateTime.UtcNow.AddMonths(-2), IndexedAtUtc = DateTime.UtcNow.AddMonths(-2) });
            context.SaveChanges();
            var oldItem = context.PrecedentItems.Single(x => x.ContentHash == "DEMO-COMMERCIAL-OLD");
            var newItem = context.PrecedentItems.Single(x => x.ContentHash == "DEMO-COMMERCIAL-NEW");
            if (!context.PrecedentConflictFlags.Any(x => x.NewPrecedentItemId == newItem.Id && x.ExistingPrecedentItemId == oldItem.Id))
                context.PrecedentConflictFlags.Add(new PrecedentConflictFlag { NewPrecedentItemId = newItem.Id, ExistingPrecedentItemId = oldItem.Id, Similarity = .88m, Reason = "Newer outcome appears to supersede the older automatic-enforcement guidance.", Status = PrecedentFlagStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddDays(-2) });
            var dataProtection = context.LegalSubjects.Single(x => x.Name == "Data Protection");
            if (!context.CoverageCommissions.Any(x => x.LegalSubjectId == dataProtection.Id))
                context.CoverageCommissions.Add(new CoverageCommission { LegalSubjectId = dataProtection.Id, Status = CoverageCommissionStatus.Commissioned, Brief = "Prepare a practical POPIA breach-response precedent pack; current library coverage is empty.", CommissionedByUserId = directorId, CommissionedAtUtc = DateTime.UtcNow.AddDays(-3), DueAtUtc = DateTime.UtcNow.AddDays(11) });
        }

        private static void EnsureCostEstimatorSeedData(ApplicationDbContext context)
        {
            var lawyers = new[]
            {
                (Email: "naledi.khumalo@simplex.com", Rate: 2200m, Bar: "LPC-NK-1001", Office: "Durban"),
                (Email: "sipho.nkosi@simplex.com", Rate: 2400m, Bar: "LPC-SN-1002", Office: "Johannesburg"),
                (Email: "david.pillay@simplex.com", Rate: 2850m, Bar: "LPC-DP-1003", Office: "Cape Town")
            };
            foreach (var seed in lawyers)
            {
                var user = context.Users.SingleOrDefault(x => x.Email == seed.Email);
                if (user is null) continue;
                var profile = context.LawyerProfiles.SingleOrDefault(x => x.UserId == user.Id);
                if (profile is null)
                {
                    context.LawyerProfiles.Add(new LawyerProfile { UserId = user.Id, HourlyRate = seed.Rate, BarNumber = seed.Bar, Bio = "Development seed profile for cost estimation.", OfficeLocation = seed.Office, YearsOfExperience = 8, IsActive = true });
                }
                else
                {
                    profile.HourlyRate = profile.HourlyRate > 0 ? profile.HourlyRate : seed.Rate;
                    profile.IsActive = true;
                }
            }
            context.SaveChanges();

            var client = context.Clients.OrderBy(x => x.Id).FirstOrDefault();
            var lawyerIds = context.LawyerProfiles.Where(x => x.IsActive && x.HourlyRate > 0).OrderBy(x => x.Id).Select(x => x.UserId).ToList();
            if (client is null || lawyerIds.Count == 0) return;

            var activeTypes = new Dictionary<string, string>
            {
                ["PI-2026-0001"] = "Personal Injury",
                ["FM-2026-0002"] = "Family Law",
                ["BC-2026-0003"] = "Commercial"
            };
            foreach (var pair in activeTypes)
            {
                var activeMatter = context.Cases.SingleOrDefault(x => x.CaseNumber == pair.Key);
                if (activeMatter is not null) activeMatter.CaseType = pair.Value;
            }

            var matterTypes = new[] { "Personal Injury", "Family Law", "Commercial", "General" };
            for (var typeIndex = 0; typeIndex < matterTypes.Length; typeIndex++)
            {
                var matterType = matterTypes[typeIndex];
                var prefix = matterType.Replace(" ", "").ToUpperInvariant()[..Math.Min(4, matterType.Replace(" ", "").Length)];
                for (var index = 1; index <= 6; index++)
                {
                    var caseNumber = $"EST-HIST-{prefix}-{index:00}";
                    var matter = context.Cases.SingleOrDefault(x => x.CaseNumber == caseNumber);
                    if (matter is null)
                    {
                        matter = new Case { CaseNumber = caseNumber, Title = $"Closed {matterType} estimate precedent {index}", Description = "Closed firm matter providing anonymised estimator cost and revenue history.", CaseType = matterType, ClientId = client.Id, LawyerId = lawyerIds[(typeIndex + index - 1) % lawyerIds.Count], Status = CaseStatus.Closed, CreatedAt = new DateTime(2025, Math.Min(index + 1, 12), 10), UpdatedAt = new DateTime(2025, Math.Min(index + 3, 12), 20), MatterValue = 250000m * index };
                        context.Cases.Add(matter);
                        context.SaveChanges();
                    }
                    if (!context.TimeEntries.Any(x => x.CaseId == matter.Id && x.Description == "Estimator historical legal work"))
                    {
                        var rate = context.LawyerProfiles.Where(x => x.UserId == matter.LawyerId).Select(x => x.HourlyRate).Single();
                        var hours = 8m + typeIndex * 3m + index * 2m;
                        context.TimeEntries.Add(new TimeEntry { CaseId = matter.Id, LawyerId = matter.LawyerId!.Value, Description = "Estimator historical legal work", Date = new DateTime(2025, Math.Min(index + 2, 12), 15), Hours = hours, HourlyRate = rate, TotalAmount = hours * rate, IsBillable = true, IsBilled = true, CreatedAt = new DateTime(2025, Math.Min(index + 2, 12), 15) });
                    }
                    if (!context.Invoices.Any(x => x.InvoiceNumber == $"EST-REV-{prefix}-{index:00}"))
                    {
                        var professionalRevenue = (24_000m + typeIndex * 18_000m) * index;
                        context.Invoices.Add(new Invoice { InvoiceNumber = $"EST-REV-{prefix}-{index:00}", ClientId = client.Id, CaseId = matter.Id, Amount = professionalRevenue, TaxAmount = professionalRevenue * .15m, TotalAmount = professionalRevenue * 1.15m, Description = $"Historical {matterType} professional fees and disbursements", Notes = "Anonymised firm revenue history for cost estimation.", IssueDate = new DateTime(2025, Math.Min(index + 3, 12), 20), DueDate = new DateTime(2025, Math.Min(index + 3, 12), 20).AddDays(30), CreatedDate = new DateTime(2025, Math.Min(index + 3, 12), 20), CreatedAt = new DateTime(2025, Math.Min(index + 3, 12), 20), Status = InvoiceStatus.Paid, IsPaid = true, PaidDate = new DateTime(2025, Math.Min(index + 3, 12), 20).AddDays(14) });
                    }
                }
            }
            context.SaveChanges();
        }

        private static void EnsureDirectorIdentity(ApplicationDbContext context)
        {
            var director = context.Users.FirstOrDefault(x => x.Email == "director@simplex.com")
                ?? context.Users.FirstOrDefault(x => x.Email == "admin@simplex.com")
                ?? context.Users.AsEnumerable().FirstOrDefault(x => x.Role is UserRole.Director or UserRole.Admin);
            if (director is null)
            {
                director = new ApplicationUser
                {
                    FullName = "Simplex Director", Email = "director@simplex.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(GetSeedPassword()), Role = UserRole.Director,
                    IsActive = true, EmailConfirmed = true, CreatedAt = DateTime.UtcNow, AssignedCases = []
                };
                context.Users.Add(director);
            }
            director.FullName = "Simplex Director";
            director.Email = "director@simplex.com";
            director.Role = UserRole.Director;
            director.IsActive = true;
            director.EmailConfirmed = true;
            context.SaveChanges();
        }

        private static void EnsureBeneficiaryDocumentAssignments(ApplicationDbContext context)
        {
            var requirements = context.BeneficiaryDocumentRequirements.Where(x => x.IsActive).ToList();
            var beneficiaryIds = context.Beneficiaries.Select(x => x.Id).ToList();
            var existing = context.BeneficiaryRequirementAssignments.Select(x => new { x.BeneficiaryId, x.RequirementId }).ToList()
                .Select(x => (x.BeneficiaryId, x.RequirementId)).ToHashSet();
            foreach (var beneficiaryId in beneficiaryIds)
                foreach (var requirement in requirements)
                    if (!existing.Contains((beneficiaryId, requirement.Id)))
                        context.BeneficiaryRequirementAssignments.Add(new BeneficiaryRequirementAssignment
                        {
                            BeneficiaryId = beneficiaryId,
                            RequirementId = requirement.Id,
                            IsRequired = requirement.IsRequired
                        });
            context.SaveChanges();
        }

        private static void ApplyRequestedDevelopmentPasswords(ApplicationDbContext context, string? sharedPassword)
        {
            var requestedPassword = sharedPassword ?? Environment.GetEnvironmentVariable("SIMPLEX_SEED_PASSWORD");
            if (string.IsNullOrWhiteSpace(requestedPassword)) return;
            var testEmails = new[] { "director@simplex.com", "naledi.khumalo@simplex.com", "sipho.nkosi@simplex.com", "david.pillay@simplex.com", "nomsa.zulu@simplex.com", "accountant@simplex.com", "thabo.mthembu@example.com" };
            var hash = BCrypt.Net.BCrypt.HashPassword(requestedPassword);
            foreach (var user in context.Users.Where(x => testEmails.Contains(x.Email)))
            {
                user.PasswordHash = hash;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.RememberMeToken = null;
            }
            context.SaveChanges();
        }

        private static string GetSeedPassword() =>
            Environment.GetEnvironmentVariable("SIMPLEX_SEED_PASSWORD")
            ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        private static string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-").TrimEnd('=');
        }

        private static void SeedKaoticBeneficiaryVerificationScenario(ApplicationDbContext context, string portalPassword)
        {
            const string email = "ndlovukhetha818@gmail.com";
            var benefactor = context.Clients.OrderBy(x => x.Id).FirstOrDefault();
            if (benefactor is null) return;

            var beneficiary = context.Beneficiaries.SingleOrDefault(x => x.Email == email);
            if (beneficiary is null)
            {
                beneficiary = new Beneficiary { Email = email };
                context.Beneficiaries.Add(beneficiary);
            }

            beneficiary.BenefactorClientId = benefactor.Id;
            beneficiary.FirstName = "Kaotic";
            beneficiary.LastName = "Being";
            beneficiary.RelationshipToBenefactor = "Friend";
            beneficiary.AssetAccessTerms = "TEST DATA ONLY. The recorded entitlement is subject to the governing estate or trust instrument, the stated conditions, and the Director's final decision. Facial verification does not approve or release any asset.";
            beneficiary.PermittedAssetPurposes = "Test scenario: view the recorded farm-interest entitlement only. No transfer, payment, client service, matter access, or member benefit is granted.";
            beneficiary.EntitlementDescription = "Test scenario: a potential farm-interest entitlement after documented graduation and final authorised review.";
            if (beneficiary.Status is BeneficiaryStatus.Draft or BeneficiaryStatus.InvitationSent or BeneficiaryStatus.AwaitingDocuments or BeneficiaryStatus.DocumentsRequireResubmission)
                beneficiary.Status = BeneficiaryStatus.AwaitingFacialVerification;
            beneficiary.PortalAccessEnabled = true;
            if (string.IsNullOrWhiteSpace(beneficiary.PortalPasswordHash) || !BCrypt.Net.BCrypt.Verify(portalPassword, beneficiary.PortalPasswordHash))
            {
                beneficiary.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword(portalPassword);
                beneficiary.PortalPasswordSetAtUtc = DateTime.UtcNow;
            }
            context.SaveChanges();

            var identityRequirement = context.BeneficiaryDocumentRequirements.Single(x => x.Code == "SA_ID");
            if (!context.BeneficiaryRequirementAssignments.Any(x => x.BeneficiaryId == beneficiary.Id && x.RequirementId == identityRequirement.Id))
            {
                context.BeneficiaryRequirementAssignments.Add(new BeneficiaryRequirementAssignment
                {
                    BeneficiaryId = beneficiary.Id,
                    RequirementId = identityRequirement.Id,
                    IsRequired = true
                });
            }

            var idDocument = context.BeneficiaryDocuments.OrderByDescending(x => x.UploadedAtUtc)
                .FirstOrDefault(x => x.BeneficiaryId == beneficiary.Id && x.RequirementId == identityRequirement.Id);
            if (idDocument is null)
            {
                var source = Path.Combine(Directory.GetCurrentDirectory(), "Data", "SeedAssets", "kaotic-being-test-id.jpg");
                if (!File.Exists(source)) throw new FileNotFoundException("Kaotic test ID asset is missing.", source);

                var relativePath = Path.Combine(beneficiary.Id.ToString(), "kaotic-being-test-id.bin").Replace('\\', '/');
                var destination = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "SecureBeneficiaryDocuments", relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                using var input = File.OpenRead(destination);
                context.BeneficiaryDocuments.Add(new BeneficiaryDocument
                {
                    BeneficiaryId = beneficiary.Id,
                    RequirementId = identityRequirement.Id,
                    OriginalFileName = "TEST_ONLY_Kaotic-Being-ID-reference.jpg",
                    StoredFileName = Path.GetFileName(destination),
                    RelativeStoragePath = relativePath,
                    ContentType = "image/jpeg",
                    SizeBytes = new FileInfo(destination).Length,
                    Sha256Hash = Convert.ToHexString(sha256.ComputeHash(input)),
                    PreScreenStatus = DocumentPreScreenStatus.Passed,
                    UserFacingReason = "Test seed: documentation assumed complete. Complete live facial verification to submit the request.",
                    AnalysedAtUtc = DateTime.UtcNow
                });
            }

            var activeInvitations = context.BeneficiaryInvitations
                .Where(x => x.BeneficiaryId == beneficiary.Id && x.UsedAtUtc == null && x.RevokedAtUtc == null)
                .ToList();
            activeInvitations.ForEach(x => x.RevokedAtUtc = DateTime.UtcNow);
            context.SaveChanges();
        }
    }
}
