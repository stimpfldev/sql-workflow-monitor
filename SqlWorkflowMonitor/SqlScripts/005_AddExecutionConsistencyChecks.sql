/* =========================================================
   Restricciones de consistencia para ProcessExecutions
   Ejecutar sobre la base SqlWorkflowMonitor_Dev
   ========================================================= */

USE SqlWorkflowMonitor_Dev;
GO


/* 1. Running no puede tener FinishedAt.
      Los estados finales deben tener FinishedAt. */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProcessExecutions_Status_FinishedAt'
      AND parent_object_id = OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    ALTER TABLE dbo.ProcessExecutions
    WITH CHECK
    ADD CONSTRAINT CK_ProcessExecutions_Status_FinishedAt
    CHECK
    (
        (
            Status = 'Running'
            AND FinishedAt IS NULL
        )
        OR
        (
            Status IN ('Succeeded', 'Failed', 'Cancelled')
            AND FinishedAt IS NOT NULL
        )
    );
END;
GO


/* 2. FinishedAt no puede ser anterior a StartedAt. */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProcessExecutions_DateRange'
      AND parent_object_id = OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    ALTER TABLE dbo.ProcessExecutions
    WITH CHECK
    ADD CONSTRAINT CK_ProcessExecutions_DateRange
    CHECK
    (
        FinishedAt IS NULL
        OR FinishedAt >= StartedAt
    );
END;
GO


/* 3. Failed debe tener ErrorMessage.
      Los demás estados no deben tenerlo. */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProcessExecutions_ErrorMessage'
      AND parent_object_id = OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    ALTER TABLE dbo.ProcessExecutions
    WITH CHECK
    ADD CONSTRAINT CK_ProcessExecutions_ErrorMessage
    CHECK
    (
        (
            Status = 'Failed'
            AND NULLIF(LTRIM(RTRIM(ErrorMessage)), '') IS NOT NULL
        )
        OR
        (
            Status <> 'Failed'
            AND ErrorMessage IS NULL
        )
    );
END;
GO