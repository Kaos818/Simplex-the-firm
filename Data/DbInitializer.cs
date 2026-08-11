using SimplexLawFirm.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context, bool isDevelopment = false, string? kaoticPortalPassword = null, string? sharedPassword = null, string? contentRootPath = null)
        {
            if (!context.LegalAuthorities.Any())
            {
                context.LegalAuthorities.AddRange(
                    new LegalAuthority { Citation="Constitution of the Republic of South Africa, 1996",Subject="Constitutional law",Summary="Supreme binding law governing rights, legality and fair process.",SearchText="rights equality dignity fair hearing administrative justice",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="PREAMBLE\n\nWe, the people of South Africa, ... adopt this Constitution as the supreme law of the Republic.\n\n[9] Everyone is equal before the law and has the right to equal protection and benefit of the law.\n\n[33] Everyone has the right to administrative action that is lawful, reasonable and procedurally fair. Everyone whose rights have been adversely affected by administrative action has the right to be given written reasons.\n\n[34] Everyone has the right to have any dispute that can be resolved by the application of law decided in a fair public hearing before a court or, where appropriate, another independent and impartial tribunal or forum." },
                    new LegalAuthority { Citation="Barkhuizen v Napier 2007 (5) SA 323 (CC)",Subject="Contract law",Summary="Public policy and constitutional fairness in enforcement of contractual time bars.",SearchText="contract public policy fairness time bar",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE CONSTITUTIONAL COURT OF SOUTH AFRICA — Barkhuizen v Napier 2007 (5) SA 323 (CC)\n\n[1] This matter concerns the enforceability of a time-limitation clause in a short-term insurance policy.\n\n[57] Contractual terms that are contrary to public policy are unenforceable. Public policy imports notions of fairness, justice and reasonableness, and takes into account the need to enforce contracts that have been freely and voluntarily entered into, as well as the doctrine of pacta sunt servanda.\n\n[59] The proper approach is to determine whether the clause is contrary to public policy, and, if it is not, whether it should nonetheless not be enforced because of the manner in which one of the parties has enforced it." },
                    new LegalAuthority { Citation="Sidumo v Rustenburg Platinum Mines Ltd 2008 (2) SA 24 (CC)",Subject="Labour law",Summary="Sets the constitutional standard for review of arbitration awards.",SearchText="labour dismissal arbitration review fairness reasonableness",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE CONSTITUTIONAL COURT OF SOUTH AFRICA — Sidumo v Rustenburg Platinum Mines Ltd 2008 (2) SA 24 (CC)\n\n[105] The question is whether the decision reached by the commissioner is one that a reasonable decision-maker could not reach.\n\n[110] Whether a decision is reasonable will depend on the circumstances of each case, including the nature of the decision, the identity and expertise of the decision-maker, the range of factors relevant to the decision, the reasons given for it, and the nature of the competing interests involved." },
                    new LegalAuthority { Citation="S v Makwanyane 1995 (3) SA 391 (CC)",Subject="Constitutional law",Summary="Leading authority on dignity, life and proportional constitutional reasoning.",SearchText="constitutional dignity life rights proportionality",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE CONSTITUTIONAL COURT OF SOUTH AFRICA — S v Makwanyane 1995 (3) SA 391 (CC)\n\n[144] The right to life and the right to dignity are the most important of all human rights, and the source of all other personal rights.\n\n[104] The question whether a limitation of a right is proportionate to the objective sought to be achieved requires a weighing of the nature and extent of the limitation against the importance of the purpose it serves." },
                    new LegalAuthority { Citation="Legacy procedural guidance (superseded)",Subject="Civil procedure",Summary="Historic procedural guidance retained with a warning that later rules have overtaken it.",SearchText="civil procedure hearing filing deadline court",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.Superseded,
                        FullText="INTERNAL PROCEDURAL NOTE (superseded)\n\nThis guidance on filing deadlines for interlocutory applications reflects the rules as they stood prior to the most recent Uniform Rules amendment.\n\nEditorial note: this guidance has been superseded by later amendments to the applicable Rules of Court. Do not rely on the specific deadlines below without checking the current rules — retained here only for historical context." },
                    new LegalAuthority { Citation="Simplex internal commercial precedent collection",Subject="Commercial law",Summary="Firm precedent fallback only; external-source availability must be verified before reliance.",SearchText="contract shareholder commercial litigation evidence procedure",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,IsInternalFallback=true,
                        FullText="SIMPLEX INTERNAL PRECEDENT — Commercial matters\n\nA collection of the firm's own commercial litigation notes and settlement reasoning, retained for continuity across matters. Use only as a starting point when external legal-database access is unavailable, and confirm every proposition against an authoritative external source once access is restored." },
                    new LegalAuthority { Citation="Simplex internal employment precedent collection",Subject="Labour law",Summary="Internal fallback on dismissal and CCMA process; verify against external authority.",SearchText="labour dismissal ccma arbitration fairness",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,IsInternalFallback=true,
                        FullText="SIMPLEX INTERNAL PRECEDENT — Employment matters\n\nNotes from past dismissal and CCMA arbitration matters handled by the firm. This internal collection is a limited fallback only, used when external legal-database access is unavailable, and should be confirmed against binding external authority before reliance." },
                    new LegalAuthority { Citation="Simplex internal civil procedure precedent collection",Subject="Civil procedure",Summary="Internal fallback on hearings, filing and court process; verify against external authority.",SearchText="civil procedure hearing filing deadline court",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,IsInternalFallback=true,
                        FullText="SIMPLEX INTERNAL PRECEDENT — Civil procedure\n\nInternal notes on hearing, filing and court-process practice built up across the firm's own matters. A limited fallback only, for use when external legal-database access is unavailable; confirm against the current Rules of Court before reliance." },
                    new LegalAuthority { Citation="Road Accident Fund v Mtati 2005 (6) SA 215 (SCA)",Subject="Personal injury",Summary="Assessment of general damages and loss of earning capacity in RAF motor-collision claims.",SearchText="road accident fund personal injury motor vehicle collision damages loss of earnings",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE SUPREME COURT OF APPEAL — Road Accident Fund v Mtati 2005 (6) SA 215 (SCA)\n\n[15] In assessing general damages for pain, suffering and loss of amenities, the court has regard to previous comparable awards, adjusted for changes in the value of money, while recognising that no two cases are identical.\n\n[22] Loss of earning capacity must be proved on a balance of probabilities, but where the nature of the injury makes precise proof impossible the court may make a fair and reasonable estimate on the evidence available." },
                    new LegalAuthority { Citation="Bee v Road Accident Fund 2018 (4) SA 366 (SCA)",Subject="Personal injury",Summary="Confirms the Fund's liability where the identity of the negligent driver cannot be established but negligence is proved.",SearchText="road accident fund unidentified driver negligence liability collision",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE SUPREME COURT OF APPEAL — Bee v Road Accident Fund 2018 (4) SA 366 (SCA)\n\n[38] Where the identity of the driver who caused the collision cannot be established, the claimant must nonetheless prove negligence on the part of the unidentified driver on a balance of probabilities for the Fund to be liable." },
                    new LegalAuthority { Citation="RAF quantum guideline (regional, distinguished on facts)",Subject="Personal injury",Summary="A regional quantum guideline distinguished where the claimant's injuries and vocational impact diverge materially from the comparator cases it was built on.",SearchText="road accident fund quantum general damages comparator award",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.Distinguished,
                        FullText="INTERNAL QUANTUM NOTE (distinguished)\n\nThis guideline collates regional general-damages awards for whiplash-type soft-tissue injuries. It has repeatedly been distinguished where the claimant suffered orthopaedic or neurological injury with lasting vocational impact, since the comparator cases underlying the guideline involved materially less severe injuries. Do not apply without first confirming the claimant's injury profile matches the comparator set." },
                    new LegalAuthority { Citation="Heaton v Heaton 2019 (2) SA 471 (GJ)",Subject="Family law",Summary="Best-interests-of-the-child standard applied to a contested primary-residence dispute.",SearchText="divorce custody primary residence best interests of the child care contact",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE HIGH COURT OF SOUTH AFRICA (GAUTENG LOCAL DIVISION) — Heaton v Heaton 2019 (2) SA 471 (GJ)\n\n[41] The best interests of the child are the paramount consideration in any dispute concerning care, contact or primary residence, and outweigh the preferences or convenience of either parent.\n\n[47] Continuity of care, the child's relationship with each parent, and the practical realities of schooling and support networks are all weighed in determining where a child's primary residence should be." },
                    new LegalAuthority { Citation="Kroon v Kroon 1986 (4) SA 616 (E)",Subject="Family law",Summary="Redistribution of assets on divorce out of community of property without accrual.",SearchText="divorce matrimonial property redistribution assets marriage out of community",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE HIGH COURT (EASTERN CAPE DIVISION) — Kroon v Kroon 1986 (4) SA 616 (E)\n\n[9] Where the parties married out of community of property without accrual, a redistribution order may nonetheless be granted under section 7(3) of the Divorce Act where one spouse's contribution to the maintenance or increase of the other's estate would otherwise go unrecognised." },
                    new LegalAuthority { Citation="Pre-2019 maintenance calculation practice note (superseded)",Subject="Family law",Summary="An older maintenance-calculation approach retained for historical reference only.",SearchText="divorce maintenance calculation child support spousal",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.Superseded,
                        FullText="INTERNAL FAMILY LAW NOTE (superseded)\n\nThis practice note set out the firm's earlier approach to estimating maintenance contributions. It has been superseded by updated Maintenance Court guidance and more recent High Court authority on the relevant factors; retained here only for historical context." },
                    new LegalAuthority { Citation="Novartis SA (Pty) Ltd v Maphil Trading (Pty) Ltd 2016 (1) SA 518 (SCA)",Subject="Commercial law",Summary="Restates the proper approach to interpretation of contractual documents in commercial disputes.",SearchText="contract interpretation commercial dispute breach agreement supplier",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE SUPREME COURT OF APPEAL — Novartis SA (Pty) Ltd v Maphil Trading (Pty) Ltd 2016 (1) SA 518 (SCA)\n\n[28] Interpretation is the process of attributing meaning to the words used in a document, having regard to the context provided by reading the particular provision in the light of the document as a whole and the circumstances attendant upon its coming into existence.\n\n[29] Where the language of the document is unambiguous, courts should not diverge from its ordinary grammatical meaning merely because a different reading would appear more businesslike or equitable." },
                    new LegalAuthority { Citation="Data Colour International (Pty) Ltd v Intamarket (Pty) Ltd 2001 (2) SA 284 (SCA)",Subject="Commercial law",Summary="Enforceability of restraint-of-trade clauses in commercial and supply agreements.",SearchText="restraint of trade breach of contract commercial supplier reasonableness",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE SUPREME COURT OF APPEAL — Data Colour International (Pty) Ltd v Intamarket (Pty) Ltd 2001 (2) SA 284 (SCA)\n\n[16] A restraint of trade is enforceable unless the party seeking to escape it shows that enforcement would be contrary to public policy, ordinarily because it is unreasonable having regard to the interests of the parties and the public." },
                    new LegalAuthority { Citation="Older breach-of-contract remedies note (overturned on appeal)",Subject="Commercial law",Summary="An earlier position on cancellation and restitution overturned by subsequent appellate authority.",SearchText="breach of contract cancellation restitution commercial remedy damages",Rank=AuthorityRank.Persuasive,Treatment=AuthorityTreatment.Overturned,
                        FullText="INTERNAL COMMERCIAL NOTE (overturned)\n\nThis note reflected an earlier first-instance approach to the interplay between cancellation and restitution following breach. That approach was overturned on appeal by binding appellate authority applying a different test; do not rely on the reasoning below without checking current binding precedent." },
                    new LegalAuthority { Citation="Le Roux v Dey 2011 (3) SA 274 (CC)",Subject="General",Summary="Delictual liability, wrongfulness and constitutional balancing of competing rights.",SearchText="delict wrongfulness damages dignity reputation general civil",Rank=AuthorityRank.Binding,Treatment=AuthorityTreatment.GoodLaw,
                        FullText="IN THE CONSTITUTIONAL COURT OF SOUTH AFRICA — Le Roux v Dey 2011 (3) SA 274 (CC)\n\n[122] Wrongfulness in the law of delict is ultimately dependent on a judicial determination of whether, assuming all the other elements of liability are present, it would be reasonable to impose liability, having regard to the legal convictions of the community as informed by constitutional norms." });
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
            // Fee rules and recorded venue coordinates are reference data that the cost
            // estimator and the check-in geofence read at runtime, so they are required
            // wherever the app runs. Gating them to development left a deployed site with
            // an empty venue list and no basis for verifying an attorney's location.
            EnsureCostEstimatorSeedData(context);
            EnsureExpansionSeedData(context, contentRootPath);
            EnsureCaseNoteSeedData(context);
            ApplyRequestedDevelopmentPasswords(context, sharedPassword);
        }

        /// <summary>
        /// Writes the mileage log that the seeded reimbursement claim cites as its proof and
        /// returns its size. Returns a nominal size when no content root is supplied, which is
        /// the case under tests that never serve the file.
        /// </summary>
        /// <summary>
        /// Outcomes for the closed-matter library, spread so the forecaster reports a
        /// realistic mixed success rate rather than a uniform one.
        /// </summary>
        /// <summary>
        /// Case notes for the live matters. Research is triggered by highlighting a passage in
        /// a note, so without notes that entry point cannot be exercised at all. Each note is
        /// written to contain a recognisable legal issue for the extraction step to pick up.
        /// </summary>
        private static void EnsureCaseNoteSeedData(ApplicationDbContext context)
        {
            var notes = new (string CaseNumber, string Content, bool Privileged)[]
            {
                ("PI-2026-0001", "Consultation with the client on 14 March. The client was a passenger in a vehicle struck from behind at the intersection of Umgeni Road and Argyle Road. The central question is whether the insured driver was negligent in failing to keep a proper lookout and whether that negligence caused the collision. The client sustained a fractured tibia and soft-tissue injury to the cervical spine. We must also consider whether the plaintiff is entitled to general damages for pain and suffering, and how loss of earning capacity is to be assessed where the client is self-employed.", false),
                ("PI-2026-0001", "Medico-legal report received from the orthopaedic surgeon. The report records a 14% whole person impairment. The question arises whether the injury meets the serious injury threshold for general damages under the Road Accident Fund Act, and whether the Fund is liable where the identity of the negligent driver is disputed.", true),
                ("FM-2026-0002", "Attendance on the client regarding the divorce action. The parties were married in community of property. The issue is whether the maintenance claimed for the two minor children is reasonable having regard to the respondent's disclosed income, and whether the client is entitled to a redistribution of the joint estate. The respondent has failed to make full financial disclosure, which raises the question whether an adverse inference may be drawn.", false),
                ("FM-2026-0002", "The respondent's attorney has proposed a settlement on maintenance but not on the patrimonial claim. We must advise whether accepting a partial settlement prejudices the client's remaining claim, and whether the best interests of the minor children require a family advocate's report before any consent paper is signed.", true),
                ("BC-2026-0003", "Review of the supply agreement. The dispute concerns late delivery of stock over the 2025 financial year. The question is whether the defendant breached a material term of the agreement, and whether the damages claimed for lost profit were within the contemplation of the parties at the time of contracting. The agreement contains a limitation of liability clause, so we must consider whether that clause is enforceable against a claim founded on gross negligence.", false),
                ("BC-2026-0003", "Client instructs that cancellation was communicated telephonically. The issue is whether valid cancellation occurred where the agreement requires written notice, and whether the client's continued acceptance of deliveries after the alleged breach amounts to an election to uphold the contract.", false)
            };

            foreach (var note in notes)
            {
                var matter = context.Cases.SingleOrDefault(x => x.CaseNumber == note.CaseNumber);
                if (matter is null) continue;
                if (context.CaseNotes.Any(x => x.CaseId == matter.Id && x.Content == note.Content)) continue;
                context.CaseNotes.Add(new CaseNote
                {
                    CaseId = matter.Id,
                    Content = note.Content,
                    IsPrivileged = note.Privileged,
                    CreatedAt = new DateTime(2026, 3, 14 + Array.IndexOf(notes, note))
                });
            }
            context.SaveChanges();
        }

        private static ForecastResult ClosedMatterOutcome(int index) => (index % 6) switch
        {
            0 => ForecastResult.Unsuccessful,
            2 or 5 => ForecastResult.PartlySuccessful,
            _ => ForecastResult.Successful
        };

        private static decimal ClosedMatterEvidenceStrength(int index) =>
            Math.Round(.35m + (index % 6) * .1m, 2);

        private static string ClosedMatterTitle(string matterType, int index)
        {
            var parties = matterType switch
            {
                "Personal Injury" => new[] { "Ngcobo vs Road Accident Fund", "Naidoo vs Ethekwini Metro", "Botha vs Coastal Transport", "Sithole vs Road Accident Fund", "Adams vs Blue Line Couriers", "Maharaj vs Road Accident Fund", "Khoza vs Umgeni Municipality", "Pillay vs Southern Freight", "Dlomo vs Road Accident Fund", "Petersen vs Harbour Logistics", "Zulu vs Metro Bus Services", "Reddy vs Road Accident Fund" },
                "Family Law" => new[] { "Mbatha Divorce Proceedings", "Govender Maintenance Application", "Steyn Custody Variation", "Ndlovu Divorce Proceedings", "Fourie Guardianship Application", "Cele Maintenance Enforcement", "Jacobs Divorce Proceedings", "Mkhize Custody Dispute", "Roux Patrimonial Claim", "Sibiya Maintenance Review", "Daniels Divorce Proceedings", "Ngubane Relocation Application" },
                "Commercial" => new[] { "Zenith Supplies vs Kruger Holdings", "Ubuntu Trading vs Delta Freight", "Highpoint Ltd vs Marais Group", "Coastal Foods vs Naicker Wholesale", "Vantage Systems vs Orbit Media", "Summit Steel vs Reddy Fabrication", "Anchor Retail vs Pioneer Distributors", "Meridian Tech vs Cascade Software", "Sandton Estates vs Verwey Construction", "Nexus Logistics vs Harbour Cranes", "Silverline Mining vs Trans-Rand Plant", "Kingfisher Foods vs Apex Packaging" },
                _ => new[] { "Estate Late Mahlangu", "Xaba Contractual Dispute", "Van Wyk Property Transfer", "Molefe Labour Referral", "Abrahams Estate Administration", "Nkosi Lease Dispute", "Brand Servitude Application", "Radebe Insurance Claim", "Olivier Sectional Title Dispute", "Sithembiso Trust Variation", "Coetzee Debt Review", "Hadebe Municipal Objection" }
            };
            return parties[(index - 1) % parties.Length];
        }

        private static string ClosedMatterDescription(string matterType) => matterType switch
        {
            "Personal Injury" => "Closed delictual claim for bodily injury, pleaded on negligence with quantum supported by medico-legal assessment.",
            "Family Law" => "Closed matrimonial matter dealing with the dissolution of the marriage and the ancillary relief sought by the parties.",
            "Commercial" => "Closed commercial dispute concerning breach of a written agreement and the damages flowing from that breach.",
            _ => "Closed general litigation matter retained in the firm's precedent library for comparison and research."
        };

        private static string ClosedMatterOutcomeSummary(string matterType, ForecastResult outcome)
        {
            var issue = matterType switch
            {
                "Personal Injury" => "whether the defendant's negligent driving caused the plaintiff's injuries, and the quantum of general damages",
                "Family Law" => "whether the maintenance sought was reasonable having regard to the means of the parties and the needs of the minor children",
                "Commercial" => "whether the defendant breached a material term of the agreement, and whether the damages claimed were within the contemplation of the parties",
                _ => "whether the applicant established the relief sought on the papers before the court"
            };
            return outcome switch
            {
                ForecastResult.Successful => $"The court found for our client on the central issue: {issue}. Liability was determined in the client's favour and costs followed the result.",
                ForecastResult.PartlySuccessful => $"The court accepted part of our client's case on the question of {issue}. Relief was granted on a reduced basis and each party bore its own costs.",
                _ => $"The court found against our client on the question of {issue}. The claim was dismissed and an adverse costs order was made."
            };
        }

        private static long EnsureSeedReimbursementProof(string? contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath)) return 48213;
            try
            {
                var path = Path.Combine(contentRootPath, "App_Data", "SecureReimbursementProofs", "seed", "mileage-log.pdf");
                if (File.Exists(path)) return new FileInfo(path).Length;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, BuildMileageLogPdf());
                return new FileInfo(path).Length;
            }
            catch (Exception)
            {
                // A read-only or unavailable data directory must not stop the rest of the seeding.
                return 48213;
            }
        }

        private static byte[] BuildMileageLogPdf()
        {
            var lines = new[]
            {
                "(Simplex Law - Travel Reimbursement Proof) Tj",
                "0 -28 Td (Claim: RMB-2026-NALEDI01) Tj",
                "0 -20 Td (Attorney: Naledi Khumalo) Tj",
                "0 -20 Td (Matter: PI-2026-0001 Mthembu vs Road Accident Fund) Tj",
                "0 -20 Td (Route: Durban CBD to Durban High Court \\(return\\)) Tj",
                "0 -20 Td (Distance: 38.2 km at R38.00 per km) Tj",
                "0 -20 Td (Amount claimed: R1 450.00) Tj"
            };
            var content = "BT /F1 12 Tf 60 760 Td " + string.Join(" ", lines) + " ET";

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream"
            };

            var builder = new System.Text.StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(builder.Length);
                builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
            }

            var xrefOffset = builder.Length;
            builder.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
            builder.Append("0000000000 65535 f \n");
            foreach (var offset in offsets) builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
            builder.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n")
                   .Append(xrefOffset).Append("\n%%EOF");

            return System.Text.Encoding.ASCII.GetBytes(builder.ToString());
        }

        private static void EnsureExpansionSeedData(ApplicationDbContext context, string? contentRootPath = null)
        {
            // Known venue coordinates — needed for GPS geofence verification on attorney check-in.
            if (!context.KnownVenues.Any())
            {
                context.KnownVenues.AddRange(
                    new KnownVenue { Name = "Durban High Court", Latitude = -29.8579, Longitude = 31.0292 },
                    new KnownVenue { Name = "Johannesburg High Court", Latitude = -26.2023, Longitude = 28.0436 },
                    new KnownVenue { Name = "Pinetown Magistrate's Court", Latitude = -29.8149, Longitude = 30.8676 },
                    new KnownVenue { Name = "Verulam Magistrate's Court", Latitude = -29.6389, Longitude = 31.0525 });
                context.SaveChanges();
            }

            // Expense policies — without these every reimbursement claim fails validation at proof-submission time.
            if (!context.ExpensePolicies.Any())
            {
                context.ExpensePolicies.AddRange(
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Travel, PerItemLimit = 3500m, DelegatedApprovalLimit = 1200m, DefaultClassification = ExpenseClassification.ClientRecoverable, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.CourtFiling, PerItemLimit = 5000m, DelegatedApprovalLimit = 2000m, DefaultClassification = ExpenseClassification.ClientRecoverable, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.SheriffService, PerItemLimit = 4000m, DelegatedApprovalLimit = 1500m, DefaultClassification = ExpenseClassification.ClientRecoverable, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Courier, PerItemLimit = 800m, DelegatedApprovalLimit = 500m, DefaultClassification = ExpenseClassification.ClientRecoverable, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Accommodation, PerItemLimit = 6000m, DelegatedApprovalLimit = 2500m, DefaultClassification = ExpenseClassification.ClientRecoverable, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Meals, PerItemLimit = 700m, DelegatedApprovalLimit = 700m, DefaultClassification = ExpenseClassification.FirmOverhead, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Parking, PerItemLimit = 300m, DelegatedApprovalLimit = 300m, DefaultClassification = ExpenseClassification.FirmOverhead, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Printing, PerItemLimit = 500m, DelegatedApprovalLimit = 500m, DefaultClassification = ExpenseClassification.FirmOverhead, IsActive = true },
                    new ExpensePolicy { ExpenseType = ReimbursementExpenseType.Other, PerItemLimit = 1000m, DelegatedApprovalLimit = 500m, DefaultClassification = ExpenseClassification.FirmOverhead, IsActive = true }
                );
                context.SaveChanges();
            }

            var naledi = context.Users.SingleOrDefault(x => x.Email == "naledi.khumalo@simplex.com");
            var piCase = context.Cases.SingleOrDefault(x => x.CaseNumber == "PI-2026-0001");
            var director = context.Users.FirstOrDefault(x => x.Role == UserRole.Director || x.Role == UserRole.Admin);

            // A completed court appearance for Naledi to validate her expense claim against.
            CalendarEvent nalediCourtEvent = null;
            if (naledi != null && piCase != null)
            {
                nalediCourtEvent = context.CalendarEvents.SingleOrDefault(x => x.CaseId == piCase.Id && x.AssignedToUserId == naledi.Id && x.Title == "RAF hearing — Mthembu");
                if (nalediCourtEvent is null)
                {
                    nalediCourtEvent = new CalendarEvent
                    {
                        Title = "RAF hearing — Mthembu", Description = "Motion hearing before the Road Accident Fund tribunal.", Location = "Durban High Court",
                        StartDateTime = DateTime.Today.AddDays(-9).AddHours(9), EndDateTime = DateTime.Today.AddDays(-9).AddHours(11),
                        Type = EventType.CourtAppearance, Status = EventStatus.Completed, AssignedToUserId = naledi.Id, CaseId = piCase.Id,
                        ActualStartTime = DateTime.Today.AddDays(-9).AddHours(9), ActualEndTime = DateTime.Today.AddDays(-9).AddHours(11),
                        CreatedByUserId = naledi.Id, CreatedAt = DateTime.Today.AddDays(-10), Attendees = [], Reminders = [], ChildEvents = []
                    };
                    context.CalendarEvents.Add(nalediCourtEvent);
                    context.SaveChanges();
                }

                // A due-dated task so the calendar's merged Events + Tasks feed has something to show.
                if (!context.Tasks.Any(x => x.Title == "Prepare RAF settlement bundle"))
                {
                    context.Tasks.Add(new TaskItem
                    {
                        Title = "Prepare RAF settlement bundle", Description = "Compile medical reports and loss-of-income schedule for settlement negotiation.",
                        Priority = TaskPriority.High, Status = SimplexLawFirm.Models.TaskStatus.InProgress, DueDate = DateTime.Today.AddDays(4),
                        AssignedToId = naledi.Id, CreatedById = naledi.Id, CaseId = piCase.Id, IsBillable = true, CreatedAt = DateTime.Now,
                        Comments = [], Attachments = [], SubTasks = []
                    });
                    context.SaveChanges();
                }

                // A reimbursement claim for Naledi, already through proof submission and awaiting director decision.
                if (director != null && !context.ReimbursementClaims.Any(x => x.ClaimNumber == "RMB-2026-NALEDI01"))
                {
                    var policy = context.ExpensePolicies.Single(x => x.ExpenseType == ReimbursementExpenseType.Travel);
                    // The claim points at a stored proof document, so the document has to exist:
                    // without it the download action throws instead of returning the file.
                    var proofSize = EnsureSeedReimbursementProof(contentRootPath);
                    context.ReimbursementClaims.Add(new ReimbursementClaim
                    {
                        ClaimNumber = "RMB-2026-NALEDI01", CaseId = piCase.Id, AttorneyId = naledi.Id,
                        ExpenseType = ReimbursementExpenseType.Travel, ExpenseDate = nalediCourtEvent.StartDateTime.Date, Amount = 1450m,
                        Description = "Return mileage Durban CBD to Durban High Court for the RAF motion hearing.",
                        Status = ReimbursementStatus.PendingDirector,
                        MatchedActivityType = ReimbursementActivityType.CourtEvent, MatchedActivityId = nalediCourtEvent.Id,
                        ProofOriginalFileName = "mileage-log.pdf", ProofRelativePath = "seed/mileage-log.pdf", ProofContentType = "application/pdf",
                        ProofSizeBytes = proofSize, ProofSha256Hash = "seed-naledi-travel-2026-01",
                        PolicyLimitSnapshot = policy.PerItemLimit, DelegatedLimitSnapshot = policy.DelegatedApprovalLimit, ExceedsPolicyLimit = false,
                        Classification = ExpenseClassification.ClientRecoverable, ClassificationReason = "Classified by the active Travel expense policy.",
                        SubmittedAtUtc = DateTime.UtcNow.AddDays(-2), AuditEntries = []
                    });
                    context.SaveChanges();
                }
            }

            // A document request in progress so both the lawyer and client views have real data.
            var thabo = context.Clients.SingleOrDefault(x => x.Email == "thabo.mthembu@example.com");
            if (naledi != null && piCase != null && thabo != null && !context.DocumentRequests.Any(x => x.Title == "Updated medical report"))
            {
                context.DocumentRequests.Add(new DocumentRequest
                {
                    CaseId = piCase.Id, ClientId = thabo.Id, RequestedByUserId = naledi.Id,
                    Title = "Updated medical report", Instructions = "Please upload the specialist's follow-up report from your most recent consultation.",
                    Status = DocumentRequestStatus.Requested, DueAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
                });
                context.SaveChanges();
            }

            // A handover request awaiting director review so the director's queue isn't empty.
            var sipho = context.Users.SingleOrDefault(x => x.Email == "sipho.nkosi@simplex.com");
            var divorceCase = context.Cases.SingleOrDefault(x => x.CaseNumber == "FM-2026-0002");
            if (sipho != null && divorceCase != null && !context.CaseHandoverRequests.Any(x => x.CaseId == divorceCase.Id && x.Status == HandoverRequestStatus.Pending))
            {
                context.CaseHandoverRequests.Add(new CaseHandoverRequest
                {
                    CaseId = divorceCase.Id, RequestedByUserId = sipho.Id,
                    Reason = "I have a scheduling conflict with an extended trial in another matter and cannot give this matter the attention the mediation phase needs.",
                    Status = HandoverRequestStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                });
                context.SaveChanges();
            }
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
            // Twelve per type clears the forecaster's "High" confidence threshold of ten
            // comparables, so a demonstration reaches the strongest band rather than
            // sitting on "Developing" with a bare quorum.
            const int comparablesPerType = 12;
            var hourVariance = new[] { 1.0m, 1.4m, .7m, 1.9m, 1.1m, .85m, 1.6m, 1.25m, .95m, 1.75m, 1.15m, .8m };
            for (var typeIndex = 0; typeIndex < matterTypes.Length; typeIndex++)
            {
                var matterType = matterTypes[typeIndex];
                var prefix = matterType.Replace(" ", "").ToUpperInvariant()[..Math.Min(4, matterType.Replace(" ", "").Length)];
                for (var index = 1; index <= comparablesPerType; index++)
                {
                    var caseNumber = $"EST-HIST-{prefix}-{index:00}";
                    var matter = context.Cases.SingleOrDefault(x => x.CaseNumber == caseNumber);
                    var closedOutcome = ClosedMatterOutcome(index);
                    var matterValue = 180000m * index * hourVariance[index - 1];
                    if (matter is null)
                    {
                        matter = new Case { CaseNumber = caseNumber, Title = ClosedMatterTitle(matterType, index), Description = ClosedMatterDescription(matterType), CaseType = matterType, ClientId = client.Id, LawyerId = lawyerIds[(typeIndex + index - 1) % lawyerIds.Count], Status = CaseStatus.Closed, CreatedAt = new DateTime(2025, index % 12 + 1, 10), UpdatedAt = new DateTime(2025, (index + 2) % 12 + 1, 20), MatterValue = matterValue };
                        context.Cases.Add(matter);
                        context.SaveChanges();
                    }
                    // The forecaster only counts a closed matter as comparable once it carries a
                    // recorded outcome, so backfill any matter seeded before outcomes were stored.
                    if (matter.RecordedOutcome is null)
                    {
                        matter.RecordedOutcome = closedOutcome;
                        matter.OutcomeSummary = ClosedMatterOutcomeSummary(matterType, closedOutcome);
                        matter.EvidenceStrength = ClosedMatterEvidenceStrength(index);
                        matter.SettlementAmount = closedOutcome switch
                        {
                            ForecastResult.Successful => Math.Round(matterValue * .78m, 2),
                            ForecastResult.PartlySuccessful => Math.Round(matterValue * .41m, 2),
                            _ => 0m
                        };
                        context.SaveChanges();
                    }
                    if (!context.TimeEntries.Any(x => x.CaseId == matter.Id && x.Description == "Estimator historical legal work"))
                    {
                        var rate = context.LawyerProfiles.Where(x => x.UserId == matter.LawyerId).Select(x => x.HourlyRate).Single();
                        var hours = Math.Round((10m + typeIndex * 4m + index * 3m) * hourVariance[index - 1], 1);
                        var revenue = hours * rate;
                        context.TimeEntries.Add(new TimeEntry { CaseId = matter.Id, LawyerId = matter.LawyerId!.Value, Description = "Estimator historical legal work", Date = new DateTime(2025, index % 12 + 1, 15), Hours = hours, HourlyRate = rate, TotalAmount = revenue, IsBillable = true, IsBilled = true, CreatedAt = new DateTime(2025, index % 12 + 1, 15) });
                        if (!context.Invoices.Any(x => x.CaseId == matter.Id && x.InvoiceNumber == $"INV-{caseNumber}"))
                        {
                            var vat = Math.Round(revenue * .15m, 2);
                            context.Invoices.Add(new Invoice
                            {
                                ClientId = client.Id, CaseId = matter.Id, InvoiceNumber = $"INV-{caseNumber}",
                                Amount = revenue, TaxAmount = vat, TotalAmount = revenue + vat,
                                IssueDate = new DateTime(2025, index % 12 + 1, 20), DueDate = new DateTime(2025, index % 12 + 1, 20).AddDays(30),
                                PaidDate = new DateTime(2025, index % 12 + 1, 20).AddDays(21), IsPaid = true, Status = InvoiceStatus.Paid,
                                CreatedDate = new DateTime(2025, index % 12 + 1, 20), CreatedAt = new DateTime(2025, index % 12 + 1, 20),
                                Description = $"Closed matter revenue — {matterType} precedent {index}", Notes = "Development seed revenue for matter cost estimation."
                            });
                        }
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
            // Every seeded user gets the known dev password here — GetSeedPassword() generates a
            // random, unrecoverable hash for the initial seed when SIMPLEX_SEED_PASSWORD isn't set,
            // so any account left out of this reset can never be logged into in a fresh dev environment.
            var hash = BCrypt.Net.BCrypt.HashPassword(requestedPassword);
            foreach (var user in context.Users)
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
