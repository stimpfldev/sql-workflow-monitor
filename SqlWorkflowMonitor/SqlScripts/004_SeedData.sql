USE SqlWorkflowMonitor_Dev;
GO

/* Inserta procesos iniciales solo si todavía no existen */
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Processes
    WHERE Name = 'Importación de clientes'
)
BEGIN
    INSERT INTO dbo.Processes
    (
        Name,
        Description,
        ProcessType
    )
    VALUES
    (
        'Importación de clientes',
        'Importa clientes desde un archivo CSV.',
        'Batch'
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Processes
    WHERE Name = 'Generación de reporte diario'
)
BEGIN
    INSERT INTO dbo.Processes
    (
        Name,
        Description,
        ProcessType
    )
    VALUES
    (
        'Generación de reporte diario',
        'Genera el resumen diario de operaciones.',
        'StoredProcedure'
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Processes
    WHERE Name = 'Sincronización con API externa'
)
BEGIN
    INSERT INTO dbo.Processes
    (
        Name,
        Description,
        ProcessType
    )
    VALUES
    (
        'Sincronización con API externa',
        'Sincroniza información con un proveedor externo.',
        'ApiIntegration'
    );
END;
GO

/* Muestra los datos insertados */
SELECT
    ProcessId,
    Name,
    Description,
    ProcessType,
    IsActive,
    CreatedAt
FROM dbo.Processes
ORDER BY ProcessId;
GO