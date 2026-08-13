USE SqlWorkflowMonitor_Dev;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProductAccess_GetLicenseContext
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        LicenseInstallationId,
        SYSUTCDATETIME() AS CurrentUtc
    FROM dbo.ProductInstallation
    WHERE InstallationId = 1;
END;
GO