CREATE TABLE [AuditEntries] (
    [Id] bigint NOT NULL IDENTITY,
    [ActorUserId] int NULL,
    [EntityType] nvarchar(450) NOT NULL,
    [EntityId] nvarchar(450) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [SafeMetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BeneficiaryDocumentRequirements] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [DisplayName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsRequired] bit NOT NULL,
    [RequiresCertifiedCopy] bit NOT NULL,
    [RequiresExpiryCheck] bit NOT NULL,
    [MaximumAgeDays] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BeneficiaryDocumentRequirements] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BiometricConsentRecords] (
    [Id] bigint NOT NULL IDENTITY,
    [VerificationSessionId] uniqueidentifier NOT NULL,
    [BeneficiaryId] int NOT NULL,
    [NoticeVersion] nvarchar(max) NOT NULL,
    [NoticeTextHash] nvarchar(max) NOT NULL,
    [ConsentGranted] bit NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [UserAgent] nvarchar(max) NULL,
    [IpAddressHash] nvarchar(max) NULL,
    CONSTRAINT [PK_BiometricConsentRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CaseDocumentRequirements] (
    [Id] int NOT NULL IDENTITY,
    [CaseType] nvarchar(120) NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(180) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Category] int NOT NULL,
    [Importance] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CaseDocumentRequirements] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CaseReadinessReviews] (
    [Id] bigint NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [ReviewedByUserId] int NOT NULL,
    [HeldCount] int NOT NULL,
    [MissingMandatoryCount] int NOT NULL,
    [MissingAdvisoryCount] int NOT NULL,
    [CourtReady] bit NOT NULL,
    [SnapshotJson] nvarchar(4000) NOT NULL,
    [ReviewedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_CaseReadinessReviews] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Clients] (
    [Id] int NOT NULL IDENTITY,
    [IsBusiness] bit NOT NULL,
    [FirstName] nvarchar(100) NULL,
    [LastName] nvarchar(100) NULL,
    [SAIDNumber] nvarchar(13) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [CompanyName] nvarchar(200) NULL,
    [RegistrationNumber] nvarchar(50) NULL,
    [Email] nvarchar(450) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [IdentificationNumber] nvarchar(max) NULL,
    CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CostEstimateCoverageGaps] (
    [Id] int NOT NULL IDENTITY,
    [MatterType] nvarchar(max) NOT NULL,
    [ComparableMatterCount] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [AcknowledgedAtUtc] datetime2 NULL,
    [AcknowledgedByUserId] int NULL,
    CONSTRAINT [PK_CostEstimateCoverageGaps] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [EmailOutboxMessages] (
    [Id] bigint NOT NULL IDENTITY,
    [ToAddress] nvarchar(254) NOT NULL,
    [Subject] nvarchar(300) NOT NULL,
    [HtmlBody] nvarchar(max) NOT NULL,
    [TextBody] nvarchar(max) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [AttemptCount] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [NextAttemptAtUtc] datetime2 NULL,
    [SentAtUtc] datetime2 NULL,
    [LastError] nvarchar(1000) NULL,
    [DeduplicationKey] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_EmailOutboxMessages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ExpensePolicies] (
    [Id] int NOT NULL IDENTITY,
    [ExpenseType] int NOT NULL,
    [PerItemLimit] decimal(18,2) NOT NULL,
    [DelegatedApprovalLimit] decimal(18,2) NOT NULL,
    [DefaultClassification] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ExpensePolicies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [KnowledgeArticles] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(240) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [IsPrivileged] bit NOT NULL,
    [IsConfidential] bit NOT NULL,
    [SuggestedSubjectId] int NULL,
    [AuthorUserId] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_KnowledgeArticles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LawyerSpecializations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_LawyerSpecializations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LegalAuthorities] (
    [Id] int NOT NULL IDENTITY,
    [Citation] nvarchar(300) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Summary] nvarchar(max) NOT NULL,
    [SearchText] nvarchar(max) NOT NULL,
    [Rank] int NOT NULL,
    [Treatment] int NOT NULL,
    [IsInternalFallback] bit NOT NULL,
    CONSTRAINT [PK_LegalAuthorities] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LegalSubjects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(120) NOT NULL,
    [Keywords] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LegalSubjects] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PrecedentIndexJobs] (
    [Id] int NOT NULL IDENTITY,
    [SourceType] int NOT NULL,
    [SourceId] int NOT NULL,
    [ContentHash] nvarchar(64) NOT NULL,
    [Title] nvarchar(240) NOT NULL,
    [SourceText] nvarchar(max) NOT NULL,
    [MatterType] nvarchar(120) NULL,
    [SuggestedSubjectId] int NULL,
    [IsArchived] bit NOT NULL,
    [IsPrivileged] bit NOT NULL,
    [IsConfidential] bit NOT NULL,
    [Status] int NOT NULL,
    [AttemptCount] int NOT NULL,
    [LastError] nvarchar(1000) NULL,
    [ExclusionReason] nvarchar(500) NULL,
    [QueuedAtUtc] datetime2 NOT NULL,
    [ProcessingStartedAtUtc] datetime2 NULL,
    [NextAttemptAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PrecedentIndexJobs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RetainerTemplates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Inclusions] nvarchar(max) NOT NULL,
    [Exclusions] nvarchar(max) NOT NULL,
    [BasePrice] decimal(18,2) NOT NULL,
    [PriceDisplay] nvarchar(100) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [IncludedHours] int NOT NULL,
    [OverageRate] decimal(18,2) NOT NULL,
    [BillingCycle] nvarchar(50) NOT NULL,
    [IsPublic] bit NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [EstimatedDuration] nvarchar(100) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [RequiresUpfrontPayment] bit NOT NULL,
    [UpfrontPercentage] int NULL,
    [AllowInstallments] bit NOT NULL,
    [MaxInstallments] int NULL,
    [TermsAndConditions] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_RetainerTemplates] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(200) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [EmailConfirmed] bit NOT NULL,
    [EmailConfirmationToken] nvarchar(max) NULL,
    [PasswordResetToken] nvarchar(max) NULL,
    [PasswordResetTokenExpiry] datetime2 NULL,
    [RememberMeToken] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CostEstimateEnquiries] (
    [Id] int NOT NULL IDENTITY,
    [PublicToken] nvarchar(450) NOT NULL,
    [ClientId] int NULL,
    [ContactName] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [MatterType] nvarchar(max) NOT NULL,
    [MatterValue] decimal(18,2) NOT NULL,
    [Urgency] int NOT NULL,
    [RequiresCourtProceedings] bit NOT NULL,
    [DocumentReadiness] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ConsultationRequestedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_CostEstimateEnquiries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CostEstimateEnquiries_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TrustAccounts] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [TotalDeposited] decimal(18,2) NOT NULL,
    [TotalWithdrawn] decimal(18,2) NOT NULL,
    [IsFrozen] bit NOT NULL,
    [IsClosed] bit NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_TrustAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrustAccounts_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CoverageCommissions] (
    [Id] int NOT NULL IDENTITY,
    [LegalSubjectId] int NOT NULL,
    [Status] int NOT NULL,
    [Brief] nvarchar(1000) NOT NULL,
    [CommissionedByUserId] int NOT NULL,
    [CommissionedAtUtc] datetime2 NOT NULL,
    [DueAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_CoverageCommissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CoverageCommissions_LegalSubjects_LegalSubjectId] FOREIGN KEY ([LegalSubjectId]) REFERENCES [LegalSubjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PrecedentItems] (
    [Id] int NOT NULL IDENTITY,
    [SourceType] int NOT NULL,
    [SourceId] int NOT NULL,
    [ContentHash] nvarchar(64) NOT NULL,
    [Title] nvarchar(240) NOT NULL,
    [SourceText] nvarchar(max) NOT NULL,
    [LegalSubjectId] int NOT NULL,
    [IsCurrent] bit NOT NULL,
    [SourceDateUtc] datetime2 NOT NULL,
    [IndexedAtUtc] datetime2 NOT NULL,
    [CuratorNote] nvarchar(1000) NULL,
    [RetiredAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PrecedentItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrecedentItems_LegalSubjects_LegalSubjectId] FOREIGN KEY ([LegalSubjectId]) REFERENCES [LegalSubjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClientRequests] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [TemplateId] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ClientNotes] nvarchar(max) NULL,
    [PreferredContact] nvarchar(50) NULL,
    [Urgency] nvarchar(50) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ReviewedDate] datetime2 NULL,
    [ConvertedToRetainerId] int NULL,
    [AdminNotes] nvarchar(max) NULL,
    CONSTRAINT [PK_ClientRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientRequests_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClientRequests_RetainerTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [RetainerTemplates] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Beneficiaries] (
    [Id] int NOT NULL IDENTITY,
    [BenefactorClientId] int NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [IdentificationNumber] nvarchar(100) NOT NULL,
    [DateOfBirth] datetime2 NULL,
    [RelationshipToBenefactor] nvarchar(100) NOT NULL,
    [AssetAccessTerms] nvarchar(2000) NOT NULL,
    [PermittedAssetPurposes] nvarchar(1000) NOT NULL,
    [EntitlementDescription] nvarchar(1000) NOT NULL,
    [EntitlementAmountLimit] decimal(18,2) NULL,
    [AccessEligibleFromUtc] datetime2 NULL,
    [AccessEligibleUntilUtc] datetime2 NULL,
    [Status] nvarchar(450) NOT NULL,
    [PortalAccessEnabled] bit NOT NULL,
    [PortalPasswordHash] nvarchar(500) NULL,
    [PortalPasswordSetAtUtc] datetime2 NULL,
    [BankAccountHolder] nvarchar(120) NULL,
    [BankName] nvarchar(100) NULL,
    [BankAccountNumber] nvarchar(40) NULL,
    [BankBranchCode] nvarchar(30) NULL,
    [BankDetailsConfirmedAtUtc] datetime2 NULL,
    [RejectionReason] nvarchar(1000) NULL,
    [ManualReviewReason] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [SubmittedAtUtc] datetime2 NULL,
    [ReviewedAtUtc] datetime2 NULL,
    [ReviewedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Beneficiaries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Beneficiaries_Clients_BenefactorClientId] FOREIGN KEY ([BenefactorClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Beneficiaries_Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Cases] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ClientId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CaseNumber] nvarchar(50) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CaseType] nvarchar(max) NOT NULL,
    [EvidenceStrength] decimal(6,5) NOT NULL,
    [RecordedOutcome] int NULL,
    [OutcomeSummary] nvarchar(max) NULL,
    [OutcomeIsPrivileged] bit NOT NULL,
    [OutcomeIsConfidential] bit NOT NULL,
    [MatterValue] decimal(18,2) NOT NULL,
    [CostRecoveryAwardedTotal] decimal(18,2) NOT NULL,
    [SettlementAmount] decimal(18,2) NULL,
    [IsCourtReady] bit NOT NULL,
    [CourtReadyAtUtc] datetime2 NULL,
    [StrategyReviewRequired] bit NOT NULL,
    [LawyerId] int NULL,
    CONSTRAINT [PK_Cases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Cases_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Cases_Users_LawyerId] FOREIGN KEY ([LawyerId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [ClientSupportSessions] (
    [Id] bigint NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [AuthorisedByStaffUserId] int NOT NULL,
    [SupportPersonName] nvarchar(160) NOT NULL,
    [Purpose] nvarchar(500) NOT NULL,
    [StartsAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_ClientSupportSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientSupportSessions_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClientSupportSessions_Users_AuthorisedByStaffUserId] FOREIGN KEY ([AuthorisedByStaffUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ForecastCalibrations] (
    [Id] int NOT NULL IDENTITY,
    [AttorneyId] int NULL,
    [ForecastCount] int NOT NULL,
    [MeanAccuracy] decimal(6,5) NOT NULL,
    [MeanBias] decimal(6,5) NOT NULL,
    [OptimisticForecastCount] int NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ForecastCalibrations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ForecastCalibrations_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LawyerProfiles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [BarNumber] nvarchar(50) NOT NULL,
    [HourlyRate] decimal(18,2) NOT NULL,
    [Bio] nvarchar(2000) NOT NULL,
    [OfficeLocation] nvarchar(200) NOT NULL,
    [IsActive] bit NOT NULL,
    [YearsOfExperience] int NOT NULL,
    CONSTRAINT [PK_LawyerProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LawyerProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SystemNotifications] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [ActionUrl] nvarchar(500) NULL,
    [IsRead] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ReadAtUtc] datetime2 NULL,
    [DeduplicationKey] nvarchar(450) NULL,
    CONSTRAINT [PK_SystemNotifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SystemNotifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [VulnerableClientFlags] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Safeguard] int NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    [LanguageRequired] nvarchar(120) NULL,
    [Status] int NOT NULL,
    [RaisedByAttorneyId] int NOT NULL,
    [RaisedAtUtc] datetime2 NOT NULL,
    [ReviewDueAtUtc] datetime2 NOT NULL,
    [NextReviewAtUtc] datetime2 NULL,
    [ReviewedByDirectorId] int NULL,
    [ReviewedAtUtc] datetime2 NULL,
    [ReviewNote] nvarchar(1000) NULL,
    [LastChangedAtUtc] datetime2 NOT NULL,
    [RemovedAtUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_VulnerableClientFlags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VulnerableClientFlags_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VulnerableClientFlags_Users_RaisedByAttorneyId] FOREIGN KEY ([RaisedByAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VulnerableClientFlags_Users_ReviewedByDirectorId] FOREIGN KEY ([ReviewedByDirectorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TrustTransactions] (
    [Id] int NOT NULL IDENTITY,
    [TrustAccountId] int NOT NULL,
    [Type] nvarchar(450) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Reference] nvarchar(100) NULL,
    [TransactionDate] datetime2 NOT NULL,
    [RelatedInvoiceId] int NULL,
    [AuthorizedBy] nvarchar(200) NULL,
    CONSTRAINT [PK_TrustTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrustTransactions_TrustAccounts_TrustAccountId] FOREIGN KEY ([TrustAccountId]) REFERENCES [TrustAccounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PrecedentConflictFlags] (
    [Id] int NOT NULL IDENTITY,
    [NewPrecedentItemId] int NOT NULL,
    [ExistingPrecedentItemId] int NOT NULL,
    [Similarity] decimal(6,5) NOT NULL,
    [Reason] nvarchar(600) NOT NULL,
    [Status] int NOT NULL,
    [ReviewedByUserId] int NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ReviewedAtUtc] datetime2 NULL,
    [ReviewNote] nvarchar(1000) NULL,
    CONSTRAINT [PK_PrecedentConflictFlags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrecedentConflictFlags_PrecedentItems_ExistingPrecedentItemId] FOREIGN KEY ([ExistingPrecedentItemId]) REFERENCES [PrecedentItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PrecedentConflictFlags_PrecedentItems_NewPrecedentItemId] FOREIGN KEY ([NewPrecedentItemId]) REFERENCES [PrecedentItems] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PrecedentPassages] (
    [Id] int NOT NULL IDENTITY,
    [PrecedentItemId] int NOT NULL,
    [PassageNumber] int NOT NULL,
    [Text] nvarchar(max) NOT NULL,
    [EmbeddingJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PrecedentPassages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrecedentPassages_PrecedentItems_PrecedentItemId] FOREIGN KEY ([PrecedentItemId]) REFERENCES [PrecedentItems] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BeneficiaryDocuments] (
    [Id] bigint NOT NULL IDENTITY,
    [BeneficiaryId] int NOT NULL,
    [RequirementId] int NOT NULL,
    [OriginalFileName] nvarchar(max) NOT NULL,
    [StoredFileName] nvarchar(max) NOT NULL,
    [RelativeStoragePath] nvarchar(max) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [Sha256Hash] nvarchar(max) NOT NULL,
    [PreScreenStatus] nvarchar(450) NOT NULL,
    [QualityScore] decimal(6,5) NULL,
    [OcrConfidence] decimal(6,5) NULL,
    [CertificationWordingDetected] bit NULL,
    [CertificationStampDetected] bit NULL,
    [SignatureDetected] bit NULL,
    [DetectedCertificationDate] datetime2 NULL,
    [DetectedExpiryDate] datetime2 NULL,
    [ExtractedDocumentType] nvarchar(max) NULL,
    [ReasonCode] nvarchar(max) NULL,
    [UserFacingReason] nvarchar(max) NULL,
    [TechnicalResultJson] nvarchar(max) NULL,
    [UploadedAtUtc] datetime2 NOT NULL,
    [AnalysedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_BeneficiaryDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BeneficiaryDocuments_Beneficiaries_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [Beneficiaries] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BeneficiaryDocuments_BeneficiaryDocumentRequirements_RequirementId] FOREIGN KEY ([RequirementId]) REFERENCES [BeneficiaryDocumentRequirements] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BeneficiaryInvitations] (
    [Id] bigint NOT NULL IDENTITY,
    [BeneficiaryId] int NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [UsedAtUtc] datetime2 NULL,
    [RevokedAtUtc] datetime2 NULL,
    [CreatedByUserId] int NOT NULL,
    CONSTRAINT [PK_BeneficiaryInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BeneficiaryInvitations_Beneficiaries_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [Beneficiaries] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BeneficiaryRequirementAssignments] (
    [BeneficiaryId] int NOT NULL,
    [RequirementId] int NOT NULL,
    [IsRequired] bit NOT NULL,
    CONSTRAINT [PK_BeneficiaryRequirementAssignments] PRIMARY KEY ([BeneficiaryId], [RequirementId]),
    CONSTRAINT [FK_BeneficiaryRequirementAssignments_Beneficiaries_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [Beneficiaries] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BeneficiaryRequirementAssignments_BeneficiaryDocumentRequirements_RequirementId] FOREIGN KEY ([RequirementId]) REFERENCES [BeneficiaryDocumentRequirements] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BeneficiaryTrustDisbursementRequests] (
    [Id] int NOT NULL IDENTITY,
    [ReferenceNumber] nvarchar(450) NOT NULL,
    [BeneficiaryId] int NOT NULL,
    [TrustAccountId] int NOT NULL,
    [Purpose] nvarchar(max) NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [EntitlementLimitSnapshot] decimal(18,2) NOT NULL,
    [BalanceSnapshot] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [SubmittedAtUtc] datetime2 NOT NULL,
    [DecidedByUserId] int NULL,
    [DecisionReason] nvarchar(max) NULL,
    [DecidedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_BeneficiaryTrustDisbursementRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BeneficiaryTrustDisbursementRequests_Beneficiaries_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [Beneficiaries] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BeneficiaryTrustDisbursementRequests_TrustAccounts_TrustAccountId] FOREIGN KEY ([TrustAccountId]) REFERENCES [TrustAccounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [FacialVerificationSessions] (
    [Id] uniqueidentifier NOT NULL,
    [BeneficiaryId] int NOT NULL,
    [ChallengeJson] nvarchar(max) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [ConsentGranted] bit NOT NULL,
    [ConsentGrantedAtUtc] datetime2 NULL,
    [ConsentNoticeVersion] nvarchar(max) NOT NULL,
    [LivenessPassed] bit NULL,
    [FaceMatched] bit NULL,
    [SimilarityScore] decimal(6,5) NULL,
    [ValidFrameRatio] decimal(6,5) NULL,
    [DuplicateFrameRatio] decimal(6,5) NULL,
    [ResultReasonCode] nvarchar(max) NULL,
    [ResultReason] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_FacialVerificationSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FacialVerificationSessions_Beneficiaries_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [Beneficiaries] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CaseAuthorityReliances] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [LegalAuthorityId] int NOT NULL,
    [AttorneyId] int NOT NULL,
    [RelevanceReason] nvarchar(max) NOT NULL,
    [AdverseTreatmentConfirmed] bit NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_CaseAuthorityReliances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseAuthorityReliances_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseAuthorityReliances_LegalAuthorities_LegalAuthorityId] FOREIGN KEY ([LegalAuthorityId]) REFERENCES [LegalAuthorities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseAuthorityReliances_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CaseDocumentRequests] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [ClientId] int NOT NULL,
    [RequestedByAttorneyId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Instructions] nvarchar(2000) NOT NULL,
    [RequiredDocuments] nvarchar(1000) NOT NULL,
    [DueAtUtc] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [ReviewFeedback] nvarchar(2000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [SubmittedAtUtc] datetime2 NULL,
    [ReviewedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_CaseDocumentRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseDocumentRequests_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseDocumentRequests_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseDocumentRequests_Users_RequestedByAttorneyId] FOREIGN KEY ([RequestedByAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CaseDocumentWaivers] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [RequirementId] int NOT NULL,
    [RequestedByAttorneyId] int NOT NULL,
    [Reason] nvarchar(3000) NOT NULL,
    [Status] int NOT NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [DirectorId] int NULL,
    [DirectorReason] nvarchar(3000) NULL,
    [DecidedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_CaseDocumentWaivers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseDocumentWaivers_CaseDocumentRequirements_RequirementId] FOREIGN KEY ([RequirementId]) REFERENCES [CaseDocumentRequirements] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseDocumentWaivers_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CaseForecasts] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [AttorneyId] int NOT NULL,
    [Status] int NOT NULL,
    [Probability] decimal(6,5) NULL,
    [ProbabilityBand] nvarchar(max) NOT NULL,
    [ConfidenceLevel] nvarchar(max) NOT NULL,
    [ComparableCount] int NOT NULL,
    [FactorsJson] nvarchar(max) NOT NULL,
    [ComparableCasesJson] nvarchar(max) NOT NULL,
    [RefusalReason] nvarchar(max) NULL,
    [AttorneyAssessment] decimal(6,5) NULL,
    [AttorneyAgrees] bit NULL,
    [AttorneyNotes] nvarchar(max) NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [LockedAtUtc] datetime2 NULL,
    [ActualOutcome] int NULL,
    [AccuracyScore] decimal(6,5) NULL,
    [ScoredAtUtc] datetime2 NULL,
    CONSTRAINT [PK_CaseForecasts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseForecasts_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseForecasts_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CaseNotes] (
    [Id] int NOT NULL IDENTITY,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsPrivileged] bit NOT NULL,
    [IsConfidential] bit NOT NULL,
    [CaseId] int NOT NULL,
    CONSTRAINT [PK_CaseNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseNotes_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CaseReassignments] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [OutgoingAttorneyId] int NOT NULL,
    [ReceivingAttorneyId] int NOT NULL,
    [ApprovedByUserId] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [ApprovedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_CaseReassignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseReassignments_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseReassignments_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseReassignments_Users_OutgoingAttorneyId] FOREIGN KEY ([OutgoingAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseReassignments_Users_ReceivingAttorneyId] FOREIGN KEY ([ReceivingAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClientCorrespondence] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [ReceivedAtUtc] datetime2 NOT NULL,
    [AnsweredAtUtc] datetime2 NULL,
    CONSTRAINT [PK_ClientCorrespondence] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientCorrespondence_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ExternalEvidenceRequests] (
    [Id] bigint NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [RecipientEmail] nvarchar(254) NOT NULL,
    [RecipientName] nvarchar(150) NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [AccessedAtUtc] datetime2 NULL,
    [ClosedAtUtc] datetime2 NULL,
    [RevokedAtUtc] datetime2 NULL,
    [RequestedByUserId] int NOT NULL,
    CONSTRAINT [PK_ExternalEvidenceRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExternalEvidenceRequests_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LegalCostRecoveryClaims] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [AttorneyId] int NOT NULL,
    [Ground] int NOT NULL,
    [Justification] nvarchar(4000) NOT NULL,
    [OpposingPartyName] nvarchar(200) NOT NULL,
    [OpposingPartyEmail] nvarchar(320) NOT NULL,
    [ClaimedAmount] decimal(18,2) NOT NULL,
    [AwardedAmount] decimal(18,2) NULL,
    [Status] int NOT NULL,
    [SubmittedAtUtc] datetime2 NOT NULL,
    [DecidedByUserId] int NULL,
    [DecisionNotes] nvarchar(4000) NULL,
    [DecidedAtUtc] datetime2 NULL,
    [ServedAtUtc] datetime2 NULL,
    [ServiceDeliveryReference] nvarchar(100) NULL,
    CONSTRAINT [PK_LegalCostRecoveryClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LegalCostRecoveryClaims_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LegalCostRecoveryClaims_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LegalCostRecoveryClaims_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LitigationStrategyDecisions] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [AttorneyId] int NOT NULL,
    [Strategy] int NOT NULL,
    [Reasoning] nvarchar(4000) NOT NULL,
    [LowProspectsJustification] nvarchar(4000) NULL,
    [CostsIncurredSnapshot] decimal(18,2) NOT NULL,
    [ProjectedCostToCompletion] decimal(18,2) NOT NULL,
    [MatterValueSnapshot] decimal(18,2) NOT NULL,
    [ProspectsSnapshot] decimal(6,5) NOT NULL,
    [ComparableSettlementLow] decimal(18,2) NULL,
    [ComparableSettlementHigh] decimal(18,2) NULL,
    [ComparableMatterCount] int NOT NULL,
    [ExpectedDurationDays] int NOT NULL,
    [CostAuthorisationRequired] bit NOT NULL,
    [ProspectsAuthorisationRequired] bit NOT NULL,
    [Status] int NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [ReviewDueAtUtc] datetime2 NULL,
    [DirectorId] int NULL,
    [DirectorReason] nvarchar(4000) NULL,
    [DirectorDecidedAtUtc] datetime2 NULL,
    [SupersededAtUtc] datetime2 NULL,
    CONSTRAINT [PK_LitigationStrategyDecisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LitigationStrategyDecisions_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LitigationStrategyDecisions_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LitigationStrategyDecisions_Users_DirectorId] FOREIGN KEY ([DirectorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MatterCostEstimates] (
    [Id] int NOT NULL IDENTITY,
    [CostEstimateEnquiryId] int NOT NULL,
    [Version] int NOT NULL,
    [Status] int NOT NULL,
    [ComparableMatterCount] int NOT NULL,
    [ProfessionalFeesLow] decimal(18,2) NOT NULL,
    [ProfessionalFeesHigh] decimal(18,2) NOT NULL,
    [DisbursementsLow] decimal(18,2) NOT NULL,
    [DisbursementsHigh] decimal(18,2) NOT NULL,
    [VatLow] decimal(18,2) NOT NULL,
    [VatHigh] decimal(18,2) NOT NULL,
    [TotalLow] decimal(18,2) NOT NULL,
    [TotalHigh] decimal(18,2) NOT NULL,
    [PermittedVarianceTolerance] decimal(6,5) NOT NULL,
    [RatesSnapshotJson] nvarchar(max) NOT NULL,
    [AssumptionsSnapshotJson] nvarchar(max) NOT NULL,
    [ComparableMattersSnapshotJson] nvarchar(max) NOT NULL,
    [DeclineReason] nvarchar(max) NULL,
    [LockedAtUtc] datetime2 NOT NULL,
    [LinkedCaseId] int NULL,
    [LinkedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_MatterCostEstimates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MatterCostEstimates_Cases_LinkedCaseId] FOREIGN KEY ([LinkedCaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MatterCostEstimates_CostEstimateEnquiries_CostEstimateEnquiryId] FOREIGN KEY ([CostEstimateEnquiryId]) REFERENCES [CostEstimateEnquiries] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MatterExpenseTerms] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [ExpenseType] int NOT NULL,
    [Classification] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_MatterExpenseTerms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MatterExpenseTerms_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ReimbursementClaims] (
    [Id] int NOT NULL IDENTITY,
    [ClaimNumber] nvarchar(450) NOT NULL,
    [CaseId] int NOT NULL,
    [AttorneyId] int NOT NULL,
    [ExpenseType] int NOT NULL,
    [ExpenseDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ApprovedAmount] decimal(18,2) NULL,
    [Description] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [MatchedActivityType] int NULL,
    [MatchedActivityId] int NULL,
    [ValidationFailureReason] nvarchar(max) NULL,
    [ProofOriginalFileName] nvarchar(max) NULL,
    [ProofRelativePath] nvarchar(max) NULL,
    [ProofContentType] nvarchar(max) NULL,
    [ProofSizeBytes] bigint NULL,
    [ProofSha256Hash] nvarchar(max) NULL,
    [PolicyLimitSnapshot] decimal(18,2) NOT NULL,
    [DelegatedLimitSnapshot] decimal(18,2) NOT NULL,
    [ExceedsPolicyLimit] bit NOT NULL,
    [Classification] int NOT NULL,
    [ClassificationReason] nvarchar(max) NOT NULL,
    [SubmittedAtUtc] datetime2 NOT NULL,
    [DecidedAtUtc] datetime2 NULL,
    [DecidedByUserId] int NULL,
    [DecisionReason] nvarchar(max) NULL,
    CONSTRAINT [PK_ReimbursementClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReimbursementClaims_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReimbursementClaims_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReimbursementClaims_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Retainers] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CaseId] int NULL,
    [TemplateId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [ScopeOfWork] nvarchar(max) NOT NULL,
    [SpecialTerms] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NULL,
    [Status] nvarchar(450) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [IncludedHours] int NOT NULL,
    [OverageRate] decimal(18,2) NOT NULL,
    [BillingCycle] nvarchar(50) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [LawyerNotes] nvarchar(max) NULL,
    [AdminNotes] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [SubmittedForApprovalDate] datetime2 NULL,
    [SubmittedByUserId] int NULL,
    [ApprovedDate] datetime2 NULL,
    [ApprovedByUserId] int NULL,
    [SentToClientDate] datetime2 NULL,
    [SignedDate] datetime2 NULL,
    [ClientSignatureName] nvarchar(200) NULL,
    [ClientIPAddress] nvarchar(50) NULL,
    [PaymentConfirmedDate] datetime2 NULL,
    [PaymentReference] nvarchar(100) NULL,
    [AmountPaid] decimal(18,2) NULL,
    [AvailableBalance] decimal(18,2) NOT NULL,
    [PdfPath] nvarchar(max) NULL,
    [SignatureToken] nvarchar(100) NULL,
    [SignatureTokenExpiry] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [RejectionReason] nvarchar(max) NULL,
    [Source] int NOT NULL,
    [AssignedLawyerId] int NULL,
    [RequiresUpfrontPayment] bit NOT NULL,
    [PaymentDueDays] int NOT NULL,
    [ActivatedDate] datetime2 NULL,
    [RevisionRequestedDate] datetime2 NULL,
    [RevisionRequestedByUserId] int NULL,
    [ClientChangeRequest] nvarchar(max) NULL,
    [ChangeRequestedDate] datetime2 NULL,
    [RejectedDate] datetime2 NULL,
    [RejectedByUserId] int NULL,
    [CancelledDate] datetime2 NULL,
    [CancellationReason] nvarchar(max) NULL,
    [CancelledByUserId] int NULL,
    CONSTRAINT [PK_Retainers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Retainers_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Retainers_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Retainers_RetainerTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [RetainerTemplates] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Retainers_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Retainers_Users_AssignedLawyerId] FOREIGN KEY ([AssignedLawyerId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Retainers_Users_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ServiceComplaints] (
    [Id] int NOT NULL IDENTITY,
    [ReferenceNumber] nvarchar(450) NOT NULL,
    [CaseId] int NOT NULL,
    [ClientId] int NOT NULL,
    [Category] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RoutedToUserId] int NOT NULL,
    [RestrictedUserIds] nvarchar(max) NOT NULL,
    [SubmittedAtUtc] datetime2 NOT NULL,
    [ResponseDueAtUtc] datetime2 NOT NULL,
    [AcknowledgedAtUtc] datetime2 NULL,
    [DuplicateWarningAcknowledged] bit NOT NULL,
    [DirectorFeedback] nvarchar(max) NULL,
    [RespondedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_ServiceComplaints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ServiceComplaints_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ServiceComplaints_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ServiceComplaints_Users_RoutedToUserId] FOREIGN KEY ([RoutedToUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LawyerProfileLawyerSpecialization] (
    [LawyerProfilesId] int NOT NULL,
    [SpecializationsId] int NOT NULL,
    CONSTRAINT [PK_LawyerProfileLawyerSpecialization] PRIMARY KEY ([LawyerProfilesId], [SpecializationsId]),
    CONSTRAINT [FK_LawyerProfileLawyerSpecialization_LawyerProfiles_LawyerProfilesId] FOREIGN KEY ([LawyerProfilesId]) REFERENCES [LawyerProfiles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LawyerProfileLawyerSpecialization_LawyerSpecializations_SpecializationsId] FOREIGN KEY ([SpecializationsId]) REFERENCES [LawyerSpecializations] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [VulnerableFlagAcknowledgements] (
    [Id] bigint NOT NULL IDENTITY,
    [VulnerableClientFlagId] int NOT NULL,
    [CaseId] int NOT NULL,
    [StaffUserId] int NOT NULL,
    [AcknowledgedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_VulnerableFlagAcknowledgements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VulnerableFlagAcknowledgements_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VulnerableFlagAcknowledgements_Users_StaffUserId] FOREIGN KEY ([StaffUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VulnerableFlagAcknowledgements_VulnerableClientFlags_VulnerableClientFlagId] FOREIGN KEY ([VulnerableClientFlagId]) REFERENCES [VulnerableClientFlags] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Documents] (
    [Id] int NOT NULL IDENTITY,
    [FileName] nvarchar(max) NOT NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [FileSize] nvarchar(max) NOT NULL,
    [FileType] nvarchar(max) NOT NULL,
    [CaseId] int NULL,
    [ClientId] int NULL,
    [UploadedById] int NULL,
    [Description] nvarchar(max) NOT NULL,
    [CaseDocumentRequestId] int NULL,
    [RequirementCode] nvarchar(80) NULL,
    [Category] int NOT NULL,
    [VersionNumber] int NULL,
    [ParentDocumentId] int NULL,
    [IsPublic] bit NOT NULL,
    [RequiresSignature] bit NOT NULL,
    [SignedAtUtc] datetime2 NULL,
    [UploadedAt] datetime2 NOT NULL,
    [LastModifiedAt] datetime2 NULL,
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Documents_CaseDocumentRequests_CaseDocumentRequestId] FOREIGN KEY ([CaseDocumentRequestId]) REFERENCES [CaseDocumentRequests] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Documents_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]),
    CONSTRAINT [FK_Documents_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]),
    CONSTRAINT [FK_Documents_Users_UploadedById] FOREIGN KEY ([UploadedById]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [ClientForecastRequests] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [ClientId] int NOT NULL,
    [Status] int NOT NULL,
    [ClientMessage] nvarchar(max) NOT NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [FulfilledByForecastId] int NULL,
    [FulfilledAtUtc] datetime2 NULL,
    CONSTRAINT [PK_ClientForecastRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientForecastRequests_CaseForecasts_FulfilledByForecastId] FOREIGN KEY ([FulfilledByForecastId]) REFERENCES [CaseForecasts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClientForecastRequests_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClientForecastRequests_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CaseHandovers] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [OutgoingAttorneyId] int NOT NULL,
    [ReceivingAttorneyId] int NOT NULL,
    [Status] int NOT NULL,
    [DueAtUtc] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [UrgentMatters] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ReadyAtUtc] datetime2 NULL,
    [AcceptedAtUtc] datetime2 NULL,
    [AcceptanceSignatureName] nvarchar(200) NULL,
    [AcceptanceRiskAcknowledged] bit NOT NULL,
    [ReceivingAttorneyQuery] nvarchar(2000) NULL,
    [DirectorQueryResponse] nvarchar(2000) NULL,
    [DirectorQueryRespondedByUserId] int NULL,
    [DirectorQueryRespondedAtUtc] datetime2 NULL,
    [DirectorQueryVerdictFinal] bit NOT NULL,
    [ReceivingAttorneyDeclineReason] nvarchar(2000) NULL,
    [ReceivingAttorneyRespondedAtUtc] datetime2 NULL,
    [CaseReassignmentId] int NOT NULL,
    CONSTRAINT [PK_CaseHandovers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseHandovers_CaseReassignments_CaseReassignmentId] FOREIGN KEY ([CaseReassignmentId]) REFERENCES [CaseReassignments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseHandovers_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseHandovers_Users_OutgoingAttorneyId] FOREIGN KEY ([OutgoingAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseHandovers_Users_ReceivingAttorneyId] FOREIGN KEY ([ReceivingAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ExternalEvidenceDocuments] (
    [Id] bigint NOT NULL IDENTITY,
    [RequestId] bigint NOT NULL,
    [OriginalFileName] nvarchar(200) NOT NULL,
    [Purpose] nvarchar(2500) NOT NULL,
    [RequirementCode] nvarchar(80) NULL,
    [RelativePath] nvarchar(500) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [Sha256Hash] nvarchar(max) NOT NULL,
    [UploadedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ExternalEvidenceDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExternalEvidenceDocuments_ExternalEvidenceRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ExternalEvidenceRequests] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LegalCostRecoveryAuditEntries] (
    [Id] bigint NOT NULL IDENTITY,
    [LegalCostRecoveryClaimId] int NOT NULL,
    [ActorUserId] int NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Details] nvarchar(2000) NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_LegalCostRecoveryAuditEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LegalCostRecoveryAuditEntries_LegalCostRecoveryClaims_LegalCostRecoveryClaimId] FOREIGN KEY ([LegalCostRecoveryClaimId]) REFERENCES [LegalCostRecoveryClaims] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AttorneyReimbursementPayables] (
    [Id] int NOT NULL IDENTITY,
    [ReimbursementClaimId] int NOT NULL,
    [AttorneyId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [IsPaid] bit NOT NULL,
    [PaidAtUtc] datetime2 NULL,
    [PaymentReference] nvarchar(max) NULL,
    CONSTRAINT [PK_AttorneyReimbursementPayables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttorneyReimbursementPayables_ReimbursementClaims_ReimbursementClaimId] FOREIGN KEY ([ReimbursementClaimId]) REFERENCES [ReimbursementClaims] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttorneyReimbursementPayables_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ReimbursementAuditEntries] (
    [Id] int NOT NULL IDENTITY,
    [ReimbursementClaimId] int NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [Details] nvarchar(max) NOT NULL,
    [ActorUserId] int NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ReimbursementAuditEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReimbursementAuditEntries_ReimbursementClaims_ReimbursementClaimId] FOREIGN KEY ([ReimbursementClaimId]) REFERENCES [ReimbursementClaims] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Invoices] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [RetainerId] int NULL,
    [CaseId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [IsPaid] bit NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [PaidDate] datetime2 NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [PdfPath] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Description] nvarchar(500) NULL,
    [InvoiceNumber] nvarchar(50) NOT NULL,
    [MatterCostEstimateId] int NULL,
    [RequiresEstimateAuthorisation] bit NOT NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoices_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Invoices_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Invoices_MatterCostEstimates_MatterCostEstimateId] FOREIGN KEY ([MatterCostEstimateId]) REFERENCES [MatterCostEstimates] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Invoices_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [RetainerActionLogs] (
    [Id] int NOT NULL IDENTITY,
    [RetainerId] int NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Details] nvarchar(1000) NULL,
    [UserId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RetainerActionLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetainerActionLogs_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RetainerActionLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RetainerPaymentSchedules] (
    [Id] int NOT NULL IDENTITY,
    [RetainerId] int NOT NULL,
    [InstallmentNumber] int NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [AmountDue] decimal(18,2) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [PaidDate] datetime2 NULL,
    [PaymentReference] nvarchar(100) NULL,
    CONSTRAINT [PK_RetainerPaymentSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetainerPaymentSchedules_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RetainerRenewals] (
    [Id] int NOT NULL IDENTITY,
    [RetainerId] int NOT NULL,
    [PreviousEndDate] datetime2 NULL,
    [NewEndDate] datetime2 NOT NULL,
    [RenewedDate] datetime2 NOT NULL,
    [RenewedByUserId] int NOT NULL,
    [AmountAdjustment] decimal(18,2) NOT NULL,
    [Notes] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_RetainerRenewals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetainerRenewals_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RetainerRenewals_Users_RenewedByUserId] FOREIGN KEY ([RenewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ComplaintAttachments] (
    [Id] int NOT NULL IDENTITY,
    [ServiceComplaintId] int NOT NULL,
    [OriginalFileName] nvarchar(max) NOT NULL,
    [RelativePath] nvarchar(max) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [Sha256Hash] nvarchar(64) NOT NULL,
    [UploadedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ComplaintAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComplaintAttachments_ServiceComplaints_ServiceComplaintId] FOREIGN KEY ([ServiceComplaintId]) REFERENCES [ServiceComplaints] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StaffServiceRecordEntries] (
    [Id] int NOT NULL IDENTITY,
    [StaffUserId] int NOT NULL,
    [ServiceComplaintId] int NOT NULL,
    [CaseId] int NOT NULL,
    [Category] int NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_StaffServiceRecordEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StaffServiceRecordEntries_ServiceComplaints_ServiceComplaintId] FOREIGN KEY ([ServiceComplaintId]) REFERENCES [ServiceComplaints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StaffServiceRecordEntries_Users_StaffUserId] FOREIGN KEY ([StaffUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DocumentShares] (
    [Id] int NOT NULL IDENTITY,
    [DocumentId] int NOT NULL,
    [SharedWithUserId] int NOT NULL,
    [SharedByUserId] int NOT NULL,
    [SharedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [AccessToken] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DocumentShares_Users_SharedByUserId] FOREIGN KEY ([SharedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DocumentShares_Users_SharedWithUserId] FOREIGN KEY ([SharedWithUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [CaseHandoverRequests] (
    [Id] int NOT NULL IDENTITY,
    [CaseId] int NOT NULL,
    [RequestedByAttorneyId] int NOT NULL,
    [Reason] nvarchar(2000) NOT NULL,
    [DirectorResponse] nvarchar(2000) NULL,
    [Status] int NOT NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [ReviewedAtUtc] datetime2 NULL,
    [ReviewedByDirectorId] int NULL,
    [CaseHandoverId] int NULL,
    CONSTRAINT [PK_CaseHandoverRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CaseHandoverRequests_CaseHandovers_CaseHandoverId] FOREIGN KEY ([CaseHandoverId]) REFERENCES [CaseHandovers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CaseHandoverRequests_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CaseHandoverRequests_Users_RequestedByAttorneyId] FOREIGN KEY ([RequestedByAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CaseHandoverRequests_Users_ReviewedByDirectorId] FOREIGN KEY ([ReviewedByDirectorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [HandoverItems] (
    [Id] int NOT NULL IDENTITY,
    [CaseHandoverId] int NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsMandatory] bit NOT NULL,
    [IsResolved] bit NOT NULL,
    [ResolutionNote] nvarchar(max) NULL,
    [SourceFingerprint] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_HandoverItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HandoverItems_CaseHandovers_CaseHandoverId] FOREIGN KEY ([CaseHandoverId]) REFERENCES [CaseHandovers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CalendarEvents] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [StartDateTime] datetime2 NOT NULL,
    [EndDateTime] datetime2 NOT NULL,
    [IsAllDay] bit NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [Color] nvarchar(20) NOT NULL,
    [CreatedByUserId] int NULL,
    [AssignedToUserId] int NULL,
    [CaseId] int NULL,
    [RetainerId] int NULL,
    [ClientId] int NULL,
    [AppointmentFee] decimal(18,2) NULL,
    [PaymentDueDays] int NOT NULL,
    [LatePenaltyType] nvarchar(max) NOT NULL,
    [LatePenaltyValue] decimal(18,2) NOT NULL,
    [LatePenaltyGraceDays] int NOT NULL,
    [ClientResponseStatus] nvarchar(max) NOT NULL,
    [LawyerApprovalStatus] int NOT NULL,
    [ClientRespondedAtUtc] datetime2 NULL,
    [ClientResponseComments] nvarchar(max) NULL,
    [BillingProcessed] bit NOT NULL,
    [BillingProcessedAtUtc] datetime2 NULL,
    [GeneratedInvoiceId] int NULL,
    [RowVersion] rowversion NOT NULL,
    [ParentEventId] int NULL,
    [RecurrenceRule] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [ReminderSentAt] datetime2 NULL,
    [ActualStartTime] datetime2 NULL,
    [ActualEndTime] datetime2 NULL,
    [MeetingLink] nvarchar(500) NOT NULL,
    [IsPrivate] bit NOT NULL,
    [CompletionNotes] nvarchar(1000) NOT NULL,
    CONSTRAINT [PK_CalendarEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CalendarEvents_CalendarEvents_ParentEventId] FOREIGN KEY ([ParentEventId]) REFERENCES [CalendarEvents] ([Id]),
    CONSTRAINT [FK_CalendarEvents_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CalendarEvents_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CalendarEvents_Invoices_GeneratedInvoiceId] FOREIGN KEY ([GeneratedInvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CalendarEvents_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]),
    CONSTRAINT [FK_CalendarEvents_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_CalendarEvents_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoiceEstimateAuthorisations] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [MatterCostEstimateId] int NOT NULL,
    [InvoiceTotal] decimal(18,2) NOT NULL,
    [VariancePercent] decimal(8,5) NOT NULL,
    [IsApproved] bit NOT NULL,
    [ApprovedByUserId] int NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [ApprovedAtUtc] datetime2 NULL,
    [ApprovalReason] nvarchar(max) NULL,
    CONSTRAINT [PK_InvoiceEstimateAuthorisations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceEstimateAuthorisations_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InvoiceEstimateAuthorisations_MatterCostEstimates_MatterCostEstimateId] FOREIGN KEY ([MatterCostEstimateId]) REFERENCES [MatterCostEstimates] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InvoiceEstimateAuthorisations_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [InvoicePenalties] (
    [Id] bigint NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [CalendarEventId] int NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [BasisAmount] decimal(18,2) NOT NULL,
    [PenaltyValue] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [AppliedAtUtc] datetime2 NOT NULL,
    [IdempotencyKey] nvarchar(450) NOT NULL,
    [AppliedByAccountantId] int NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    CONSTRAINT [PK_InvoicePenalties] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoicePenalties_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InvoicePenalties_Users_AppliedByAccountantId] FOREIGN KEY ([AppliedByAccountantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MatterDisbursements] (
    [Id] int NOT NULL IDENTITY,
    [ReimbursementClaimId] int NOT NULL,
    [CaseId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IncurredDate] datetime2 NOT NULL,
    [InvoiceId] int NULL,
    [InvoicedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_MatterDisbursements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MatterDisbursements_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MatterDisbursements_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MatterDisbursements_ReimbursementClaims_ReimbursementClaimId] FOREIGN KEY ([ReimbursementClaimId]) REFERENCES [ReimbursementClaims] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [ClientId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [TransactionReference] nvarchar(100) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsTrustAccountDeposit] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TimeEntries] (
    [Id] int NOT NULL IDENTITY,
    [LawyerId] int NOT NULL,
    [CaseId] int NOT NULL,
    [RetainerId] int NULL,
    [InvoiceId] int NULL,
    [Description] nvarchar(500) NOT NULL,
    [Date] datetime2 NOT NULL,
    [Hours] decimal(18,2) NOT NULL,
    [HourlyRate] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [IsBillable] bit NOT NULL,
    [IsBilled] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TimeEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TimeEntries_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TimeEntries_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TimeEntries_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TimeEntries_Users_LawyerId] FOREIGN KEY ([LawyerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RetainerPayments] (
    [Id] int NOT NULL IDENTITY,
    [RetainerId] int NOT NULL,
    [PaymentScheduleId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [TransactionReference] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [IsDepositedToTrust] bit NOT NULL,
    CONSTRAINT [PK_RetainerPayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetainerPayments_RetainerPaymentSchedules_PaymentScheduleId] FOREIGN KEY ([PaymentScheduleId]) REFERENCES [RetainerPaymentSchedules] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RetainerPayments_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AppointmentBillingRecords] (
    [Id] bigint NOT NULL IDENTITY,
    [CalendarEventId] int NOT NULL,
    [IdempotencyKey] nvarchar(450) NOT NULL,
    [AppointmentFee] decimal(18,2) NOT NULL,
    [CoveredAmount] decimal(18,2) NOT NULL,
    [InvoicedAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [TrustTransactionId] int NULL,
    [InvoiceId] int NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [FailureReason] nvarchar(max) NULL,
    CONSTRAINT [PK_AppointmentBillingRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppointmentBillingRecords_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AppointmentInterpreterAssignments] (
    [Id] int NOT NULL IDENTITY,
    [CalendarEventId] int NOT NULL,
    [InterpreterName] nvarchar(160) NOT NULL,
    [Language] nvarchar(120) NOT NULL,
    [ContactDetails] nvarchar(200) NULL,
    [AssignedByUserId] int NOT NULL,
    [AssignedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AppointmentInterpreterAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppointmentInterpreterAssignments_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppointmentInterpreterAssignments_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AppointmentInvitations] (
    [Id] bigint NOT NULL IDENTITY,
    [CalendarEventId] int NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [UsedAtUtc] datetime2 NULL,
    [RevokedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_AppointmentInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppointmentInvitations_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AppointmentSupportPersonAssignments] (
    [Id] int NOT NULL IDENTITY,
    [CalendarEventId] int NOT NULL,
    [SupportPersonName] nvarchar(160) NOT NULL,
    [Relationship] nvarchar(120) NULL,
    [RecordedByUserId] int NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AppointmentSupportPersonAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppointmentSupportPersonAssignments_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppointmentSupportPersonAssignments_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AttorneyWhereabouts] (
    [Id] int NOT NULL IDENTITY,
    [AttorneyId] int NOT NULL,
    [CalendarEventId] int NULL,
    [Venue] nvarchar(max) NOT NULL,
    [CheckedInAtUtc] datetime2 NOT NULL,
    [ExpectedReturnAtUtc] datetime2 NOT NULL,
    [CheckedOutAtUtc] datetime2 NULL,
    [Status] int NOT NULL,
    [AlertedAtUtc] datetime2 NULL,
    [DirectorEscalatedAtUtc] datetime2 NULL,
    [ContactOutcome] nvarchar(max) NULL,
    CONSTRAINT [PK_AttorneyWhereabouts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttorneyWhereabouts_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]),
    CONSTRAINT [FK_AttorneyWhereabouts_Users_AttorneyId] FOREIGN KEY ([AttorneyId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [EventAttendees] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [UserId] int NOT NULL,
    [Status] int NOT NULL,
    [RespondedAt] datetime2 NULL,
    [ResponseStatus] nvarchar(50) NOT NULL,
    [ResponseDate] datetime2 NULL,
    [Comments] nvarchar(500) NOT NULL,
    [IsOptional] bit NOT NULL,
    [InvitedAt] datetime2 NOT NULL,
    [ReminderSent] bit NOT NULL,
    CONSTRAINT [PK_EventAttendees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventAttendees_CalendarEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EventAttendees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [EventReminders] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [ReminderMinutesBefore] int NOT NULL,
    [MinutesBefore] int NOT NULL,
    [Method] int NOT NULL,
    [IsSent] bit NOT NULL,
    [SentAt] datetime2 NULL,
    [SentTo] nvarchar(500) NOT NULL,
    [ReminderType] nvarchar(50) NOT NULL,
    [ErrorMessage] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_EventReminders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventReminders_CalendarEvents_EventId] FOREIGN KEY ([EventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Tasks] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Priority] nvarchar(450) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [DueDate] datetime2 NULL,
    [AssignedToId] int NULL,
    [CompletedAt] datetime2 NULL,
    [CreatedById] int NULL,
    [CaseId] int NULL,
    [RetainerId] int NULL,
    [ParentTaskId] int NULL,
    [RelatedEventId] int NULL,
    [EstimatedHours] decimal(10,2) NULL,
    [ActualHours] decimal(10,2) NULL,
    [IsBillable] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tasks_CalendarEvents_RelatedEventId] FOREIGN KEY ([RelatedEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tasks_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tasks_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]),
    CONSTRAINT [FK_Tasks_Tasks_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [Tasks] ([Id]),
    CONSTRAINT [FK_Tasks_Users_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tasks_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LegalCostRecoveryTimeEntries] (
    [LegalCostRecoveryClaimId] int NOT NULL,
    [TimeEntryId] int NOT NULL,
    [AmountSnapshot] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_LegalCostRecoveryTimeEntries] PRIMARY KEY ([LegalCostRecoveryClaimId], [TimeEntryId]),
    CONSTRAINT [FK_LegalCostRecoveryTimeEntries_LegalCostRecoveryClaims_LegalCostRecoveryClaimId] FOREIGN KEY ([LegalCostRecoveryClaimId]) REFERENCES [LegalCostRecoveryClaims] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LegalCostRecoveryTimeEntries_TimeEntries_TimeEntryId] FOREIGN KEY ([TimeEntryId]) REFERENCES [TimeEntries] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TaskAttachments] (
    [Id] int NOT NULL IDENTITY,
    [TaskId] int NOT NULL,
    [FileName] nvarchar(200) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskAttachments_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TaskComments] (
    [Id] int NOT NULL IDENTITY,
    [TaskId] int NOT NULL,
    [UserId] int NOT NULL,
    [Comment] nvarchar(1000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsInternal] bit NOT NULL,
    CONSTRAINT [PK_TaskComments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskComments_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskComments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultClassification', N'DelegatedApprovalLimit', N'ExpenseType', N'IsActive', N'PerItemLimit') AND [object_id] = OBJECT_ID(N'[ExpensePolicies]'))
    SET IDENTITY_INSERT [ExpensePolicies] ON;
INSERT INTO [ExpensePolicies] ([Id], [DefaultClassification], [DelegatedApprovalLimit], [ExpenseType], [IsActive], [PerItemLimit])
VALUES (1, 0, 1500.0, 0, CAST(1 AS bit), 2500.0),
(2, 0, 5000.0, 1, CAST(1 AS bit), 15000.0),
(3, 0, 5000.0, 2, CAST(1 AS bit), 10000.0),
(4, 0, 1000.0, 3, CAST(1 AS bit), 1500.0),
(5, 0, 2500.0, 4, CAST(1 AS bit), 3500.0),
(6, 1, 500.0, 5, CAST(1 AS bit), 600.0),
(7, 0, 500.0, 6, CAST(1 AS bit), 500.0),
(8, 1, 1000.0, 7, CAST(1 AS bit), 2000.0),
(9, 1, 500.0, 8, CAST(1 AS bit), 1000.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultClassification', N'DelegatedApprovalLimit', N'ExpenseType', N'IsActive', N'PerItemLimit') AND [object_id] = OBJECT_ID(N'[ExpensePolicies]'))
    SET IDENTITY_INSERT [ExpensePolicies] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Keywords', N'Name') AND [object_id] = OBJECT_ID(N'[LegalSubjects]'))
    SET IDENTITY_INSERT [LegalSubjects] ON;
INSERT INTO [LegalSubjects] ([Id], [IsActive], [Keywords], [Name])
VALUES (1, CAST(1 AS bit), N'litigation,court,trial,appeal,interdict,damages', N'Civil Litigation'),
(2, CAST(1 AS bit), N'commercial,contract,company,business,shareholder', N'Commercial Law'),
(3, CAST(1 AS bit), N'family,divorce,custody,maintenance,matrimonial', N'Family Law'),
(4, CAST(1 AS bit), N'labour,employment,employee,ccma,dismissal', N'Labour Law'),
(5, CAST(1 AS bit), N'criminal,bail,prosecution,sentence,accused', N'Criminal Law'),
(6, CAST(1 AS bit), N'property,transfer,lease,eviction,conveyancing', N'Property Law'),
(7, CAST(1 AS bit), N'estate,will,trust,beneficiary,executor', N'Estates and Trusts'),
(8, CAST(1 AS bit), N'injury,accident,raf,medical negligence,compensation', N'Personal Injury'),
(9, CAST(1 AS bit), N'general,advice,procedure', N'General Practice');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Keywords', N'Name') AND [object_id] = OBJECT_ID(N'[LegalSubjects]'))
    SET IDENTITY_INSERT [LegalSubjects] OFF;
GO


CREATE UNIQUE INDEX [IX_AppointmentBillingRecords_CalendarEventId] ON [AppointmentBillingRecords] ([CalendarEventId]);
GO


CREATE UNIQUE INDEX [IX_AppointmentBillingRecords_IdempotencyKey] ON [AppointmentBillingRecords] ([IdempotencyKey]);
GO


CREATE INDEX [IX_AppointmentInterpreterAssignments_AssignedByUserId] ON [AppointmentInterpreterAssignments] ([AssignedByUserId]);
GO


CREATE UNIQUE INDEX [IX_AppointmentInterpreterAssignments_CalendarEventId] ON [AppointmentInterpreterAssignments] ([CalendarEventId]);
GO


CREATE INDEX [IX_AppointmentInvitations_CalendarEventId] ON [AppointmentInvitations] ([CalendarEventId]);
GO


CREATE INDEX [IX_AppointmentInvitations_ExpiresAtUtc] ON [AppointmentInvitations] ([ExpiresAtUtc]);
GO


CREATE UNIQUE INDEX [IX_AppointmentInvitations_TokenHash] ON [AppointmentInvitations] ([TokenHash]);
GO


CREATE UNIQUE INDEX [IX_AppointmentSupportPersonAssignments_CalendarEventId] ON [AppointmentSupportPersonAssignments] ([CalendarEventId]);
GO


CREATE INDEX [IX_AppointmentSupportPersonAssignments_RecordedByUserId] ON [AppointmentSupportPersonAssignments] ([RecordedByUserId]);
GO


CREATE INDEX [IX_AttorneyReimbursementPayables_AttorneyId] ON [AttorneyReimbursementPayables] ([AttorneyId]);
GO


CREATE UNIQUE INDEX [IX_AttorneyReimbursementPayables_ReimbursementClaimId] ON [AttorneyReimbursementPayables] ([ReimbursementClaimId]);
GO


CREATE INDEX [IX_AttorneyWhereabouts_AttorneyId] ON [AttorneyWhereabouts] ([AttorneyId]);
GO


CREATE INDEX [IX_AttorneyWhereabouts_CalendarEventId] ON [AttorneyWhereabouts] ([CalendarEventId]);
GO


CREATE INDEX [IX_AttorneyWhereabouts_Status_ExpectedReturnAtUtc] ON [AttorneyWhereabouts] ([Status], [ExpectedReturnAtUtc]);
GO


CREATE INDEX [IX_AuditEntries_CreatedAtUtc] ON [AuditEntries] ([CreatedAtUtc]);
GO


CREATE INDEX [IX_AuditEntries_EntityType_EntityId] ON [AuditEntries] ([EntityType], [EntityId]);
GO


CREATE INDEX [IX_Beneficiaries_BenefactorClientId] ON [Beneficiaries] ([BenefactorClientId]);
GO


CREATE INDEX [IX_Beneficiaries_Email] ON [Beneficiaries] ([Email]);
GO


CREATE INDEX [IX_Beneficiaries_ReviewedByUserId] ON [Beneficiaries] ([ReviewedByUserId]);
GO


CREATE INDEX [IX_Beneficiaries_Status] ON [Beneficiaries] ([Status]);
GO


CREATE UNIQUE INDEX [IX_BeneficiaryDocumentRequirements_Code] ON [BeneficiaryDocumentRequirements] ([Code]);
GO


CREATE INDEX [IX_BeneficiaryDocuments_BeneficiaryId] ON [BeneficiaryDocuments] ([BeneficiaryId]);
GO


CREATE INDEX [IX_BeneficiaryDocuments_PreScreenStatus] ON [BeneficiaryDocuments] ([PreScreenStatus]);
GO


CREATE INDEX [IX_BeneficiaryDocuments_RequirementId] ON [BeneficiaryDocuments] ([RequirementId]);
GO


CREATE INDEX [IX_BeneficiaryInvitations_BeneficiaryId] ON [BeneficiaryInvitations] ([BeneficiaryId]);
GO


CREATE INDEX [IX_BeneficiaryInvitations_ExpiresAtUtc] ON [BeneficiaryInvitations] ([ExpiresAtUtc]);
GO


CREATE UNIQUE INDEX [IX_BeneficiaryInvitations_TokenHash] ON [BeneficiaryInvitations] ([TokenHash]);
GO


CREATE INDEX [IX_BeneficiaryRequirementAssignments_RequirementId] ON [BeneficiaryRequirementAssignments] ([RequirementId]);
GO


CREATE INDEX [IX_BeneficiaryTrustDisbursementRequests_BeneficiaryId] ON [BeneficiaryTrustDisbursementRequests] ([BeneficiaryId]);
GO


CREATE UNIQUE INDEX [IX_BeneficiaryTrustDisbursementRequests_ReferenceNumber] ON [BeneficiaryTrustDisbursementRequests] ([ReferenceNumber]);
GO


CREATE INDEX [IX_BeneficiaryTrustDisbursementRequests_TrustAccountId] ON [BeneficiaryTrustDisbursementRequests] ([TrustAccountId]);
GO


CREATE INDEX [IX_CalendarEvents_AssignedToUserId] ON [CalendarEvents] ([AssignedToUserId]);
GO


CREATE INDEX [IX_CalendarEvents_CaseId] ON [CalendarEvents] ([CaseId]);
GO


CREATE INDEX [IX_CalendarEvents_ClientId] ON [CalendarEvents] ([ClientId]);
GO


CREATE INDEX [IX_CalendarEvents_CreatedByUserId] ON [CalendarEvents] ([CreatedByUserId]);
GO


CREATE INDEX [IX_CalendarEvents_GeneratedInvoiceId] ON [CalendarEvents] ([GeneratedInvoiceId]);
GO


CREATE INDEX [IX_CalendarEvents_ParentEventId] ON [CalendarEvents] ([ParentEventId]);
GO


CREATE INDEX [IX_CalendarEvents_RetainerId] ON [CalendarEvents] ([RetainerId]);
GO


CREATE INDEX [IX_CalendarEvents_StartDateTime] ON [CalendarEvents] ([StartDateTime]);
GO


CREATE INDEX [IX_CalendarEvents_Status] ON [CalendarEvents] ([Status]);
GO


CREATE INDEX [IX_CaseAuthorityReliances_AttorneyId] ON [CaseAuthorityReliances] ([AttorneyId]);
GO


CREATE INDEX [IX_CaseAuthorityReliances_CaseId_LegalAuthorityId_AttorneyId] ON [CaseAuthorityReliances] ([CaseId], [LegalAuthorityId], [AttorneyId]);
GO


CREATE INDEX [IX_CaseAuthorityReliances_LegalAuthorityId] ON [CaseAuthorityReliances] ([LegalAuthorityId]);
GO


CREATE INDEX [IX_CaseDocumentRequests_CaseId_Status] ON [CaseDocumentRequests] ([CaseId], [Status]);
GO


CREATE INDEX [IX_CaseDocumentRequests_ClientId] ON [CaseDocumentRequests] ([ClientId]);
GO


CREATE INDEX [IX_CaseDocumentRequests_RequestedByAttorneyId] ON [CaseDocumentRequests] ([RequestedByAttorneyId]);
GO


CREATE UNIQUE INDEX [IX_CaseDocumentRequirements_CaseType_Code] ON [CaseDocumentRequirements] ([CaseType], [Code]);
GO


CREATE INDEX [IX_CaseDocumentWaivers_CaseId_RequirementId_Status] ON [CaseDocumentWaivers] ([CaseId], [RequirementId], [Status]);
GO


CREATE INDEX [IX_CaseDocumentWaivers_RequirementId] ON [CaseDocumentWaivers] ([RequirementId]);
GO


CREATE INDEX [IX_CaseForecasts_AttorneyId] ON [CaseForecasts] ([AttorneyId]);
GO


CREATE INDEX [IX_CaseForecasts_CaseId] ON [CaseForecasts] ([CaseId]);
GO


CREATE INDEX [IX_CaseHandoverRequests_CaseHandoverId] ON [CaseHandoverRequests] ([CaseHandoverId]);
GO


CREATE INDEX [IX_CaseHandoverRequests_CaseId] ON [CaseHandoverRequests] ([CaseId]);
GO


CREATE INDEX [IX_CaseHandoverRequests_RequestedByAttorneyId] ON [CaseHandoverRequests] ([RequestedByAttorneyId]);
GO


CREATE INDEX [IX_CaseHandoverRequests_ReviewedByDirectorId] ON [CaseHandoverRequests] ([ReviewedByDirectorId]);
GO


CREATE INDEX [IX_CaseHandoverRequests_Status_RequestedAtUtc] ON [CaseHandoverRequests] ([Status], [RequestedAtUtc]);
GO


CREATE INDEX [IX_CaseHandovers_CaseId_Status] ON [CaseHandovers] ([CaseId], [Status]);
GO


CREATE INDEX [IX_CaseHandovers_CaseReassignmentId] ON [CaseHandovers] ([CaseReassignmentId]);
GO


CREATE INDEX [IX_CaseHandovers_OutgoingAttorneyId] ON [CaseHandovers] ([OutgoingAttorneyId]);
GO


CREATE INDEX [IX_CaseHandovers_ReceivingAttorneyId] ON [CaseHandovers] ([ReceivingAttorneyId]);
GO


CREATE INDEX [IX_CaseNotes_CaseId] ON [CaseNotes] ([CaseId]);
GO


CREATE INDEX [IX_CaseReadinessReviews_CaseId_ReviewedAtUtc] ON [CaseReadinessReviews] ([CaseId], [ReviewedAtUtc]);
GO


CREATE INDEX [IX_CaseReassignments_ApprovedByUserId] ON [CaseReassignments] ([ApprovedByUserId]);
GO


CREATE INDEX [IX_CaseReassignments_CaseId_Status] ON [CaseReassignments] ([CaseId], [Status]);
GO


CREATE INDEX [IX_CaseReassignments_OutgoingAttorneyId] ON [CaseReassignments] ([OutgoingAttorneyId]);
GO


CREATE INDEX [IX_CaseReassignments_ReceivingAttorneyId] ON [CaseReassignments] ([ReceivingAttorneyId]);
GO


CREATE UNIQUE INDEX [IX_Cases_CaseNumber] ON [Cases] ([CaseNumber]);
GO


CREATE INDEX [IX_Cases_ClientId] ON [Cases] ([ClientId]);
GO


CREATE INDEX [IX_Cases_LawyerId] ON [Cases] ([LawyerId]);
GO


CREATE INDEX [IX_ClientCorrespondence_CaseId_AnsweredAtUtc] ON [ClientCorrespondence] ([CaseId], [AnsweredAtUtc]);
GO


CREATE INDEX [IX_ClientForecastRequests_CaseId_Status] ON [ClientForecastRequests] ([CaseId], [Status]);
GO


CREATE INDEX [IX_ClientForecastRequests_ClientId] ON [ClientForecastRequests] ([ClientId]);
GO


CREATE INDEX [IX_ClientForecastRequests_FulfilledByForecastId] ON [ClientForecastRequests] ([FulfilledByForecastId]);
GO


CREATE INDEX [IX_ClientRequests_ClientId] ON [ClientRequests] ([ClientId]);
GO


CREATE INDEX [IX_ClientRequests_CreatedDate] ON [ClientRequests] ([CreatedDate]);
GO


CREATE INDEX [IX_ClientRequests_Status] ON [ClientRequests] ([Status]);
GO


CREATE INDEX [IX_ClientRequests_TemplateId] ON [ClientRequests] ([TemplateId]);
GO


CREATE INDEX [IX_Clients_Email] ON [Clients] ([Email]);
GO


CREATE INDEX [IX_ClientSupportSessions_AuthorisedByStaffUserId] ON [ClientSupportSessions] ([AuthorisedByStaffUserId]);
GO


CREATE INDEX [IX_ClientSupportSessions_ClientId_ExpiresAtUtc] ON [ClientSupportSessions] ([ClientId], [ExpiresAtUtc]);
GO


CREATE INDEX [IX_ComplaintAttachments_ServiceComplaintId] ON [ComplaintAttachments] ([ServiceComplaintId]);
GO


CREATE INDEX [IX_CostEstimateCoverageGaps_Status_RecordedAtUtc] ON [CostEstimateCoverageGaps] ([Status], [RecordedAtUtc]);
GO


CREATE INDEX [IX_CostEstimateEnquiries_ClientId] ON [CostEstimateEnquiries] ([ClientId]);
GO


CREATE INDEX [IX_CostEstimateEnquiries_Email_CreatedAtUtc] ON [CostEstimateEnquiries] ([Email], [CreatedAtUtc]);
GO


CREATE UNIQUE INDEX [IX_CostEstimateEnquiries_PublicToken] ON [CostEstimateEnquiries] ([PublicToken]);
GO


CREATE INDEX [IX_CoverageCommissions_LegalSubjectId_Status] ON [CoverageCommissions] ([LegalSubjectId], [Status]);
GO


CREATE INDEX [IX_Documents_CaseDocumentRequestId] ON [Documents] ([CaseDocumentRequestId]);
GO


CREATE INDEX [IX_Documents_CaseId] ON [Documents] ([CaseId]);
GO


CREATE INDEX [IX_Documents_ClientId] ON [Documents] ([ClientId]);
GO


CREATE INDEX [IX_Documents_UploadedById] ON [Documents] ([UploadedById]);
GO


CREATE INDEX [IX_DocumentShares_DocumentId] ON [DocumentShares] ([DocumentId]);
GO


CREATE INDEX [IX_DocumentShares_SharedByUserId] ON [DocumentShares] ([SharedByUserId]);
GO


CREATE INDEX [IX_DocumentShares_SharedWithUserId] ON [DocumentShares] ([SharedWithUserId]);
GO


CREATE UNIQUE INDEX [IX_EmailOutboxMessages_DeduplicationKey] ON [EmailOutboxMessages] ([DeduplicationKey]);
GO


CREATE INDEX [IX_EmailOutboxMessages_Status_NextAttemptAtUtc] ON [EmailOutboxMessages] ([Status], [NextAttemptAtUtc]);
GO


CREATE INDEX [IX_EventAttendees_EventId] ON [EventAttendees] ([EventId]);
GO


CREATE INDEX [IX_EventAttendees_UserId] ON [EventAttendees] ([UserId]);
GO


CREATE INDEX [IX_EventReminders_EventId] ON [EventReminders] ([EventId]);
GO


CREATE INDEX [IX_EventReminders_IsSent] ON [EventReminders] ([IsSent]);
GO


CREATE INDEX [IX_EventReminders_ReminderMinutesBefore] ON [EventReminders] ([ReminderMinutesBefore]);
GO


CREATE UNIQUE INDEX [IX_ExpensePolicies_ExpenseType] ON [ExpensePolicies] ([ExpenseType]);
GO


CREATE INDEX [IX_ExternalEvidenceDocuments_RequestId] ON [ExternalEvidenceDocuments] ([RequestId]);
GO


CREATE INDEX [IX_ExternalEvidenceRequests_CaseId] ON [ExternalEvidenceRequests] ([CaseId]);
GO


CREATE INDEX [IX_FacialVerificationSessions_BeneficiaryId] ON [FacialVerificationSessions] ([BeneficiaryId]);
GO


CREATE INDEX [IX_FacialVerificationSessions_ExpiresAtUtc] ON [FacialVerificationSessions] ([ExpiresAtUtc]);
GO


CREATE INDEX [IX_FacialVerificationSessions_Status] ON [FacialVerificationSessions] ([Status]);
GO


CREATE UNIQUE INDEX [IX_ForecastCalibrations_AttorneyId] ON [ForecastCalibrations] ([AttorneyId]) WHERE [AttorneyId] IS NOT NULL;
GO


CREATE INDEX [IX_HandoverItems_CaseHandoverId] ON [HandoverItems] ([CaseHandoverId]);
GO


CREATE INDEX [IX_InvoiceEstimateAuthorisations_ApprovedByUserId] ON [InvoiceEstimateAuthorisations] ([ApprovedByUserId]);
GO


CREATE UNIQUE INDEX [IX_InvoiceEstimateAuthorisations_InvoiceId] ON [InvoiceEstimateAuthorisations] ([InvoiceId]);
GO


CREATE INDEX [IX_InvoiceEstimateAuthorisations_MatterCostEstimateId] ON [InvoiceEstimateAuthorisations] ([MatterCostEstimateId]);
GO


CREATE INDEX [IX_InvoicePenalties_AppliedByAccountantId] ON [InvoicePenalties] ([AppliedByAccountantId]);
GO


CREATE UNIQUE INDEX [IX_InvoicePenalties_IdempotencyKey] ON [InvoicePenalties] ([IdempotencyKey]);
GO


CREATE INDEX [IX_InvoicePenalties_InvoiceId] ON [InvoicePenalties] ([InvoiceId]);
GO


CREATE INDEX [IX_Invoices_CaseId] ON [Invoices] ([CaseId]);
GO


CREATE INDEX [IX_Invoices_ClientId] ON [Invoices] ([ClientId]);
GO


CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
GO


CREATE INDEX [IX_Invoices_MatterCostEstimateId] ON [Invoices] ([MatterCostEstimateId]);
GO


CREATE INDEX [IX_Invoices_RetainerId] ON [Invoices] ([RetainerId]);
GO


CREATE INDEX [IX_LawyerProfileLawyerSpecialization_SpecializationsId] ON [LawyerProfileLawyerSpecialization] ([SpecializationsId]);
GO


CREATE INDEX [IX_LawyerProfiles_UserId] ON [LawyerProfiles] ([UserId]);
GO


CREATE INDEX [IX_LegalCostRecoveryAuditEntries_LegalCostRecoveryClaimId_RecordedAtUtc] ON [LegalCostRecoveryAuditEntries] ([LegalCostRecoveryClaimId], [RecordedAtUtc]);
GO


CREATE INDEX [IX_LegalCostRecoveryClaims_AttorneyId] ON [LegalCostRecoveryClaims] ([AttorneyId]);
GO


CREATE INDEX [IX_LegalCostRecoveryClaims_CaseId] ON [LegalCostRecoveryClaims] ([CaseId]);
GO


CREATE INDEX [IX_LegalCostRecoveryClaims_DecidedByUserId] ON [LegalCostRecoveryClaims] ([DecidedByUserId]);
GO


CREATE INDEX [IX_LegalCostRecoveryClaims_Status_SubmittedAtUtc] ON [LegalCostRecoveryClaims] ([Status], [SubmittedAtUtc]);
GO


CREATE UNIQUE INDEX [IX_LegalCostRecoveryTimeEntries_TimeEntryId] ON [LegalCostRecoveryTimeEntries] ([TimeEntryId]);
GO


CREATE UNIQUE INDEX [IX_LegalSubjects_Name] ON [LegalSubjects] ([Name]);
GO


CREATE INDEX [IX_LitigationStrategyDecisions_AttorneyId] ON [LitigationStrategyDecisions] ([AttorneyId]);
GO


CREATE INDEX [IX_LitigationStrategyDecisions_CaseId_Status] ON [LitigationStrategyDecisions] ([CaseId], [Status]);
GO


CREATE INDEX [IX_LitigationStrategyDecisions_DirectorId] ON [LitigationStrategyDecisions] ([DirectorId]);
GO


CREATE UNIQUE INDEX [IX_MatterCostEstimates_CostEstimateEnquiryId_Version] ON [MatterCostEstimates] ([CostEstimateEnquiryId], [Version]);
GO


CREATE INDEX [IX_MatterCostEstimates_LinkedCaseId] ON [MatterCostEstimates] ([LinkedCaseId]);
GO


CREATE INDEX [IX_MatterDisbursements_CaseId_InvoiceId] ON [MatterDisbursements] ([CaseId], [InvoiceId]);
GO


CREATE INDEX [IX_MatterDisbursements_InvoiceId] ON [MatterDisbursements] ([InvoiceId]);
GO


CREATE UNIQUE INDEX [IX_MatterDisbursements_ReimbursementClaimId] ON [MatterDisbursements] ([ReimbursementClaimId]);
GO


CREATE UNIQUE INDEX [IX_MatterExpenseTerms_CaseId_ExpenseType] ON [MatterExpenseTerms] ([CaseId], [ExpenseType]);
GO


CREATE INDEX [IX_Payments_ClientId] ON [Payments] ([ClientId]);
GO


CREATE INDEX [IX_Payments_InvoiceId] ON [Payments] ([InvoiceId]);
GO


CREATE INDEX [IX_Payments_PaymentDate] ON [Payments] ([PaymentDate]);
GO


CREATE INDEX [IX_Payments_TransactionReference] ON [Payments] ([TransactionReference]);
GO


CREATE UNIQUE INDEX [IX_PrecedentConflictFlags_ExistingPrecedentItemId_NewPrecedentItemId] ON [PrecedentConflictFlags] ([ExistingPrecedentItemId], [NewPrecedentItemId]);
GO


CREATE INDEX [IX_PrecedentConflictFlags_NewPrecedentItemId] ON [PrecedentConflictFlags] ([NewPrecedentItemId]);
GO


CREATE UNIQUE INDEX [IX_PrecedentIndexJobs_SourceType_SourceId_ContentHash] ON [PrecedentIndexJobs] ([SourceType], [SourceId], [ContentHash]);
GO


CREATE INDEX [IX_PrecedentIndexJobs_Status_NextAttemptAtUtc] ON [PrecedentIndexJobs] ([Status], [NextAttemptAtUtc]);
GO


CREATE INDEX [IX_PrecedentItems_LegalSubjectId_IsCurrent] ON [PrecedentItems] ([LegalSubjectId], [IsCurrent]);
GO


CREATE UNIQUE INDEX [IX_PrecedentItems_SourceType_SourceId_ContentHash] ON [PrecedentItems] ([SourceType], [SourceId], [ContentHash]);
GO


CREATE UNIQUE INDEX [IX_PrecedentPassages_PrecedentItemId_PassageNumber] ON [PrecedentPassages] ([PrecedentItemId], [PassageNumber]);
GO


CREATE INDEX [IX_ReimbursementAuditEntries_ReimbursementClaimId_RecordedAtUtc] ON [ReimbursementAuditEntries] ([ReimbursementClaimId], [RecordedAtUtc]);
GO


CREATE INDEX [IX_ReimbursementClaims_AttorneyId_SubmittedAtUtc] ON [ReimbursementClaims] ([AttorneyId], [SubmittedAtUtc]);
GO


CREATE INDEX [IX_ReimbursementClaims_CaseId] ON [ReimbursementClaims] ([CaseId]);
GO


CREATE UNIQUE INDEX [IX_ReimbursementClaims_ClaimNumber] ON [ReimbursementClaims] ([ClaimNumber]);
GO


CREATE INDEX [IX_ReimbursementClaims_DecidedByUserId] ON [ReimbursementClaims] ([DecidedByUserId]);
GO


CREATE INDEX [IX_ReimbursementClaims_Status_SubmittedAtUtc] ON [ReimbursementClaims] ([Status], [SubmittedAtUtc]);
GO


CREATE INDEX [IX_RetainerActionLogs_CreatedAt] ON [RetainerActionLogs] ([CreatedAt]);
GO


CREATE INDEX [IX_RetainerActionLogs_RetainerId] ON [RetainerActionLogs] ([RetainerId]);
GO


CREATE INDEX [IX_RetainerActionLogs_UserId] ON [RetainerActionLogs] ([UserId]);
GO


CREATE INDEX [IX_RetainerPayments_PaymentDate] ON [RetainerPayments] ([PaymentDate]);
GO


CREATE INDEX [IX_RetainerPayments_PaymentScheduleId] ON [RetainerPayments] ([PaymentScheduleId]);
GO


CREATE INDEX [IX_RetainerPayments_RetainerId] ON [RetainerPayments] ([RetainerId]);
GO


CREATE INDEX [IX_RetainerPayments_TransactionReference] ON [RetainerPayments] ([TransactionReference]);
GO


CREATE INDEX [IX_RetainerPaymentSchedules_DueDate] ON [RetainerPaymentSchedules] ([DueDate]);
GO


CREATE INDEX [IX_RetainerPaymentSchedules_RetainerId] ON [RetainerPaymentSchedules] ([RetainerId]);
GO


CREATE INDEX [IX_RetainerPaymentSchedules_Status] ON [RetainerPaymentSchedules] ([Status]);
GO


CREATE INDEX [IX_RetainerRenewals_RenewedByUserId] ON [RetainerRenewals] ([RenewedByUserId]);
GO


CREATE INDEX [IX_RetainerRenewals_RetainerId] ON [RetainerRenewals] ([RetainerId]);
GO


CREATE INDEX [IX_Retainers_ApprovedByUserId] ON [Retainers] ([ApprovedByUserId]);
GO


CREATE INDEX [IX_Retainers_AssignedLawyerId] ON [Retainers] ([AssignedLawyerId]);
GO


CREATE INDEX [IX_Retainers_CaseId] ON [Retainers] ([CaseId]);
GO


CREATE INDEX [IX_Retainers_ClientId] ON [Retainers] ([ClientId]);
GO


CREATE INDEX [IX_Retainers_SignatureToken] ON [Retainers] ([SignatureToken]);
GO


CREATE INDEX [IX_Retainers_Status] ON [Retainers] ([Status]);
GO


CREATE INDEX [IX_Retainers_SubmittedByUserId] ON [Retainers] ([SubmittedByUserId]);
GO


CREATE INDEX [IX_Retainers_TemplateId] ON [Retainers] ([TemplateId]);
GO


CREATE INDEX [IX_RetainerTemplates_Category] ON [RetainerTemplates] ([Category]);
GO


CREATE INDEX [IX_RetainerTemplates_IsPublic] ON [RetainerTemplates] ([IsPublic]);
GO


CREATE INDEX [IX_ServiceComplaints_CaseId] ON [ServiceComplaints] ([CaseId]);
GO


CREATE INDEX [IX_ServiceComplaints_ClientId] ON [ServiceComplaints] ([ClientId]);
GO


CREATE UNIQUE INDEX [IX_ServiceComplaints_ReferenceNumber] ON [ServiceComplaints] ([ReferenceNumber]);
GO


CREATE INDEX [IX_ServiceComplaints_RoutedToUserId] ON [ServiceComplaints] ([RoutedToUserId]);
GO


CREATE INDEX [IX_ServiceComplaints_Status_ResponseDueAtUtc] ON [ServiceComplaints] ([Status], [ResponseDueAtUtc]);
GO


CREATE UNIQUE INDEX [IX_StaffServiceRecordEntries_ServiceComplaintId_StaffUserId] ON [StaffServiceRecordEntries] ([ServiceComplaintId], [StaffUserId]);
GO


CREATE INDEX [IX_StaffServiceRecordEntries_StaffUserId_RecordedAtUtc] ON [StaffServiceRecordEntries] ([StaffUserId], [RecordedAtUtc]);
GO


CREATE UNIQUE INDEX [IX_SystemNotifications_DeduplicationKey] ON [SystemNotifications] ([DeduplicationKey]) WHERE [DeduplicationKey] IS NOT NULL;
GO


CREATE INDEX [IX_SystemNotifications_UserId] ON [SystemNotifications] ([UserId]);
GO


CREATE INDEX [IX_TaskAttachments_TaskId] ON [TaskAttachments] ([TaskId]);
GO


CREATE INDEX [IX_TaskComments_TaskId] ON [TaskComments] ([TaskId]);
GO


CREATE INDEX [IX_TaskComments_UserId] ON [TaskComments] ([UserId]);
GO


CREATE INDEX [IX_Tasks_AssignedToId] ON [Tasks] ([AssignedToId]);
GO


CREATE INDEX [IX_Tasks_CaseId] ON [Tasks] ([CaseId]);
GO


CREATE INDEX [IX_Tasks_CreatedById] ON [Tasks] ([CreatedById]);
GO


CREATE INDEX [IX_Tasks_DueDate] ON [Tasks] ([DueDate]);
GO


CREATE INDEX [IX_Tasks_ParentTaskId] ON [Tasks] ([ParentTaskId]);
GO


CREATE INDEX [IX_Tasks_Priority] ON [Tasks] ([Priority]);
GO


CREATE INDEX [IX_Tasks_RelatedEventId] ON [Tasks] ([RelatedEventId]);
GO


CREATE INDEX [IX_Tasks_RetainerId] ON [Tasks] ([RetainerId]);
GO


CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);
GO


CREATE INDEX [IX_TimeEntries_CaseId] ON [TimeEntries] ([CaseId]);
GO


CREATE INDEX [IX_TimeEntries_Date] ON [TimeEntries] ([Date]);
GO


CREATE INDEX [IX_TimeEntries_InvoiceId] ON [TimeEntries] ([InvoiceId]);
GO


CREATE INDEX [IX_TimeEntries_IsBillable] ON [TimeEntries] ([IsBillable]);
GO


CREATE INDEX [IX_TimeEntries_LawyerId] ON [TimeEntries] ([LawyerId]);
GO


CREATE INDEX [IX_TimeEntries_RetainerId] ON [TimeEntries] ([RetainerId]);
GO


CREATE UNIQUE INDEX [IX_TrustAccounts_ClientId] ON [TrustAccounts] ([ClientId]);
GO


CREATE INDEX [IX_TrustTransactions_TransactionDate] ON [TrustTransactions] ([TransactionDate]);
GO


CREATE INDEX [IX_TrustTransactions_TrustAccountId] ON [TrustTransactions] ([TrustAccountId]);
GO


CREATE INDEX [IX_TrustTransactions_Type] ON [TrustTransactions] ([Type]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE UNIQUE INDEX [IX_VulnerableClientFlags_ClientId_Safeguard] ON [VulnerableClientFlags] ([ClientId], [Safeguard]) WHERE [Status] <> 3;
GO


CREATE INDEX [IX_VulnerableClientFlags_ClientId_Status] ON [VulnerableClientFlags] ([ClientId], [Status]);
GO


CREATE INDEX [IX_VulnerableClientFlags_RaisedByAttorneyId] ON [VulnerableClientFlags] ([RaisedByAttorneyId]);
GO


CREATE INDEX [IX_VulnerableClientFlags_ReviewedByDirectorId] ON [VulnerableClientFlags] ([ReviewedByDirectorId]);
GO


CREATE INDEX [IX_VulnerableClientFlags_Status_NextReviewAtUtc] ON [VulnerableClientFlags] ([Status], [NextReviewAtUtc]);
GO


CREATE INDEX [IX_VulnerableClientFlags_Status_ReviewDueAtUtc] ON [VulnerableClientFlags] ([Status], [ReviewDueAtUtc]);
GO


CREATE INDEX [IX_VulnerableFlagAcknowledgements_CaseId_StaffUserId_VulnerableClientFlagId] ON [VulnerableFlagAcknowledgements] ([CaseId], [StaffUserId], [VulnerableClientFlagId]);
GO


CREATE INDEX [IX_VulnerableFlagAcknowledgements_StaffUserId] ON [VulnerableFlagAcknowledgements] ([StaffUserId]);
GO


CREATE INDEX [IX_VulnerableFlagAcknowledgements_VulnerableClientFlagId] ON [VulnerableFlagAcknowledgements] ([VulnerableClientFlagId]);
GO


