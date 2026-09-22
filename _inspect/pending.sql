IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [Clients] (
        [Id] int NOT NULL IDENTITY,
        [IsBusiness] bit NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [SAIDNumber] nvarchar(13) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [RegistrationNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(450) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [IdentificationNumber] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [TrustAccounts] (
        [Id] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [TotalDeposited] decimal(18,2) NOT NULL,
        [TotalWithdrawn] decimal(18,2) NOT NULL,
        [LastUpdated] datetime2 NOT NULL,
        CONSTRAINT [PK_TrustAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrustAccounts_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [ClientRequests] (
        [Id] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [TemplateId] int NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ClientNotes] nvarchar(max) NOT NULL,
        [PreferredContact] nvarchar(50) NOT NULL,
        [Urgency] nvarchar(50) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ReviewedDate] datetime2 NULL,
        [ConvertedToRetainerId] int NULL,
        [AdminNotes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ClientRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientRequests_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClientRequests_RetainerTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [RetainerTemplates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [Cases] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ClientId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CaseNumber] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [LawyerId] int NULL,
        CONSTRAINT [PK_Cases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cases_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Users_LawyerId] FOREIGN KEY ([LawyerId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [TrustTransactions] (
        [Id] int NOT NULL IDENTITY,
        [TrustAccountId] int NOT NULL,
        [Type] nvarchar(450) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Reference] nvarchar(100) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [RelatedInvoiceId] int NULL,
        [AuthorizedBy] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_TrustTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrustTransactions_TrustAccounts_TrustAccountId] FOREIGN KEY ([TrustAccountId]) REFERENCES [TrustAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [CaseNotes] (
        [Id] int NOT NULL IDENTITY,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CaseId] int NOT NULL,
        CONSTRAINT [PK_CaseNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseNotes_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
        [Category] int NOT NULL,
        [VersionNumber] int NULL,
        [ParentDocumentId] int NULL,
        [IsPublic] bit NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        [LastModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Documents_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]),
        CONSTRAINT [FK_Documents_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]),
        CONSTRAINT [FK_Documents_Users_UploadedById] FOREIGN KEY ([UploadedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [Retainers] (
        [Id] int NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [CaseId] int NULL,
        [TemplateId] int NULL,
        [Title] nvarchar(200) NOT NULL,
        [ScopeOfWork] nvarchar(max) NOT NULL,
        [SpecialTerms] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [IncludedHours] int NOT NULL,
        [OverageRate] decimal(18,2) NOT NULL,
        [BillingCycle] nvarchar(50) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [LawyerNotes] nvarchar(max) NOT NULL,
        [AdminNotes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [SubmittedForApprovalDate] datetime2 NULL,
        [SubmittedByUserId] int NULL,
        [ApprovedDate] datetime2 NULL,
        [ApprovedByUserId] int NULL,
        [SentToClientDate] datetime2 NULL,
        [SignedDate] datetime2 NULL,
        [ClientSignatureName] nvarchar(200) NOT NULL,
        [ClientIPAddress] nvarchar(50) NOT NULL,
        [PaymentConfirmedDate] datetime2 NULL,
        [PaymentReference] nvarchar(100) NOT NULL,
        [AmountPaid] decimal(18,2) NULL,
        [PdfPath] nvarchar(max) NOT NULL,
        [SignatureToken] nvarchar(100) NOT NULL,
        [SignatureTokenExpiry] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [RejectionReason] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Retainers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Retainers_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Retainers_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Retainers_RetainerTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [RetainerTemplates] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Retainers_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Retainers_Users_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [LawyerProfileLawyerSpecialization] (
        [LawyerProfilesId] int NOT NULL,
        [SpecializationsId] int NOT NULL,
        CONSTRAINT [PK_LawyerProfileLawyerSpecialization] PRIMARY KEY ([LawyerProfilesId], [SpecializationsId]),
        CONSTRAINT [FK_LawyerProfileLawyerSpecialization_LawyerProfiles_LawyerProfilesId] FOREIGN KEY ([LawyerProfilesId]) REFERENCES [LawyerProfiles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LawyerProfileLawyerSpecialization_LawyerSpecializations_SpecializationsId] FOREIGN KEY ([SpecializationsId]) REFERENCES [LawyerSpecializations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
        CONSTRAINT [FK_CalendarEvents_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]),
        CONSTRAINT [FK_CalendarEvents_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_CalendarEvents_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
        [PdfPath] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Invoices_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [RetainerPaymentSchedules] (
        [Id] int NOT NULL IDENTITY,
        [RetainerId] int NOT NULL,
        [InstallmentNumber] int NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [AmountDue] decimal(18,2) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [PaidDate] datetime2 NULL,
        [PaymentReference] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_RetainerPaymentSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RetainerPaymentSchedules_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [Tasks] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Priority] nvarchar(450) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [DueDate] datetime2 NULL,
        [AssignedToId] int NULL,
        [CompletedAt] datetime2 NULL,
        [AssignedToUserId] int NULL,
        [CreatedById] int NULL,
        [CaseId] int NULL,
        [RetainerId] int NULL,
        [ParentTaskId] int NULL,
        [RelatedEventId] int NULL,
        [EstimatedHours] decimal(18,2) NULL,
        [ActualHours] decimal(18,2) NULL,
        [IsBillable] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tasks_CalendarEvents_RelatedEventId] FOREIGN KEY ([RelatedEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tasks_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tasks_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]),
        CONSTRAINT [FK_Tasks_Tasks_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [Tasks] ([Id]),
        CONSTRAINT [FK_Tasks_Users_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tasks_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Tasks_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [ClientId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [TransactionReference] nvarchar(100) NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        [IsTrustAccountDeposit] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [RetainerPayments] (
        [Id] int NOT NULL IDENTITY,
        [RetainerId] int NOT NULL,
        [PaymentScheduleId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [TransactionReference] nvarchar(100) NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        [IsDepositedToTrust] bit NOT NULL,
        CONSTRAINT [PK_RetainerPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RetainerPayments_RetainerPaymentSchedules_PaymentScheduleId] FOREIGN KEY ([PaymentScheduleId]) REFERENCES [RetainerPaymentSchedules] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_RetainerPayments_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE TABLE [TaskAttachments] (
        [Id] int NOT NULL IDENTITY,
        [TaskId] int NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskAttachments_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_AssignedToUserId] ON [CalendarEvents] ([AssignedToUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_CaseId] ON [CalendarEvents] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_CreatedByUserId] ON [CalendarEvents] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_ParentEventId] ON [CalendarEvents] ([ParentEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_RetainerId] ON [CalendarEvents] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_StartDateTime] ON [CalendarEvents] ([StartDateTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_Status] ON [CalendarEvents] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CaseNotes_CaseId] ON [CaseNotes] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cases_CaseNumber] ON [Cases] ([CaseNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cases_ClientId] ON [Cases] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cases_LawyerId] ON [Cases] ([LawyerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientRequests_ClientId] ON [ClientRequests] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientRequests_CreatedDate] ON [ClientRequests] ([CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientRequests_Status] ON [ClientRequests] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientRequests_TemplateId] ON [ClientRequests] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Clients_Email] ON [Clients] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Documents_CaseId] ON [Documents] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Documents_ClientId] ON [Documents] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Documents_UploadedById] ON [Documents] ([UploadedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_DocumentId] ON [DocumentShares] ([DocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_SharedByUserId] ON [DocumentShares] ([SharedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_SharedWithUserId] ON [DocumentShares] ([SharedWithUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventAttendees_EventId] ON [EventAttendees] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventAttendees_UserId] ON [EventAttendees] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventReminders_EventId] ON [EventReminders] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventReminders_IsSent] ON [EventReminders] ([IsSent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventReminders_ReminderMinutesBefore] ON [EventReminders] ([ReminderMinutesBefore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Invoices_CaseId] ON [Invoices] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Invoices_ClientId] ON [Invoices] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Invoices_RetainerId] ON [Invoices] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LawyerProfileLawyerSpecialization_SpecializationsId] ON [LawyerProfileLawyerSpecialization] ([SpecializationsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LawyerProfiles_UserId] ON [LawyerProfiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_ClientId] ON [Payments] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_InvoiceId] ON [Payments] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_PaymentDate] ON [Payments] ([PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_TransactionReference] ON [Payments] ([TransactionReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPayments_PaymentDate] ON [RetainerPayments] ([PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPayments_PaymentScheduleId] ON [RetainerPayments] ([PaymentScheduleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPayments_RetainerId] ON [RetainerPayments] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPayments_TransactionReference] ON [RetainerPayments] ([TransactionReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPaymentSchedules_DueDate] ON [RetainerPaymentSchedules] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPaymentSchedules_RetainerId] ON [RetainerPaymentSchedules] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerPaymentSchedules_Status] ON [RetainerPaymentSchedules] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_ApprovedByUserId] ON [Retainers] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_CaseId] ON [Retainers] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_ClientId] ON [Retainers] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_SignatureToken] ON [Retainers] ([SignatureToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_Status] ON [Retainers] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_SubmittedByUserId] ON [Retainers] ([SubmittedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Retainers_TemplateId] ON [Retainers] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerTemplates_Category] ON [RetainerTemplates] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RetainerTemplates_IsPublic] ON [RetainerTemplates] ([IsPublic]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaskAttachments_TaskId] ON [TaskAttachments] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaskComments_TaskId] ON [TaskComments] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaskComments_UserId] ON [TaskComments] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_AssignedToId] ON [Tasks] ([AssignedToId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_AssignedToUserId] ON [Tasks] ([AssignedToUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_CaseId] ON [Tasks] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_CreatedById] ON [Tasks] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_DueDate] ON [Tasks] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_ParentTaskId] ON [Tasks] ([ParentTaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_Priority] ON [Tasks] ([Priority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_RelatedEventId] ON [Tasks] ([RelatedEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_RetainerId] ON [Tasks] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_CaseId] ON [TimeEntries] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_Date] ON [TimeEntries] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_InvoiceId] ON [TimeEntries] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_IsBillable] ON [TimeEntries] ([IsBillable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_LawyerId] ON [TimeEntries] ([LawyerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_RetainerId] ON [TimeEntries] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TrustAccounts_ClientId] ON [TrustAccounts] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrustTransactions_TransactionDate] ON [TrustTransactions] ([TransactionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrustTransactions_TrustAccountId] ON [TrustTransactions] ([TrustAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrustTransactions_Type] ON [TrustTransactions] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405125255_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405125255_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405204505_MakeClientFieldsNullable'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'SAIDNumber');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Clients] ALTER COLUMN [SAIDNumber] nvarchar(13) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405204505_MakeClientFieldsNullable'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'RegistrationNumber');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Clients] ALTER COLUMN [RegistrationNumber] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405204505_MakeClientFieldsNullable'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'LastName');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Clients] ALTER COLUMN [LastName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405204505_MakeClientFieldsNullable'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'FirstName');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Clients] ALTER COLUMN [FirstName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405204505_MakeClientFieldsNullable'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'CompanyName');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Clients] ALTER COLUMN [CompanyName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405204505_MakeClientFieldsNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405204505_MakeClientFieldsNullable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405205745_MakeIdentificationNumberNullable'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'IdentificationNumber');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Clients] ALTER COLUMN [IdentificationNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405205745_MakeIdentificationNumberNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405205745_MakeIdentificationNumberNullable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [ActivatedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [AssignedLawyerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [CancellationReason] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [CancelledByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [CancelledDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [ChangeRequestedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [ClientChangeRequest] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [PaymentDueDays] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [RejectedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [RejectedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [RequiresUpfrontPayment] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [RevisionRequestedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [RevisionRequestedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [Source] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD [UpdatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE TABLE [RetainerActionLogs] (
        [Id] int NOT NULL IDENTITY,
        [RetainerId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [Details] nvarchar(1000) NOT NULL,
        [UserId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RetainerActionLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RetainerActionLogs_Retainers_RetainerId] FOREIGN KEY ([RetainerId]) REFERENCES [Retainers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RetainerActionLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE INDEX [IX_Retainers_AssignedLawyerId] ON [Retainers] ([AssignedLawyerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE INDEX [IX_RetainerActionLogs_CreatedAt] ON [RetainerActionLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE INDEX [IX_RetainerActionLogs_RetainerId] ON [RetainerActionLogs] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE INDEX [IX_RetainerActionLogs_UserId] ON [RetainerActionLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE INDEX [IX_RetainerRenewals_RenewedByUserId] ON [RetainerRenewals] ([RenewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    CREATE INDEX [IX_RetainerRenewals_RetainerId] ON [RetainerRenewals] ([RetainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    ALTER TABLE [Retainers] ADD CONSTRAINT [FK_Retainers_Users_AssignedLawyerId] FOREIGN KEY ([AssignedLawyerId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406103952_AddRetainerAuditAndRenewal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406103952_AddRetainerAuditAndRenewal', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrustTransactions]') AND [c].[name] = N'Reference');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [TrustTransactions] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [TrustTransactions] ALTER COLUMN [Reference] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrustTransactions]') AND [c].[name] = N'Description');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [TrustTransactions] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [TrustTransactions] ALTER COLUMN [Description] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrustTransactions]') AND [c].[name] = N'AuthorizedBy');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [TrustTransactions] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [TrustTransactions] ALTER COLUMN [AuthorizedBy] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'Type');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [Type] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'SignatureToken');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [SignatureToken] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'RejectionReason');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [RejectionReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'PdfPath');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [PdfPath] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'PaymentReference');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [PaymentReference] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'LawyerNotes');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [LawyerNotes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'ClientSignatureName');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [ClientSignatureName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'ClientIPAddress');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [ClientIPAddress] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'ClientChangeRequest');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [ClientChangeRequest] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'CancellationReason');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [CancellationReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'BillingCycle');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [BillingCycle] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Retainers]') AND [c].[name] = N'AdminNotes');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Retainers] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [Retainers] ALTER COLUMN [AdminNotes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RetainerPaymentSchedules]') AND [c].[name] = N'PaymentReference');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [RetainerPaymentSchedules] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [RetainerPaymentSchedules] ALTER COLUMN [PaymentReference] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RetainerPayments]') AND [c].[name] = N'TransactionReference');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [RetainerPayments] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [RetainerPayments] ALTER COLUMN [TransactionReference] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RetainerPayments]') AND [c].[name] = N'Notes');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [RetainerPayments] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [RetainerPayments] ALTER COLUMN [Notes] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RetainerActionLogs]') AND [c].[name] = N'Details');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [RetainerActionLogs] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [RetainerActionLogs] ALTER COLUMN [Details] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClientRequests]') AND [c].[name] = N'Urgency');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [ClientRequests] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [ClientRequests] ALTER COLUMN [Urgency] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClientRequests]') AND [c].[name] = N'PreferredContact');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [ClientRequests] DROP CONSTRAINT ' + @var26 + ';');
    ALTER TABLE [ClientRequests] ALTER COLUMN [PreferredContact] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClientRequests]') AND [c].[name] = N'ClientNotes');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [ClientRequests] DROP CONSTRAINT ' + @var27 + ';');
    ALTER TABLE [ClientRequests] ALTER COLUMN [ClientNotes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    DECLARE @var28 nvarchar(max);
    SELECT @var28 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClientRequests]') AND [c].[name] = N'AdminNotes');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [ClientRequests] DROP CONSTRAINT ' + @var28 + ';');
    ALTER TABLE [ClientRequests] ALTER COLUMN [AdminNotes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192247_MakeCancellationReasonNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406192247_MakeCancellationReasonNullable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406192516_FixNullableStringColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406192516_FixNullableStringColumns', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    ALTER TABLE [Tasks] DROP CONSTRAINT [FK_Tasks_Users_AssignedToUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    DROP INDEX [IX_Tasks_AssignedToUserId] ON [Tasks];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    DECLARE @var29 nvarchar(max);
    SELECT @var29 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'AssignedToUserId');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var29 + ';');
    ALTER TABLE [Tasks] DROP COLUMN [AssignedToUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    DECLARE @var30 nvarchar(max);
    SELECT @var30 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'Description');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var30 + ';');
    ALTER TABLE [Tasks] ALTER COLUMN [Description] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    DECLARE @var31 nvarchar(max);
    SELECT @var31 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'Notes');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT ' + @var31 + ';');
    ALTER TABLE [Payments] ALTER COLUMN [Notes] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    DECLARE @var32 nvarchar(max);
    SELECT @var32 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'PdfPath');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var32 + ';');
    ALTER TABLE [Invoices] ALTER COLUMN [PdfPath] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    DECLARE @var33 nvarchar(max);
    SELECT @var33 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'Description');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var33 + ';');
    ALTER TABLE [Invoices] ALTER COLUMN [Description] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407024709_MakeTaskDescriptionNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407024709_MakeTaskDescriptionNullable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [AppointmentFee] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [BillingProcessed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [BillingProcessedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [ClientId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [ClientRespondedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [ClientResponseComments] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [ClientResponseStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [GeneratedInvoiceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [LatePenaltyGraceDays] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [LatePenaltyType] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [LatePenaltyValue] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [PaymentDueDays] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
        [Status] nvarchar(450) NOT NULL,
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
        CONSTRAINT [PK_InvoicePenalties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoicePenalties_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE TABLE [BeneficiaryRequirementAssignments] (
        [BeneficiaryId] int NOT NULL,
        [RequirementId] int NOT NULL,
        [IsRequired] bit NOT NULL,
        CONSTRAINT [PK_BeneficiaryRequirementAssignments] PRIMARY KEY ([BeneficiaryId], [RequirementId]),
        CONSTRAINT [FK_BeneficiaryRequirementAssignments_Beneficiaries_BeneficiaryId] FOREIGN KEY ([BeneficiaryId]) REFERENCES [Beneficiaries] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BeneficiaryRequirementAssignments_BeneficiaryDocumentRequirements_RequirementId] FOREIGN KEY ([RequirementId]) REFERENCES [BeneficiaryDocumentRequirements] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_ClientId] ON [CalendarEvents] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_CalendarEvents_GeneratedInvoiceId] ON [CalendarEvents] ([GeneratedInvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppointmentBillingRecords_CalendarEventId] ON [AppointmentBillingRecords] ([CalendarEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppointmentBillingRecords_IdempotencyKey] ON [AppointmentBillingRecords] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_AppointmentInvitations_CalendarEventId] ON [AppointmentInvitations] ([CalendarEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_AppointmentInvitations_ExpiresAtUtc] ON [AppointmentInvitations] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppointmentInvitations_TokenHash] ON [AppointmentInvitations] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_CreatedAtUtc] ON [AuditEntries] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_EntityType_EntityId] ON [AuditEntries] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_Beneficiaries_BenefactorClientId] ON [Beneficiaries] ([BenefactorClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_Beneficiaries_Email] ON [Beneficiaries] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_Beneficiaries_ReviewedByUserId] ON [Beneficiaries] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_Beneficiaries_Status] ON [Beneficiaries] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BeneficiaryDocumentRequirements_Code] ON [BeneficiaryDocumentRequirements] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryDocuments_BeneficiaryId] ON [BeneficiaryDocuments] ([BeneficiaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryDocuments_PreScreenStatus] ON [BeneficiaryDocuments] ([PreScreenStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryDocuments_RequirementId] ON [BeneficiaryDocuments] ([RequirementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryInvitations_BeneficiaryId] ON [BeneficiaryInvitations] ([BeneficiaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryInvitations_ExpiresAtUtc] ON [BeneficiaryInvitations] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BeneficiaryInvitations_TokenHash] ON [BeneficiaryInvitations] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryRequirementAssignments_RequirementId] ON [BeneficiaryRequirementAssignments] ([RequirementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmailOutboxMessages_DeduplicationKey] ON [EmailOutboxMessages] ([DeduplicationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_EmailOutboxMessages_Status_NextAttemptAtUtc] ON [EmailOutboxMessages] ([Status], [NextAttemptAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_FacialVerificationSessions_BeneficiaryId] ON [FacialVerificationSessions] ([BeneficiaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_FacialVerificationSessions_ExpiresAtUtc] ON [FacialVerificationSessions] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_FacialVerificationSessions_Status] ON [FacialVerificationSessions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoicePenalties_IdempotencyKey] ON [InvoicePenalties] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_InvoicePenalties_InvoiceId] ON [InvoicePenalties] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SystemNotifications_DeduplicationKey] ON [SystemNotifications] ([DeduplicationKey]) WHERE [DeduplicationKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    CREATE INDEX [IX_SystemNotifications_UserId] ON [SystemNotifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD CONSTRAINT [FK_CalendarEvents_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD CONSTRAINT [FK_CalendarEvents_Invoices_GeneratedInvoiceId] FOREIGN KEY ([GeneratedInvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728082528_AddBeneficiaryIdentityVerification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728082528_AddBeneficiaryIdentityVerification', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728084522_ConfigureRemainingDecimalPrecision'
)
BEGIN
    DECLARE @var34 nvarchar(max);
    SELECT @var34 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'EstimatedHours');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var34 + ';');
    ALTER TABLE [Tasks] ALTER COLUMN [EstimatedHours] decimal(10,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728084522_ConfigureRemainingDecimalPrecision'
)
BEGIN
    DECLARE @var35 nvarchar(max);
    SELECT @var35 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'ActualHours');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var35 + ';');
    ALTER TABLE [Tasks] ALTER COLUMN [ActualHours] decimal(10,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728084522_ConfigureRemainingDecimalPrecision'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728084522_ConfigureRemainingDecimalPrecision', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102744_AddBeneficiaryAssetAccessTerms'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [AccessEligibleFromUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102744_AddBeneficiaryAssetAccessTerms'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [AccessEligibleUntilUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102744_AddBeneficiaryAssetAccessTerms'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [AssetAccessTerms] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102744_AddBeneficiaryAssetAccessTerms'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [EntitlementDescription] nvarchar(1000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102744_AddBeneficiaryAssetAccessTerms'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [PermittedAssetPurposes] nvarchar(1000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102744_AddBeneficiaryAssetAccessTerms'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728102744_AddBeneficiaryAssetAccessTerms', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseType] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    ALTER TABLE [Cases] ADD [EvidenceStrength] decimal(6,5) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    ALTER TABLE [Cases] ADD [RecordedOutcome] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
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
        CONSTRAINT [PK_CaseHandovers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseHandovers_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseHandovers_Users_OutgoingAttorneyId] FOREIGN KEY ([OutgoingAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseHandovers_Users_ReceivingAttorneyId] FOREIGN KEY ([ReceivingAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
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
        CONSTRAINT [PK_ServiceComplaints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceComplaints_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceComplaints_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceComplaints_Users_RoutedToUserId] FOREIGN KEY ([RoutedToUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE TABLE [HandoverItems] (
        [Id] int NOT NULL IDENTITY,
        [CaseHandoverId] int NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsMandatory] bit NOT NULL,
        [IsResolved] bit NOT NULL,
        [ResolutionNote] nvarchar(max) NULL,
        CONSTRAINT [PK_HandoverItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HandoverItems_CaseHandovers_CaseHandoverId] FOREIGN KEY ([CaseHandoverId]) REFERENCES [CaseHandovers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_CaseForecasts_AttorneyId] ON [CaseForecasts] ([AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_CaseForecasts_CaseId] ON [CaseForecasts] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_CaseHandovers_CaseId_Status] ON [CaseHandovers] ([CaseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_CaseHandovers_OutgoingAttorneyId] ON [CaseHandovers] ([OutgoingAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_CaseHandovers_ReceivingAttorneyId] ON [CaseHandovers] ([ReceivingAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_HandoverItems_CaseHandoverId] ON [HandoverItems] ([CaseHandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_ServiceComplaints_CaseId] ON [ServiceComplaints] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_ServiceComplaints_ClientId] ON [ServiceComplaints] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceComplaints_ReferenceNumber] ON [ServiceComplaints] ([ReferenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_ServiceComplaints_RoutedToUserId] ON [ServiceComplaints] ([RoutedToUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    CREATE INDEX [IX_ServiceComplaints_Status_ResponseDueAtUtc] ON [ServiceComplaints] ([Status], [ResponseDueAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729065001_PracticeIntelligenceUseCases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729065001_PracticeIntelligenceUseCases', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    ALTER TABLE [HandoverItems] ADD [SourceFingerprint] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [CaseReassignmentId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE TABLE [ClientCorrespondence] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] int NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [ReceivedAtUtc] datetime2 NOT NULL,
        [AnsweredAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ClientCorrespondence] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientCorrespondence_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_CaseHandovers_CaseReassignmentId] ON [CaseHandovers] ([CaseReassignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_CaseReassignments_ApprovedByUserId] ON [CaseReassignments] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_CaseReassignments_CaseId_Status] ON [CaseReassignments] ([CaseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_CaseReassignments_OutgoingAttorneyId] ON [CaseReassignments] ([OutgoingAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_CaseReassignments_ReceivingAttorneyId] ON [CaseReassignments] ([ReceivingAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_ClientCorrespondence_CaseId_AnsweredAtUtc] ON [ClientCorrespondence] ([CaseId], [AnsweredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    CREATE INDEX [IX_ComplaintAttachments_ServiceComplaintId] ON [ComplaintAttachments] ([ServiceComplaintId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ForecastCalibrations_AttorneyId] ON [ForecastCalibrations] ([AttorneyId]) WHERE [AttorneyId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD CONSTRAINT [FK_CaseHandovers_CaseReassignments_CaseReassignmentId] FOREIGN KEY ([CaseReassignmentId]) REFERENCES [CaseReassignments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729070633_CompletePracticeIntelligenceWorkflows'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729070633_CompletePracticeIntelligenceWorkflows', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
    CREATE INDEX [IX_ClientForecastRequests_CaseId_Status] ON [ClientForecastRequests] ([CaseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
    CREATE INDEX [IX_ClientForecastRequests_ClientId] ON [ClientForecastRequests] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
    CREATE INDEX [IX_ClientForecastRequests_FulfilledByForecastId] ON [ClientForecastRequests] ([FulfilledByForecastId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaffServiceRecordEntries_ServiceComplaintId_StaffUserId] ON [StaffServiceRecordEntries] ([ServiceComplaintId], [StaffUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
    CREATE INDEX [IX_StaffServiceRecordEntries_StaffUserId_RecordedAtUtc] ON [StaffServiceRecordEntries] ([StaffUserId], [RecordedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072005_CloseUseCaseAcceptanceGaps'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729072005_CloseUseCaseAcceptanceGaps', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072202_AddDocumentSignatureState'
)
BEGIN
    ALTER TABLE [Documents] ADD [RequiresSignature] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072202_AddDocumentSignatureState'
)
BEGIN
    ALTER TABLE [Documents] ADD [SignedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729072202_AddDocumentSignatureState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729072202_AddDocumentSignatureState', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    ALTER TABLE [Invoices] ADD [MatterCostEstimateId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    ALTER TABLE [Invoices] ADD [RequiresEstimateAuthorisation] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_Invoices_MatterCostEstimateId] ON [Invoices] ([MatterCostEstimateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_CostEstimateCoverageGaps_Status_RecordedAtUtc] ON [CostEstimateCoverageGaps] ([Status], [RecordedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_CostEstimateEnquiries_ClientId] ON [CostEstimateEnquiries] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_CostEstimateEnquiries_Email_CreatedAtUtc] ON [CostEstimateEnquiries] ([Email], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CostEstimateEnquiries_PublicToken] ON [CostEstimateEnquiries] ([PublicToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_InvoiceEstimateAuthorisations_ApprovedByUserId] ON [InvoiceEstimateAuthorisations] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoiceEstimateAuthorisations_InvoiceId] ON [InvoiceEstimateAuthorisations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_InvoiceEstimateAuthorisations_MatterCostEstimateId] ON [InvoiceEstimateAuthorisations] ([MatterCostEstimateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MatterCostEstimates_CostEstimateEnquiryId_Version] ON [MatterCostEstimates] ([CostEstimateEnquiryId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    CREATE INDEX [IX_MatterCostEstimates_LinkedCaseId] ON [MatterCostEstimates] ([LinkedCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_MatterCostEstimates_MatterCostEstimateId] FOREIGN KEY ([MatterCostEstimateId]) REFERENCES [MatterCostEstimates] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729073924_EstimateMatterCost'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729073924_EstimateMatterCost', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE TABLE [ExpensePolicies] (
        [Id] int NOT NULL IDENTITY,
        [ExpenseType] int NOT NULL,
        [PerItemLimit] decimal(18,2) NOT NULL,
        [DelegatedApprovalLimit] decimal(18,2) NOT NULL,
        [DefaultClassification] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ExpensePolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE TABLE [MatterExpenseTerms] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] int NOT NULL,
        [ExpenseType] int NOT NULL,
        [Classification] int NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_MatterExpenseTerms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MatterExpenseTerms_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE TABLE [ReimbursementClaims] (
        [Id] int NOT NULL IDENTITY,
        [ClaimNumber] nvarchar(450) NOT NULL,
        [CaseId] int NOT NULL,
        [AttorneyId] int NOT NULL,
        [ExpenseType] int NOT NULL,
        [ExpenseDate] datetime2 NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultClassification', N'DelegatedApprovalLimit', N'ExpenseType', N'IsActive', N'PerItemLimit') AND [object_id] = OBJECT_ID(N'[ExpensePolicies]'))
        SET IDENTITY_INSERT [ExpensePolicies] ON;
    EXEC(N'INSERT INTO [ExpensePolicies] ([Id], [DefaultClassification], [DelegatedApprovalLimit], [ExpenseType], [IsActive], [PerItemLimit])
    VALUES (1, 0, 1500.0, 0, CAST(1 AS bit), 2500.0),
    (2, 0, 5000.0, 1, CAST(1 AS bit), 15000.0),
    (3, 0, 5000.0, 2, CAST(1 AS bit), 10000.0),
    (4, 0, 1000.0, 3, CAST(1 AS bit), 1500.0),
    (5, 0, 2500.0, 4, CAST(1 AS bit), 3500.0),
    (6, 1, 500.0, 5, CAST(1 AS bit), 600.0),
    (7, 0, 500.0, 6, CAST(1 AS bit), 500.0),
    (8, 1, 1000.0, 7, CAST(1 AS bit), 2000.0),
    (9, 1, 500.0, 8, CAST(1 AS bit), 1000.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultClassification', N'DelegatedApprovalLimit', N'ExpenseType', N'IsActive', N'PerItemLimit') AND [object_id] = OBJECT_ID(N'[ExpensePolicies]'))
        SET IDENTITY_INSERT [ExpensePolicies] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_AttorneyReimbursementPayables_AttorneyId] ON [AttorneyReimbursementPayables] ([AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttorneyReimbursementPayables_ReimbursementClaimId] ON [AttorneyReimbursementPayables] ([ReimbursementClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExpensePolicies_ExpenseType] ON [ExpensePolicies] ([ExpenseType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_MatterDisbursements_CaseId_InvoiceId] ON [MatterDisbursements] ([CaseId], [InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_MatterDisbursements_InvoiceId] ON [MatterDisbursements] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MatterDisbursements_ReimbursementClaimId] ON [MatterDisbursements] ([ReimbursementClaimId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MatterExpenseTerms_CaseId_ExpenseType] ON [MatterExpenseTerms] ([CaseId], [ExpenseType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_ReimbursementAuditEntries_ReimbursementClaimId_RecordedAtUtc] ON [ReimbursementAuditEntries] ([ReimbursementClaimId], [RecordedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_ReimbursementClaims_AttorneyId_SubmittedAtUtc] ON [ReimbursementClaims] ([AttorneyId], [SubmittedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_ReimbursementClaims_CaseId] ON [ReimbursementClaims] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReimbursementClaims_ClaimNumber] ON [ReimbursementClaims] ([ClaimNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_ReimbursementClaims_DecidedByUserId] ON [ReimbursementClaims] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    CREATE INDEX [IX_ReimbursementClaims_Status_SubmittedAtUtc] ON [ReimbursementClaims] ([Status], [SubmittedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729081541_ClaimReimbursement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729081541_ClaimReimbursement', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    ALTER TABLE [Cases] ADD [OutcomeIsConfidential] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    ALTER TABLE [Cases] ADD [OutcomeIsPrivileged] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    ALTER TABLE [Cases] ADD [OutcomeSummary] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    ALTER TABLE [CaseNotes] ADD [IsConfidential] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    ALTER TABLE [CaseNotes] ADD [IsPrivileged] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE TABLE [LegalSubjects] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Keywords] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_LegalSubjects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
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
        [NextAttemptAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PrecedentIndexJobs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE TABLE [PrecedentPassages] (
        [Id] int NOT NULL IDENTITY,
        [PrecedentItemId] int NOT NULL,
        [PassageNumber] int NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [EmbeddingJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PrecedentPassages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrecedentPassages_PrecedentItems_PrecedentItemId] FOREIGN KEY ([PrecedentItemId]) REFERENCES [PrecedentItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Keywords', N'Name') AND [object_id] = OBJECT_ID(N'[LegalSubjects]'))
        SET IDENTITY_INSERT [LegalSubjects] ON;
    EXEC(N'INSERT INTO [LegalSubjects] ([Id], [IsActive], [Keywords], [Name])
    VALUES (1, CAST(1 AS bit), N''litigation,court,trial,appeal,interdict,damages'', N''Civil Litigation''),
    (2, CAST(1 AS bit), N''commercial,contract,company,business,shareholder'', N''Commercial Law''),
    (3, CAST(1 AS bit), N''family,divorce,custody,maintenance,matrimonial'', N''Family Law''),
    (4, CAST(1 AS bit), N''labour,employment,employee,ccma,dismissal'', N''Labour Law''),
    (5, CAST(1 AS bit), N''criminal,bail,prosecution,sentence,accused'', N''Criminal Law''),
    (6, CAST(1 AS bit), N''property,transfer,lease,eviction,conveyancing'', N''Property Law''),
    (7, CAST(1 AS bit), N''estate,will,trust,beneficiary,executor'', N''Estates and Trusts''),
    (8, CAST(1 AS bit), N''injury,accident,raf,medical negligence,compensation'', N''Personal Injury''),
    (9, CAST(1 AS bit), N''general,advice,procedure'', N''General Practice'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Keywords', N'Name') AND [object_id] = OBJECT_ID(N'[LegalSubjects]'))
        SET IDENTITY_INSERT [LegalSubjects] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE INDEX [IX_CoverageCommissions_LegalSubjectId_Status] ON [CoverageCommissions] ([LegalSubjectId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LegalSubjects_Name] ON [LegalSubjects] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PrecedentConflictFlags_ExistingPrecedentItemId_NewPrecedentItemId] ON [PrecedentConflictFlags] ([ExistingPrecedentItemId], [NewPrecedentItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE INDEX [IX_PrecedentConflictFlags_NewPrecedentItemId] ON [PrecedentConflictFlags] ([NewPrecedentItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PrecedentIndexJobs_SourceType_SourceId_ContentHash] ON [PrecedentIndexJobs] ([SourceType], [SourceId], [ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE INDEX [IX_PrecedentIndexJobs_Status_NextAttemptAtUtc] ON [PrecedentIndexJobs] ([Status], [NextAttemptAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE INDEX [IX_PrecedentItems_LegalSubjectId_IsCurrent] ON [PrecedentItems] ([LegalSubjectId], [IsCurrent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PrecedentItems_SourceType_SourceId_ContentHash] ON [PrecedentItems] ([SourceType], [SourceId], [ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PrecedentPassages_PrecedentItemId_PassageNumber] ON [PrecedentPassages] ([PrecedentItemId], [PassageNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729085558_CuratePrecedentLibrary'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729085558_CuratePrecedentLibrary', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729091303_HardenPrecedentCuration'
)
BEGIN
    ALTER TABLE [PrecedentIndexJobs] ADD [ProcessingStartedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729091303_HardenPrecedentCuration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729091303_HardenPrecedentCuration', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE TABLE [AppointmentInterpreterAssignments] (
        [Id] int NOT NULL IDENTITY,
        [CalendarEventId] int NOT NULL,
        [InterpreterName] nvarchar(160) NOT NULL,
        [Language] nvarchar(120) NOT NULL,
        [ContactDetails] nvarchar(200) NULL,
        [AssignedByUserId] int NOT NULL,
        [AssignedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AppointmentInterpreterAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentInterpreterAssignments_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE TABLE [AppointmentSupportPersonAssignments] (
        [Id] int NOT NULL IDENTITY,
        [CalendarEventId] int NOT NULL,
        [SupportPersonName] nvarchar(160) NOT NULL,
        [Relationship] nvarchar(120) NULL,
        [RecordedByUserId] int NOT NULL,
        [RecordedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AppointmentSupportPersonAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppointmentSupportPersonAssignments_CalendarEvents_CalendarEventId] FOREIGN KEY ([CalendarEventId]) REFERENCES [CalendarEvents] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE TABLE [ClientSupportSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [ClientId] int NOT NULL,
        [AuthorisedByStaffUserId] int NOT NULL,
        [SupportPersonName] nvarchar(160) NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [StartsAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ClientSupportSessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
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
        CONSTRAINT [PK_VulnerableClientFlags] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VulnerableClientFlags_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VulnerableClientFlags_Users_RaisedByAttorneyId] FOREIGN KEY ([RaisedByAttorneyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VulnerableClientFlags_Users_ReviewedByDirectorId] FOREIGN KEY ([ReviewedByDirectorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE TABLE [VulnerableFlagAcknowledgements] (
        [Id] bigint NOT NULL IDENTITY,
        [VulnerableClientFlagId] int NOT NULL,
        [CaseId] int NOT NULL,
        [StaffUserId] int NOT NULL,
        [AcknowledgedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_VulnerableFlagAcknowledgements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VulnerableFlagAcknowledgements_VulnerableClientFlags_VulnerableClientFlagId] FOREIGN KEY ([VulnerableClientFlagId]) REFERENCES [VulnerableClientFlags] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppointmentInterpreterAssignments_CalendarEventId] ON [AppointmentInterpreterAssignments] ([CalendarEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppointmentSupportPersonAssignments_CalendarEventId] ON [AppointmentSupportPersonAssignments] ([CalendarEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_ClientSupportSessions_ClientId_ExpiresAtUtc] ON [ClientSupportSessions] ([ClientId], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableClientFlags_ClientId_Status] ON [VulnerableClientFlags] ([ClientId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableClientFlags_RaisedByAttorneyId] ON [VulnerableClientFlags] ([RaisedByAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableClientFlags_ReviewedByDirectorId] ON [VulnerableClientFlags] ([ReviewedByDirectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableClientFlags_Status_NextReviewAtUtc] ON [VulnerableClientFlags] ([Status], [NextReviewAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableClientFlags_Status_ReviewDueAtUtc] ON [VulnerableClientFlags] ([Status], [ReviewDueAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableFlagAcknowledgements_CaseId_StaffUserId_VulnerableClientFlagId] ON [VulnerableFlagAcknowledgements] ([CaseId], [StaffUserId], [VulnerableClientFlagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    CREATE INDEX [IX_VulnerableFlagAcknowledgements_VulnerableClientFlagId] ON [VulnerableFlagAcknowledgements] ([VulnerableClientFlagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729121736_FlagVulnerableClient'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729121736_FlagVulnerableClient', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    CREATE INDEX [IX_VulnerableFlagAcknowledgements_StaffUserId] ON [VulnerableFlagAcknowledgements] ([StaffUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VulnerableClientFlags_ClientId_Safeguard] ON [VulnerableClientFlags] ([ClientId], [Safeguard]) WHERE [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    CREATE INDEX [IX_ClientSupportSessions_AuthorisedByStaffUserId] ON [ClientSupportSessions] ([AuthorisedByStaffUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    CREATE INDEX [IX_AppointmentSupportPersonAssignments_RecordedByUserId] ON [AppointmentSupportPersonAssignments] ([RecordedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    CREATE INDEX [IX_AppointmentInterpreterAssignments_AssignedByUserId] ON [AppointmentInterpreterAssignments] ([AssignedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    ALTER TABLE [AppointmentInterpreterAssignments] ADD CONSTRAINT [FK_AppointmentInterpreterAssignments_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    ALTER TABLE [AppointmentSupportPersonAssignments] ADD CONSTRAINT [FK_AppointmentSupportPersonAssignments_Users_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    ALTER TABLE [ClientSupportSessions] ADD CONSTRAINT [FK_ClientSupportSessions_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    ALTER TABLE [ClientSupportSessions] ADD CONSTRAINT [FK_ClientSupportSessions_Users_AuthorisedByStaffUserId] FOREIGN KEY ([AuthorisedByStaffUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    ALTER TABLE [VulnerableFlagAcknowledgements] ADD CONSTRAINT [FK_VulnerableFlagAcknowledgements_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    ALTER TABLE [VulnerableFlagAcknowledgements] ADD CONSTRAINT [FK_VulnerableFlagAcknowledgements_Users_StaffUserId] FOREIGN KEY ([StaffUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729122337_HardenVulnerableClientIntegrity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729122337_HardenVulnerableClientIntegrity', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729124137_FinaliseVulnerableClientConsistency'
)
BEGIN
    ALTER TABLE [VulnerableClientFlags] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729124137_FinaliseVulnerableClientConsistency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729124137_FinaliseVulnerableClientConsistency', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801130423_HardenIdentityVerificationWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801130423_HardenIdentityVerificationWorkflow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805021556_AddAppointmentApprovalWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805021556_AddAppointmentApprovalWorkflow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805180000_AddBeneficiaryPortalCredentials'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [PortalAccessEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805180000_AddBeneficiaryPortalCredentials'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [PortalPasswordHash] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805180000_AddBeneficiaryPortalCredentials'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [PortalPasswordSetAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805180000_AddBeneficiaryPortalCredentials'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805180000_AddBeneficiaryPortalCredentials', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [TrustAccounts] ADD [IsClosed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [TrustAccounts] ADD [IsFrozen] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Retainers] ADD [AvailableBalance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [InvoicePenalties] ADD [AppliedByAccountantId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [InvoicePenalties] ADD [Reason] nvarchar(1000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Documents] ADD [RequirementCode] nvarchar(80) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Cases] ADD [CourtReadyAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Cases] ADD [IsCourtReady] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Cases] ADD [MatterValue] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Cases] ADD [SettlementAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Cases] ADD [StrategyReviewRequired] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [CalendarEvents] ADD [LawyerApprovalStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [BankAccountHolder] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [BankAccountNumber] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [BankBranchCode] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [BankDetailsConfirmedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [BankName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [Beneficiaries] ADD [EntitlementAmountLimit] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE TABLE [LegalCostRecoveryTimeEntries] (
        [LegalCostRecoveryClaimId] int NOT NULL,
        [TimeEntryId] int NOT NULL,
        [AmountSnapshot] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_LegalCostRecoveryTimeEntries] PRIMARY KEY ([LegalCostRecoveryClaimId], [TimeEntryId]),
        CONSTRAINT [FK_LegalCostRecoveryTimeEntries_LegalCostRecoveryClaims_LegalCostRecoveryClaimId] FOREIGN KEY ([LegalCostRecoveryClaimId]) REFERENCES [LegalCostRecoveryClaims] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LegalCostRecoveryTimeEntries_TimeEntries_TimeEntryId] FOREIGN KEY ([TimeEntryId]) REFERENCES [TimeEntries] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_InvoicePenalties_AppliedByAccountantId] ON [InvoicePenalties] ([AppliedByAccountantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_AttorneyWhereabouts_AttorneyId] ON [AttorneyWhereabouts] ([AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_AttorneyWhereabouts_CalendarEventId] ON [AttorneyWhereabouts] ([CalendarEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_AttorneyWhereabouts_Status_ExpectedReturnAtUtc] ON [AttorneyWhereabouts] ([Status], [ExpectedReturnAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryTrustDisbursementRequests_BeneficiaryId] ON [BeneficiaryTrustDisbursementRequests] ([BeneficiaryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BeneficiaryTrustDisbursementRequests_ReferenceNumber] ON [BeneficiaryTrustDisbursementRequests] ([ReferenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_BeneficiaryTrustDisbursementRequests_TrustAccountId] ON [BeneficiaryTrustDisbursementRequests] ([TrustAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_CaseAuthorityReliances_AttorneyId] ON [CaseAuthorityReliances] ([AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_CaseAuthorityReliances_CaseId_LegalAuthorityId_AttorneyId] ON [CaseAuthorityReliances] ([CaseId], [LegalAuthorityId], [AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_CaseAuthorityReliances_LegalAuthorityId] ON [CaseAuthorityReliances] ([LegalAuthorityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CaseDocumentRequirements_CaseType_Code] ON [CaseDocumentRequirements] ([CaseType], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_CaseDocumentWaivers_CaseId_RequirementId_Status] ON [CaseDocumentWaivers] ([CaseId], [RequirementId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_CaseDocumentWaivers_RequirementId] ON [CaseDocumentWaivers] ([RequirementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_CaseReadinessReviews_CaseId_ReviewedAtUtc] ON [CaseReadinessReviews] ([CaseId], [ReviewedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_ExternalEvidenceDocuments_RequestId] ON [ExternalEvidenceDocuments] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_ExternalEvidenceRequests_CaseId] ON [ExternalEvidenceRequests] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LegalCostRecoveryAuditEntries_LegalCostRecoveryClaimId_RecordedAtUtc] ON [LegalCostRecoveryAuditEntries] ([LegalCostRecoveryClaimId], [RecordedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LegalCostRecoveryClaims_AttorneyId] ON [LegalCostRecoveryClaims] ([AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LegalCostRecoveryClaims_CaseId] ON [LegalCostRecoveryClaims] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LegalCostRecoveryClaims_DecidedByUserId] ON [LegalCostRecoveryClaims] ([DecidedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LegalCostRecoveryClaims_Status_SubmittedAtUtc] ON [LegalCostRecoveryClaims] ([Status], [SubmittedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LegalCostRecoveryTimeEntries_TimeEntryId] ON [LegalCostRecoveryTimeEntries] ([TimeEntryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LitigationStrategyDecisions_AttorneyId] ON [LitigationStrategyDecisions] ([AttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LitigationStrategyDecisions_CaseId_Status] ON [LitigationStrategyDecisions] ([CaseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    CREATE INDEX [IX_LitigationStrategyDecisions_DirectorId] ON [LitigationStrategyDecisions] ([DirectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    ALTER TABLE [InvoicePenalties] ADD CONSTRAINT [FK_InvoicePenalties_Users_AppliedByAccountantId] FOREIGN KEY ([AppliedByAccountantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808193116_SyncProductionModel_20260808'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260808193116_SyncProductionModel_20260808', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809172737_AddCaseCostRecoveryAwardedTotal'
)
BEGIN
    ALTER TABLE [Cases] ADD [CostRecoveryAwardedTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809172737_AddCaseCostRecoveryAwardedTotal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260809172737_AddCaseCostRecoveryAwardedTotal', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165827_SyncProductionModel_20260810'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810165827_SyncProductionModel_20260810', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    ALTER TABLE [ServiceComplaints] ADD [DirectorFeedback] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    ALTER TABLE [ServiceComplaints] ADD [RespondedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    ALTER TABLE [Documents] ADD [CaseDocumentRequestId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_Documents_CaseDocumentRequestId] ON [Documents] ([CaseDocumentRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseDocumentRequests_CaseId_Status] ON [CaseDocumentRequests] ([CaseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseDocumentRequests_ClientId] ON [CaseDocumentRequests] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseDocumentRequests_RequestedByAttorneyId] ON [CaseDocumentRequests] ([RequestedByAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseHandoverRequests_CaseHandoverId] ON [CaseHandoverRequests] ([CaseHandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseHandoverRequests_CaseId] ON [CaseHandoverRequests] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseHandoverRequests_RequestedByAttorneyId] ON [CaseHandoverRequests] ([RequestedByAttorneyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseHandoverRequests_ReviewedByDirectorId] ON [CaseHandoverRequests] ([ReviewedByDirectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    CREATE INDEX [IX_CaseHandoverRequests_Status_RequestedAtUtc] ON [CaseHandoverRequests] ([Status], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    ALTER TABLE [Documents] ADD CONSTRAINT [FK_Documents_CaseDocumentRequests_CaseDocumentRequestId] FOREIGN KEY ([CaseDocumentRequestId]) REFERENCES [CaseDocumentRequests] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810224747_AddCaseCollaborationWorkflows_20260811'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810224747_AddCaseCollaborationWorkflows_20260811', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811093948_AddReceivingAttorneyHandoverResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [AcceptanceRiskAcknowledged] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811093948_AddReceivingAttorneyHandoverResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [AcceptanceSignatureName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811093948_AddReceivingAttorneyHandoverResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [ReceivingAttorneyDeclineReason] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811093948_AddReceivingAttorneyHandoverResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [ReceivingAttorneyQuery] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811093948_AddReceivingAttorneyHandoverResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [ReceivingAttorneyRespondedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811093948_AddReceivingAttorneyHandoverResponse'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811093948_AddReceivingAttorneyHandoverResponse', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100544_AddDirectorHandoverQueryResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [DirectorQueryRespondedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100544_AddDirectorHandoverQueryResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [DirectorQueryRespondedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100544_AddDirectorHandoverQueryResponse'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [DirectorQueryResponse] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100544_AddDirectorHandoverQueryResponse'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811100544_AddDirectorHandoverQueryResponse', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811102726_FinaliseHandoverAndAppointmentFinancialFlow'
)
BEGIN
    ALTER TABLE [ReimbursementClaims] ADD [ApprovedAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811102726_FinaliseHandoverAndAppointmentFinancialFlow'
)
BEGIN
    ALTER TABLE [CaseHandovers] ADD [DirectorQueryVerdictFinal] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811102726_FinaliseHandoverAndAppointmentFinancialFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811102726_FinaliseHandoverAndAppointmentFinancialFlow', N'10.0.10');
END;

COMMIT;
GO

