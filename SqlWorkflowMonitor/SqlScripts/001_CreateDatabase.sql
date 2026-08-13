USE master;
GO

IF DB_ID(N'SqlWorkflowMonitor_Dev') IS NULL
BEGIN
    CREATE DATABASE SqlWorkflowMonitor_Dev;
END;
GO