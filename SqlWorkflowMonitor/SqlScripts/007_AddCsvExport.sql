USE SqlWorkflowMonitor_Dev;
GO

/* Exporta las ejecuciones usando los mismos filtros y orden del dashboard. */
CREATE OR ALTER PROCEDURE dbo.sp_ProcessExecution_Export
    @Status VARCHAR(20) = NULL,
    @ProcessId INT = NULL,
    @DateFrom DATE = NULL,
    @DateTo DATE = NULL,
    @SortBy VARCHAR(20) = 'ExecutionId',
 @SortDirection VARCHAR(4) = 'DESC',
@MaxRows INT = 5000
AS
BEGIN
    SET NOCOUNT ON;
    IF @MaxRows < 1
    SET @MaxRows = 1;

IF @MaxRows > 5000
    SET @MaxRows = 5000;
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
    BEGIN
        SET @SortBy = 'ExecutionId';
    END;

    IF @SortDirection IS NULL
       OR @SortDirection NOT IN ('ASC', 'DESC')
    BEGIN
        SET @SortDirection = 'DESC';
    END;

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
        e.ExecutionId DESC;
END;
GO
