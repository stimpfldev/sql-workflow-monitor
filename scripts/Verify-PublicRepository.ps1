[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$errors = [System.Collections.Generic.List[string]]::new()

function Get-RepositoryRelativePath {
    param([Parameter(Mandatory)][string]$FullPath)

    $rootWithSeparator =
        [IO.Path]::GetFullPath($repositoryRoot).TrimEnd(
            [char[]]@('\', '/')) +
        [IO.Path]::DirectorySeparatorChar

    $normalizedFullPath = [IO.Path]::GetFullPath($FullPath)

    if (-not $normalizedFullPath.StartsWith(
            $rootWithSeparator,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "La ruta no pertenece al repositorio: $FullPath"
    }

    return $normalizedFullPath.Substring($rootWithSeparator.Length)
}

function Add-VerificationError {
    param([Parameter(Mandatory)][string]$Message)

    $errors.Add($Message)
}

$forbiddenPatterns = @(
    "*.csproj.user",
    "*.pubxml.user",
    "*.lic",
    "*.pfx",
    "*.p12",
    "*.key",
    "*.zip",
    "*.7z"
)

foreach ($pattern in $forbiddenPatterns) {
    $matches = @(
        Get-ChildItem -Path $repositoryRoot -Recurse -File -Filter $pattern |
            Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' }
    )

    foreach ($match in $matches) {
        Add-VerificationError (
            "Archivo no publicable: " +
            (Get-RepositoryRelativePath $match.FullName))
    }
}

$privateRelativePaths = @(
    "Package.ps1",
    "PRIVATE-RELEASE.md",
    "scripts/Export-PublicRepository.ps1",
    "SqlWorkflowMonitor.LicenseGenerator",
    "PrivateKeys",
    "GeneratedLicenses"
)

foreach ($privateRelativePath in $privateRelativePaths) {
    if (Test-Path -LiteralPath (
            Join-Path $repositoryRoot $privateRelativePath)) {
        Add-VerificationError (
            "Componente privado presente: $privateRelativePath")
    }
}

$textExtensions = @(
    ".config",
    ".cs",
    ".csproj",
    ".json",
    ".md",
    ".pem",
    ".ps1",
    ".sql",
    ".slnx",
    ".txt",
    ".xml",
    ".yml",
    ".yaml"
)

$privateKeyMarkers = @(
    "BEGIN " + "PRIVATE KEY",
    "BEGIN RSA " + "PRIVATE KEY",
    "BEGIN ENCRYPTED " + "PRIVATE KEY"
)

foreach ($file in Get-ChildItem -Path $repositoryRoot -Recurse -File) {
    if ($file.FullName -match '[\\/]\.git[\\/]' -or
        $file.Length -gt 5MB -or
        $file.Extension.ToLowerInvariant() -notin $textExtensions) {
        continue
    }

    $content = [IO.File]::ReadAllText($file.FullName)

    foreach ($marker in $privateKeyMarkers) {
        if ($content.Contains($marker)) {
            Add-VerificationError (
                "Material de clave privada detectado: " +
                (Get-RepositoryRelativePath $file.FullName))
        }
    }
}

$appSettingsPath =
    Join-Path $repositoryRoot "SqlWorkflowMonitor/appsettings.json"
$appSettings =
    Get-Content -LiteralPath $appSettingsPath -Raw |
    ConvertFrom-Json

if (-not [string]::IsNullOrWhiteSpace($appSettings.Security.ApiKey)) {
    Add-VerificationError (
        "SqlWorkflowMonitor/appsettings.json contiene una API key.")
}

if (-not [string]::IsNullOrWhiteSpace(
        $appSettings.Security.Admin.Username) -or
    -not [string]::IsNullOrWhiteSpace(
        $appSettings.Security.Admin.PasswordHash) -or
    -not [string]::IsNullOrWhiteSpace(
        $appSettings.Security.Admin.PasswordSalt)) {
    Add-VerificationError (
        "SqlWorkflowMonitor/appsettings.json contiene credenciales administrativas.")
}

if ($appSettings.ConnectionStrings.WorkflowMonitor -notmatch
    'Encrypt=True;') {
    Add-VerificationError (
        "La conexión Web de producción debe exigir cifrado SQL.")
}

if ($appSettings.ConnectionStrings.WorkflowMonitor -match
    'TrustServerCertificate=True;') {
    Add-VerificationError (
        "La conexión Web de producción confía en certificados SQL sin validarlos.")
}

if ($appSettings.AllowedHosts -eq "*" -or
    [string]::IsNullOrWhiteSpace($appSettings.AllowedHosts)) {
    Add-VerificationError (
        "AllowedHosts debe ser explícito en la configuración pública.")
}

$kestrelUrl = [string]$appSettings.Kestrel.Endpoints.Http.Url
$kestrelUri = $null

if (-not [Uri]::TryCreate(
        $kestrelUrl,
        [UriKind]::Absolute,
        [ref]$kestrelUri) -or
    -not $kestrelUri.IsLoopback) {
    Add-VerificationError (
        "Kestrel debe escuchar únicamente en loopback por defecto.")
}

$workerSettingsPath =
    Join-Path $repositoryRoot "SqlWorkflowMonitor.Worker/appsettings.json"
$workerSettings =
    Get-Content -LiteralPath $workerSettingsPath -Raw |
    ConvertFrom-Json

if (-not [string]::IsNullOrWhiteSpace(
        $workerSettings.WorkflowMonitorApi.ApiKey)) {
    Add-VerificationError (
        "SqlWorkflowMonitor.Worker/appsettings.json contiene una API key.")
}

if ($workerSettings.ConnectionStrings.WorkflowMonitor -notmatch
    'Encrypt=True;') {
    Add-VerificationError (
        "La conexión Worker de producción debe exigir cifrado SQL.")
}

if ($workerSettings.ConnectionStrings.WorkflowMonitor -match
    'TrustServerCertificate=True;') {
    Add-VerificationError (
        "La conexión Worker de producción confía en certificados SQL sin validarlos.")
}

$workerBaseUri = $null

if (-not [Uri]::TryCreate(
        [string]$workerSettings.WorkflowMonitorApi.BaseUrl,
        [UriKind]::Absolute,
        [ref]$workerBaseUri) -or
    ($workerBaseUri.Scheme -eq [Uri]::UriSchemeHttp -and
     -not $workerBaseUri.IsLoopback)) {
    Add-VerificationError (
        "El Worker solo puede usar HTTP contra una dirección loopback.")
}

$buildPropsPath = Join-Path $repositoryRoot "Directory.Build.props"
[xml]$buildProps = Get-Content -LiteralPath $buildPropsPath -Raw
$productVersion = [string]$buildProps.Project.PropertyGroup.Version
$versionFilePath = Join-Path $repositoryRoot "VERSION.txt"
$versionFile = Get-Content -LiteralPath $versionFilePath -Raw

if ([string]::IsNullOrWhiteSpace($productVersion) -or
    $versionFile.Trim() -ne $productVersion) {
    Add-VerificationError (
        "VERSION.txt no coincide con Directory.Build.props.")
}

$expectedDocumentPaths = @(
    "Documentation/Source/Commercial/SqlWorkflowMonitor_Descripcion_Comercial_ES_$productVersion.docx",
    "Documentation/Source/Commercial/SqlWorkflowMonitor_Product_Overview_EN_$productVersion.docx",
    "Documentation/Source/Manuals/SqlWorkflowMonitor_Installation_and_User_Guide_EN_$productVersion.docx",
    "Documentation/Source/Manuals/SqlWorkflowMonitor_Manual_de_Instalacion_y_Uso_ES_$productVersion.docx"
)

foreach ($expectedDocumentPath in $expectedDocumentPaths) {
    if (-not (Test-Path -LiteralPath (
            Join-Path $repositoryRoot $expectedDocumentPath) -PathType Leaf)) {
        Add-VerificationError (
            "Documento de versión faltante: $expectedDocumentPath")
    }
}

$solutionPath = Join-Path $repositoryRoot "SqlWorkflowMonitor.slnx"
[xml]$solution = Get-Content -LiteralPath $solutionPath -Raw
$solutionPathNodes = @(
    $solution.SelectNodes('//*[@Path]')
)

foreach ($node in $solutionPathNodes) {
    $relativePath = [string]$node.Path

    if ($relativePath -match
        'LicenseGenerator|Package\.ps1|PRIVATE-RELEASE|Export-PublicRepository') {
        Add-VerificationError (
            "La solución pública referencia tooling privado: $relativePath")
    }

    if (-not (Test-Path -LiteralPath (
            Join-Path $repositoryRoot $relativePath))) {
        Add-VerificationError (
            "La solución referencia una ruta inexistente: $relativePath")
    }
}

& (Join-Path $PSScriptRoot "Build-InstallSql.ps1") -VerifyOnly

if ($errors.Count -gt 0) {
    throw "La verificación pública falló:`n- $($errors -join "`n- ")"
}

Write-Host "Repositorio público validado: configuración segura, versión y SQL sincronizados, sin secretos ni tooling privado."
