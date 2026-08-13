USE SqlWorkflowMonitor_Dev;
GO

/* Las métricas informadas no pueden ser negativas */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name =
        'CK_ProcessExecutions_MetricsNonNegative'
      AND parent_object_id =
          OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    ALTER TABLE dbo.ProcessExecutions
    WITH CHECK
    ADD CONSTRAINT CK_ProcessExecutions_MetricsNonNegative
    CHECK
    (
        (TotalItems IS NULL OR TotalItems >= 0)
        AND
        (SucceededItems IS NULL OR SucceededItems >= 0)
        AND
        (FailedItems IS NULL OR FailedItems >= 0)
        AND
        (AffectedRows IS NULL OR AffectedRows >= 0)
    );
END;
GO


/* Los elementos exitosos más los fallidos
   no pueden superar el total */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name =
        'CK_ProcessExecutions_MetricsWithinTotal'
      AND parent_object_id =
          OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    ALTER TABLE dbo.ProcessExecutions
    WITH CHECK
    ADD CONSTRAINT CK_ProcessExecutions_MetricsWithinTotal
    CHECK
    (
        TotalItems IS NULL
        OR
        (
            COALESCE(SucceededItems, 0)
            + COALESCE(FailedItems, 0)
            <= TotalItems
        )
    );
END;
GO