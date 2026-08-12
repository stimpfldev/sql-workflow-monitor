USE SqlWorkflowMonitor_Dev;
GO

/*
    Estado persistente de la instalación Demo.
    La fecha se crea una sola vez y no cambia al reiniciar la aplicación.
*/
IF OBJECT_ID('dbo.ProductInstallation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductInstallation
    (
        InstallationId TINYINT NOT NULL,
        Edition VARCHAR(20) NOT NULL,
        InstalledAtUtc DATETIME2(3) NOT NULL,
        DemoDays INT NOT NULL,
        MaxProcesses INT NOT NULL,
        MaxWorkers INT NOT NULL,
        CsvExportEnabled BIT NOT NULL,

        CONSTRAINT PK_ProductInstallation
            PRIMARY KEY (InstallationId),

        CONSTRAINT CK_ProductInstallation_SingleRow
            CHECK (InstallationId = 1),

        CONSTRAINT CK_ProductInstallation_DemoValues
            CHECK
            (
                Edition = 'Demo'
                AND DemoDays = 30
                AND MaxProcesses = 3
                AND MaxWorkers = 1
                AND CsvExportEnabled = 1
            )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.ProductInstallation
    WHERE InstallationId = 1
)
BEGIN
    INSERT INTO dbo.ProductInstallation
    (
        InstallationId,
        Edition,
        InstalledAtUtc,
        DemoDays,
        MaxProcesses,
        MaxWorkers,
        CsvExportEnabled
    )
    VALUES
    (
        1,
        'Demo',
        SYSUTCDATETIME(),
        30,
        3,
        1,
        1
    );
END;
GO

/* Procesos que ya consumieron un lugar de la Demo. */
IF OBJECT_ID('dbo.DemoProcessRegistrations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DemoProcessRegistrations
    (
        ProcessId INT NOT NULL,
        RegisteredAtUtc DATETIME2(3) NOT NULL
            CONSTRAINT DF_DemoProcessRegistrations_RegisteredAtUtc
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_DemoProcessRegistrations
            PRIMARY KEY (ProcessId),

        CONSTRAINT FK_DemoProcessRegistrations_Processes
            FOREIGN KEY (ProcessId)
            REFERENCES dbo.Processes(ProcessId)
    );
END;
GO

/* Workers/integraciones que ya consumieron un lugar de la Demo. */
IF OBJECT_ID('dbo.DemoWorkerRegistrations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DemoWorkerRegistrations
    (
        WorkerId NVARCHAR(100) NOT NULL,
        RegisteredAtUtc DATETIME2(3) NOT NULL
            CONSTRAINT DF_DemoWorkerRegistrations_RegisteredAtUtc
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_DemoWorkerRegistrations
            PRIMARY KEY (WorkerId)
    );
END;
GO

/* Estado que se muestra en el dashboard. */
CREATE OR ALTER PROCEDURE dbo.sp_ProductAccess_GetDemoStatus
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentUtc DATETIME2(3) =
        SYSUTCDATETIME();

    SELECT
        installation.Edition,
        installation.InstalledAtUtc,
        DATEADD(
            DAY,
            installation.DemoDays,
            installation.InstalledAtUtc) AS ExpiresAtUtc,
        @CurrentUtc AS CurrentUtc,
        installation.MaxProcesses,
        CONVERT(
            INT,
            (
                SELECT COUNT(*)
                FROM dbo.DemoProcessRegistrations
            )) AS RegisteredProcesses,
        installation.MaxWorkers,
        CONVERT(
            INT,
            (
                SELECT COUNT(*)
                FROM dbo.DemoWorkerRegistrations
            )) AS RegisteredWorkers,
        installation.CsvExportEnabled
    FROM dbo.ProductInstallation installation
    WHERE installation.InstallationId = 1;
END;
GO

/*
    Valida la Demo antes de iniciar una ejecución.
    La transacción evita que dos Workers o procesos nuevos superen
    el límite al intentar registrarse al mismo tiempo.
*/
CREATE OR ALTER PROCEDURE dbo.sp_ProductAccess_ValidateDemoStart
    @ProcessId INT,
    @WorkerId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @WorkerId = NULLIF(
        LTRIM(RTRIM(@WorkerId)),
        '');

    IF @WorkerId IS NULL
    BEGIN
        ;THROW 50013,
            'El Worker debe enviar un identificador.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @InstalledAtUtc DATETIME2(3);
        DECLARE @DemoDays INT;
        DECLARE @MaxProcesses INT;
        DECLARE @MaxWorkers INT;

        SELECT
            @InstalledAtUtc = InstalledAtUtc,
            @DemoDays = DemoDays,
            @MaxProcesses = MaxProcesses,
            @MaxWorkers = MaxWorkers
        FROM dbo.ProductInstallation WITH (UPDLOCK, HOLDLOCK)
        WHERE InstallationId = 1;

        IF @InstalledAtUtc IS NULL
        BEGIN
            ;THROW 50014,
                'No se encontró el estado de instalación de la Demo.',
                1;
        END;

        IF SYSUTCDATETIME() >= DATEADD(
                DAY,
                @DemoDays,
                @InstalledAtUtc)
        BEGIN
            ;THROW 50010,
                'La Demo de 30 días venció. El sistema permanece disponible en modo solo lectura.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Processes
            WHERE ProcessId = @ProcessId
              AND IsActive = 1
        )
        BEGIN
            ;THROW 50001,
                'El proceso no existe o está inactivo.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.DemoProcessRegistrations WITH (UPDLOCK, HOLDLOCK)
            WHERE ProcessId = @ProcessId
        )
        BEGIN
            DECLARE @RegisteredProcesses INT;

            SELECT @RegisteredProcesses = COUNT(*)
            FROM dbo.DemoProcessRegistrations WITH (UPDLOCK, HOLDLOCK);

            IF @RegisteredProcesses >= @MaxProcesses
            BEGIN
                ;THROW 50011,
                    'La edición Demo permite monitorear como máximo 3 procesos.',
                    1;
            END;

            INSERT INTO dbo.DemoProcessRegistrations
            (
                ProcessId
            )
            VALUES
            (
                @ProcessId
            );
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.DemoWorkerRegistrations WITH (UPDLOCK, HOLDLOCK)
            WHERE WorkerId = @WorkerId
        )
        BEGIN
            DECLARE @RegisteredWorkers INT;

            SELECT @RegisteredWorkers = COUNT(*)
            FROM dbo.DemoWorkerRegistrations WITH (UPDLOCK, HOLDLOCK);

            IF @RegisteredWorkers >= @MaxWorkers
            BEGIN
                ;THROW 50012,
                    'La edición Demo permite registrar como máximo 1 Worker o integración.',
                    1;
            END;

            INSERT INTO dbo.DemoWorkerRegistrations
            (
                WorkerId
            )
            VALUES
            (
                @WorkerId
            );
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
GO

/* Verificación inicial. */
EXEC dbo.sp_ProductAccess_GetDemoStatus;
GO
