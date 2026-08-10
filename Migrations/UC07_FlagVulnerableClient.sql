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

