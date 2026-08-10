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
            if (isDevelopment) EnsureCostEstimatorSeedData(context);
            ApplyRequestedDevelopmentPasswords(context, sharedPassword);
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
                for (var index = 1; index <= MatterCostEstimateService.MinimumComparableMatters; index++)
                {
                    var caseNumber = $"EST-HIST-{prefix}-{index:00}";
                    var matter = context.Cases.SingleOrDefault(x => x.CaseNumber == caseNumber);
                    if (matter is null)
                    {
                        matter = new Case { CaseNumber = caseNumber, Title = $"Closed {matterType} estimate precedent {index}", Description = "Development-only closed matter providing deterministic estimator history.", CaseType = matterType, ClientId = client.Id, LawyerId = lawyerIds[(typeIndex + index - 1) % lawyerIds.Count], Status = CaseStatus.Closed, CreatedAt = new DateTime(2025, index + 1, 10), UpdatedAt = new DateTime(2025, index + 3, 20), MatterValue = 250000m * index };
                        context.Cases.Add(matter);
                        context.SaveChanges();
                    }
                    if (!context.TimeEntries.Any(x => x.CaseId == matter.Id && x.Description == "Estimator historical legal work"))
                    {
                        var rate = context.LawyerProfiles.Where(x => x.UserId == matter.LawyerId).Select(x => x.HourlyRate).Single();
                        var hours = 8m + typeIndex * 3m + index * 2m;
                        context.TimeEntries.Add(new TimeEntry { CaseId = matter.Id, LawyerId = matter.LawyerId!.Value, Description = "Estimator historical legal work", Date = new DateTime(2025, index + 2, 15), Hours = hours, HourlyRate = rate, TotalAmount = hours * rate, IsBillable = true, IsBilled = true, CreatedAt = new DateTime(2025, index + 2, 15) });
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
            var testEmails = new[] { "director@simplex.com", "naledi.khumalo@simplex.com", "nomsa.zulu@simplex.com", "accountant@simplex.com", "thabo.mthembu@example.com" };
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
