USE SqlWorkflowMonitor_Dev;
GO

/* Inicia una nueva ejecución */
DROP PROCEDURE IF EXISTS dbo.sp_ProcessExecution_Start
GO
CREATE PROCEDURE dbo.sp_ProcessExecution_Start
    @ProcessId INT
AS
BEGIN
    SET NOCOUNT ON;

    /* Verifica que el proceso exista y esté activo */
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

    INSERT INTO dbo.ProcessExecutions
    (
        ProcessId,
        Status,
        StartedAt
    )
    VALUES
    (
        @ProcessId,
        'Running',
        SYSUTCDATETIME()
    );

    DECLARE @ExecutionId BIGINT;

    SET @ExecutionId =
        CAST(SCOPE_IDENTITY() AS BIGINT);

    /* Devuelve la ejecución creada */
    SELECT
        ExecutionId,
        ProcessId,
        Status,
        StartedAt,
        FinishedAt,
        DurationMs,
        ErrorMessage
    FROM dbo.ProcessExecutions
    WHERE ExecutionId = @ExecutionId;
END;
GO


/* Finaliza una ejecución */
DROP PROCEDURE IF EXISTS dbo.sp_ProcessExecution_Finish
GO
CREATE PROCEDURE dbo.sp_ProcessExecution_Finish
    @ExecutionId BIGINT,
    @Status VARCHAR(20),
    @ErrorMessage NVARCHAR(2000) = NULL,
    @TotalItems INT = NULL,
    @SucceededItems INT = NULL,
    @FailedItems INT = NULL,
    @AffectedRows INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    /* Solo acepta estados finales */
    IF @Status NOT IN
    (
        'Succeeded',
        'Failed',
        'Cancelled'
    )
    BEGIN
       ;THROW 50002,
              'El estado final no es válido.',
              1;
    END;

    /* Si falló, debe indicar el error */
    IF @Status = 'Failed'
       AND NULLIF
       (
           LTRIM(RTRIM(@ErrorMessage)),
           ''
       ) IS NULL
    BEGIN
       ; THROW 50003,
              'Una ejecución fallida debe informar el error.',
              1;
    END;

    UPDATE dbo.ProcessExecutions
    SET
        Status = @Status,
        FinishedAt = SYSUTCDATETIME(),
       ErrorMessage =
    CASE
        WHEN @Status = 'Failed'
            THEN @ErrorMessage
        ELSE NULL
    END,
        TotalItems = @TotalItems,
        SucceededItems = @SucceededItems,
        FailedItems = @FailedItems,
        AffectedRows = @AffectedRows
    WHERE ExecutionId = @ExecutionId
      AND Status = 'Running'
      AND FinishedAt IS NULL;

    /* Si no actualizó ninguna fila */
    IF @@ROWCOUNT = 0
    BEGIN
       ;THROW 50004,
              'La ejecución no existe o ya fue finalizada.',
              1;
    END;

    /* Devuelve la ejecución finalizada */
    SELECT
        ExecutionId,
        ProcessId,
        Status,
        StartedAt,
        FinishedAt,
        DurationMs,
        ErrorMessage,
        TotalItems,
        SucceededItems,
        FailedItems,
        AffectedRows
    FROM dbo.ProcessExecutions
    WHERE ExecutionId = @ExecutionId;
END;
GO
/* Lista ejecuciones que permanecen en Running demasiado tiempo */
/* Lista una cantidad limitada de ejecuciones */
DROP PROCEDURE IF EXISTS dbo.sp_ProcessExecution_List
GO
CREATE PROCEDURE dbo.sp_ProcessExecution_List
    @MaxRows INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxRows < 1
        SET @MaxRows = 1;

    IF @MaxRows > 500
        SET @MaxRows = 500;

    SELECT TOP (@MaxRows)
        e.ExecutionId,
        e.ProcessId,
        p.Name AS ProcessName,
        e.Status,
        e.StartedAt,
        e.FinishedAt,
        e.DurationMs
    FROM dbo.ProcessExecutions e
    INNER JOIN dbo.Processes p
        ON p.ProcessId = e.ProcessId
    ORDER BY e.ExecutionId DESC;
END;
GO

/* Lista ejecuciones que permanecen en Running demasiado tiempo */
DROP PROCEDURE IF EXISTS dbo.sp_ProcessExecution_ListStuck
GO
CREATE PROCEDURE dbo.sp_ProcessExecution_ListStuck
    @OlderThanMinutes INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    IF @OlderThanMinutes <= 0
    BEGIN
    ;THROW 50005,
       'La cantidad de minutos debe ser mayor que cero.',
       1;
    END;

    SELECT
        e.ExecutionId,
        e.ProcessId,
        p.Name AS ProcessName,
        e.Status,
        e.StartedAt,
        DATEDIFF_BIG
        (
            MINUTE,
            e.StartedAt,
            SYSUTCDATETIME()
        ) AS RunningMinutes
    FROM dbo.ProcessExecutions e
    INNER JOIN dbo.Processes p
        ON p.ProcessId = e.ProcessId
    WHERE e.Status = 'Running'
      AND e.FinishedAt IS NULL
      AND e.StartedAt <= DATEADD
      (
          MINUTE,
          -@OlderThanMinutes,
          SYSUTCDATETIME()
      )
    ORDER BY e.StartedAt;
