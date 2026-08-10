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

