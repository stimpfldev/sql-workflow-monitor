USE SqlWorkflowMonitor_Dev;
GO

/* Tabla que define los procesos monitoreados */
IF OBJECT_ID('dbo.Processes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Processes
    (
        ProcessId INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        ProcessType VARCHAR(30) NOT NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_Processes_IsActive DEFAULT 1,

        CreatedAt DATETIME2(3) NOT NULL
            CONSTRAINT DF_Processes_CreatedAt
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Processes
            PRIMARY KEY (ProcessId)
    );
END;
GO

/* Tabla que guarda cada ejecución de un proceso */
IF OBJECT_ID('dbo.ProcessExecutions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessExecutions
    (
        ExecutionId BIGINT IDENTITY(1,1) NOT NULL,
        ProcessId INT NOT NULL,
        Status VARCHAR(20) NOT NULL,
        StartedAt DATETIME2(3) NOT NULL,
        FinishedAt DATETIME2(3) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
                /* Métricas de la ejecución */
        TotalItems INT NULL,
        SucceededItems INT NULL,
        FailedItems INT NULL,
        AffectedRows INT NULL,
        /* Duración calculada automáticamente */
        DurationMs AS
        (
            CASE
                WHEN FinishedAt IS NULL THEN NULL
                ELSE DATEDIFF_BIG
                (
                    MILLISECOND,
                    StartedAt,
                    FinishedAt
                )
            END
        ),

        CONSTRAINT PK_ProcessExecutions
            PRIMARY KEY (ExecutionId),

        CONSTRAINT FK_ProcessExecutions_Processes
            FOREIGN KEY (ProcessId)
            REFERENCES dbo.Processes(ProcessId),

        CONSTRAINT CK_ProcessExecutions_Status
            CHECK
            (
                Status IN
                (
                    'Running',
                    'Succeeded',
                    'Failed',
                    'Cancelled'
                )
            )
    );
END;
GO

/* Índice para consultar ejecuciones por proceso y fecha */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProcessExecutions_ProcessId_StartedAt'
      AND object_id = OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    CREATE INDEX IX_ProcessExecutions_ProcessId_StartedAt
        ON dbo.ProcessExecutions
        (
            ProcessId,
            StartedAt DESC
        );
END;
GO

/* Índice para filtrar rápidamente por estado */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProcessExecutions_Status'
      AND object_id = OBJECT_ID('dbo.ProcessExecutions')
)
BEGIN
    CREATE INDEX IX_ProcessExecutions_Status
        ON dbo.ProcessExecutions(Status);
END;
GO
/* Tabla final de clientes procesados */
IF OBJECT_ID('dbo.Customers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId BIGINT IDENTITY(1,1) NOT NULL,

        Name NVARCHAR(150) NOT NULL,

        Email NVARCHAR(200) NOT NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Customers_CreatedAt
            DEFAULT SYSDATETIME(),

        CONSTRAINT PK_Customers
            PRIMARY KEY (CustomerId)
    );
END;
GO


/* Tabla temporal utilizada durante la importación */
IF OBJECT_ID('dbo.StagingCustomers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StagingCustomers
    (
        StagingCustomerId BIGINT IDENTITY(1,1) NOT NULL,

        ExecutionId BIGINT NOT NULL,

        Name NVARCHAR(150) NULL,

        Email NVARCHAR(200) NULL,

        IsValid BIT NOT NULL,

        ValidationError NVARCHAR(500) NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_StagingCustomers_CreatedAt
            DEFAULT SYSDATETIME(),

        CONSTRAINT PK_StagingCustomers
            PRIMARY KEY (StagingCustomerId),

        CONSTRAINT FK_StagingCustomers_ProcessExecutions
            FOREIGN KEY (ExecutionId)
            REFERENCES dbo.ProcessExecutions(ExecutionId)
    );
END;
GO