END;
GO


USE SqlWorkflowMonitor_Dev;
GO

DROP PROCEDURE IF EXISTS dbo.sp_ProcessExecution_ListPaged
GO
CREATE PROCEDURE   dbo.sp_ProcessExecution_ListPaged
    @Status VARCHAR(20) = NULL,
    @ProcessId INT = NULL,
    @DateFrom DATE = NULL,
    @DateTo DATE = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @SortBy VARCHAR(20) = 'ExecutionId',
    @SortDirection VARCHAR(4) = 'DESC',
    @StaleThresholdMinutes INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
        SET @PageNumber = 1;

    IF @PageSize NOT IN (10, 25, 50)
        SET @PageSize = 10;

    IF @SortBy IS NULL
       OR @SortBy NOT IN
       (
           'ExecutionId',
           'ProcessName',
           'Status',
           'StartedAt',
           'FinishedAt',
           'DurationMs'
       )
        SET @SortBy = 'ExecutionId';

    IF @SortDirection IS NULL
       OR @SortDirection NOT IN ('ASC', 'DESC')
        SET @SortDirection = 'DESC';

    IF @StaleThresholdMinutes < 1
        SET @StaleThresholdMinutes = 10;

    DECLARE @TotalCount INT;
    DECLARE @RunningCount INT;
    DECLARE @StaleRunningCount INT;
    DECLARE @SucceededCount INT;
    DECLARE @FailedCount INT;
    DECLARE @AverageDurationMs BIGINT;
    DECLARE @TotalPages INT;
    DECLARE @Offset INT;

    SELECT
        @TotalCount = COUNT(*),
        @RunningCount = COALESCE(
            SUM(CASE WHEN e.Status = 'Running' THEN 1 ELSE 0 END),
            0),
        @StaleRunningCount = COALESCE(
            SUM(
                CASE
                    WHEN e.Status = 'Running'
                         AND e.FinishedAt IS NULL
                         AND e.StartedAt < DATEADD(
                             MINUTE,
                             -@StaleThresholdMinutes,
                             SYSUTCDATETIME())
                    THEN 1
                    ELSE 0
                END),
            0),
        @SucceededCount = COALESCE(
            SUM(CASE WHEN e.Status = 'Succeeded' THEN 1 ELSE 0 END),
            0),
        @FailedCount = COALESCE(
            SUM(CASE WHEN e.Status = 'Failed' THEN 1 ELSE 0 END),
            0),
        @AverageDurationMs =
            CONVERT(
                BIGINT,
                ROUND(
                    AVG(
                        CASE
                            WHEN e.Status = 'Succeeded'
                                 AND e.DurationMs IS NOT NULL
                            THEN CONVERT(DECIMAL(20, 2), e.DurationMs)
                        END),
                    0))
    FROM dbo.ProcessExecutions e
    WHERE
        (@Status IS NULL OR @Status = '' OR e.Status = @Status)
        AND (@ProcessId IS NULL OR e.ProcessId = @ProcessId)
        AND (@DateFrom IS NULL OR e.StartedAt >= @DateFrom)
        AND
        (
            @DateTo IS NULL
            OR e.StartedAt < DATEADD(
                DAY,
                1,
                CONVERT(DATETIME2, @DateTo))
        );

    SET @TotalPages =
        CASE
            WHEN @TotalCount = 0 THEN 0
            ELSE (@TotalCount + @PageSize - 1) / @PageSize
        END;

    IF @TotalPages > 0
       AND @PageNumber > @TotalPages
    BEGIN
        SET @PageNumber = @TotalPages;
    END;

    SET @Offset = (@PageNumber - 1) * @PageSize;

    /* Resultado 1: ejecuciones de la página */
    SELECT
        e.ExecutionId,
        e.ProcessId,
        p.Name AS ProcessName,
        e.Status,
        e.StartedAt,
        e.FinishedAt,
        e.DurationMs
    FROM dbo.ProcessExecutions e
    INNER JOIN dbo.Processes p
        ON p.ProcessId = e.ProcessId
    WHERE
        (@Status IS NULL OR @Status = '' OR e.Status = @Status)
        AND (@ProcessId IS NULL OR e.ProcessId = @ProcessId)
        AND (@DateFrom IS NULL OR e.StartedAt >= @DateFrom)
        AND
        (
            @DateTo IS NULL
            OR e.StartedAt < DATEADD(
                DAY,
                1,
                CONVERT(DATETIME2, @DateTo))
        )
    ORDER BY
        CASE
            WHEN @SortBy = 'ExecutionId'
                 AND @SortDirection = 'ASC'
            THEN e.ExecutionId
        END ASC,
        CASE
            WHEN @SortBy = 'ExecutionId'
                 AND @SortDirection = 'DESC'
            THEN e.ExecutionId
        END DESC,
        CASE
            WHEN @SortBy = 'ProcessName'
                 AND @SortDirection = 'ASC'
            THEN p.Name
        END ASC,
        CASE
            WHEN @SortBy = 'ProcessName'
                 AND @SortDirection = 'DESC'
            THEN p.Name
        END DESC,
        CASE
            WHEN @SortBy = 'Status'
                 AND @SortDirection = 'ASC'
            THEN e.Status
        END ASC,
        CASE
            WHEN @SortBy = 'Status'
                 AND @SortDirection = 'DESC'
            THEN e.Status
        END DESC,
        CASE
            WHEN @SortBy = 'StartedAt'
                 AND @SortDirection = 'ASC'
            THEN e.StartedAt
        END ASC,
        CASE
            WHEN @SortBy = 'StartedAt'
                 AND @SortDirection = 'DESC'
            THEN e.StartedAt
        END DESC,

        /* Finalizaciones NULL siempre al final */
        CASE
            WHEN @SortBy = 'FinishedAt'
                 AND e.FinishedAt IS NULL
            THEN 1
            ELSE 0
        END ASC,
        CASE
            WHEN @SortBy = 'FinishedAt'
                 AND @SortDirection = 'ASC'
            THEN e.FinishedAt
        END ASC,
        CASE
            WHEN @SortBy = 'FinishedAt'
                 AND @SortDirection = 'DESC'
            THEN e.FinishedAt
        END DESC,

        /* Duraciones NULL siempre al final */
        CASE
            WHEN @SortBy = 'DurationMs'
                 AND e.DurationMs IS NULL
            THEN 1
            ELSE 0
        END ASC,
        CASE
            WHEN @SortBy = 'DurationMs'
                 AND @SortDirection = 'ASC'
            THEN e.DurationMs
        END ASC,
        CASE
            WHEN @SortBy = 'DurationMs'
                 AND @SortDirection = 'DESC'
            THEN e.DurationMs
        END DESC,

        e.ExecutionId DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    /* Resultado 2: resumen de todos los registros filtrados */
    SELECT
        @TotalCount AS TotalCount,
        @PageNumber AS PageNumber,
        @PageSize AS PageSize,
        @TotalPages AS TotalPages,
        @RunningCount AS RunningCount,
        @StaleRunningCount AS StaleRunningCount,
        @SucceededCount AS SucceededCount,
        @FailedCount AS FailedCount,
        @AverageDurationMs AS AverageDurationMs;

    /* Resultado 3: procesos del combo */
    SELECT DISTINCT
        p.ProcessId,
        p.Name AS ProcessName
    FROM dbo.Processes p
    INNER JOIN dbo.ProcessExecutions e
        ON e.ProcessId = p.ProcessId
    ORDER BY p.Name;
