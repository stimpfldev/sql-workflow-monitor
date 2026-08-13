USE SqlWorkflowMonitor_Dev;
GO

/*
    Identificador único y persistente utilizado para vincular
    una licencia con esta instalación.
*/
IF COL_LENGTH(
    'dbo.ProductInstallation',
    'LicenseInstallationId') IS NULL
BEGIN
    ALTER TABLE dbo.ProductInstallation
    ADD LicenseInstallationId UNIQUEIDENTIFIER NULL;
END;
GO

UPDATE dbo.ProductInstallation
SET LicenseInstallationId = NEWID()
WHERE LicenseInstallationId IS NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints defaultConstraint
    INNER JOIN sys.columns columnInfo
        ON columnInfo.default_object_id =
           defaultConstraint.object_id
    WHERE defaultConstraint.parent_object_id =
          OBJECT_ID('dbo.ProductInstallation')
      AND columnInfo.name = 'LicenseInstallationId'
)
BEGIN
    ALTER TABLE dbo.ProductInstallation
    ADD CONSTRAINT DF_ProductInstallation_LicenseInstallationId
        DEFAULT NEWID()
        FOR LicenseInstallationId;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id =
          OBJECT_ID('dbo.ProductInstallation')
      AND name = 'LicenseInstallationId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.ProductInstallation
    ALTER COLUMN LicenseInstallationId
        UNIQUEIDENTIFIER NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProductAccess_GetInstallationId
AS
BEGIN
    SET NOCOUNT ON;

    SELECT LicenseInstallationId
    FROM dbo.ProductInstallation
    WHERE InstallationId = 1;
END;
GO