USE [SqlWorkflowMonitor_Dev];
GO

/*
    Procedimiento común para registrar ejecuciones.

    Los límites y el vencimiento efectivo son recibidos desde
    la política de acceso que previamente validó la Demo
    o la licencia firmada.
*/
CREATE OR ALTER PROCEDURE dbo.sp_ProductAccess_ValidateAndRegisterStart
    @ProcessId INT,
    @WorkerId NVARCHAR(100),
    @MaxProcesses INT,
    @MaxWorkers INT,
    @AccessExpiresAtUtc DATETIME2(3)
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

    IF @MaxProcesses <= 0 OR @MaxWorkers <= 0
    BEGIN
        ;THROW 50015,
            'Los límites del producto no son válidos.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            Bloquea la instalación para serializar los intentos
            concurrentes de registro.
        */
        DECLARE @InstallationExists BIT = 0;

        SELECT
            @InstallationExists = 1
        FROM dbo.ProductInstallation WITH (UPDLOCK, HOLDLOCK)
        WHERE InstallationId = 1;

        IF @InstallationExists = 0
        BEGIN
            ;THROW 50014,
                'No se encontró el estado de instalación del producto.',
                1;
        END;

        IF SYSUTCDATETIME() >= @AccessExpiresAtUtc
        BEGIN
            ;THROW 50010,
                'El acceso venció. El sistema permanece disponible en modo solo lectura.',
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
            FROM dbo.DemoProcessRegistrations
                WITH (UPDLOCK, HOLDLOCK)
            WHERE ProcessId = @ProcessId
        )
        BEGIN
            DECLARE @RegisteredProcesses INT;

            SELECT
                @RegisteredProcesses = COUNT(*)
            FROM dbo.DemoProcessRegistrations
                WITH (UPDLOCK, HOLDLOCK);

            IF @RegisteredProcesses >= @MaxProcesses
            BEGIN
                DECLARE @ProcessLimitMessage NVARCHAR(2048);

                SET @ProcessLimitMessage = CONCAT(
                    'La edición actual permite monitorear como máximo ',
                    @MaxProcesses,
                    ' procesos.');

                ;THROW 50011,
                    @ProcessLimitMessage,
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
            FROM dbo.DemoWorkerRegistrations
                WITH (UPDLOCK, HOLDLOCK)
            WHERE WorkerId = @WorkerId
        )
        BEGIN
            DECLARE @RegisteredWorkers INT;

            SELECT
                @RegisteredWorkers = COUNT(*)
            FROM dbo.DemoWorkerRegistrations
                WITH (UPDLOCK, HOLDLOCK);

            IF @RegisteredWorkers >= @MaxWorkers
            BEGIN
                DECLARE @WorkerLimitMessage NVARCHAR(2048);

                SET @WorkerLimitMessage = CONCAT(
                    'La edición actual permite registrar como máximo ',
                    @MaxWorkers,
                    ' Workers o integraciones.');

                ;THROW 50012,
                    @WorkerLimitMessage,
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

/*
    Mantiene compatible el procedimiento utilizado actualmente
    por DemoProductAccessService.
*/
CREATE OR ALTER PROCEDURE dbo.sp_ProductAccess_ValidateDemoStart
    @ProcessId INT,
    @WorkerId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InstalledAtUtc DATETIME2(3);
    DECLARE @DemoDays INT;
    DECLARE @MaxProcesses INT;
    DECLARE @MaxWorkers INT;
    DECLARE @DemoExpiresAtUtc DATETIME2(3);

    SELECT
        @InstalledAtUtc = InstalledAtUtc,
        @DemoDays = DemoDays,
        @MaxProcesses = MaxProcesses,
        @MaxWorkers = MaxWorkers
    FROM dbo.ProductInstallation
    WHERE InstallationId = 1;

    IF @InstalledAtUtc IS NULL
    BEGIN
        ;THROW 50014,
            'No se encontró el estado de instalación de la Demo.',
            1;
    END;

    SET @DemoExpiresAtUtc = DATEADD(
        DAY,
        @DemoDays,
        @InstalledAtUtc);

    EXEC dbo.sp_ProductAccess_ValidateAndRegisterStart
        @ProcessId = @ProcessId,
        @WorkerId = @WorkerId,
        @MaxProcesses = @MaxProcesses,
        @MaxWorkers = @MaxWorkers,
        @AccessExpiresAtUtc = @DemoExpiresAtUtc;
END;
GO