END;
GO

/* Procesa clientes válidos de staging */
DROP PROCEDURE IF EXISTS dbo.sp_StagingCustomers_Process
GO
CREATE PROCEDURE dbo.sp_StagingCustomers_Process
    @ExecutionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Customers
        (
            Name,
            Email
        )
        SELECT
            Name,
            Email
        FROM dbo.StagingCustomers
        WHERE ExecutionId = @ExecutionId
          AND IsValid = 1;

        DECLARE @InsertedCustomers INT =
            @@ROWCOUNT;

        COMMIT TRANSACTION;

        SELECT
            @InsertedCustomers AS InsertedCustomers;
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
/* Lista todas las ejecuciones */

/* Obtiene el detalle de una ejecución */
DROP PROCEDURE IF EXISTS dbo.sp_ProcessExecution_GetById
GO
CREATE PROCEDURE dbo.sp_ProcessExecution_GetById
    @ExecutionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.ExecutionId,
        e.ProcessId,
        p.Name AS ProcessName,
        e.Status,
        e.StartedAt,
        e.FinishedAt,
        e.DurationMs,
        e.ErrorMessage,
        e.TotalItems,
        e.SucceededItems,
        e.FailedItems,
        e.AffectedRows
    FROM dbo.ProcessExecutions e
    INNER JOIN dbo.Processes p
        ON p.ProcessId = e.ProcessId
    WHERE e.ExecutionId = @ExecutionId;
END;
GO
