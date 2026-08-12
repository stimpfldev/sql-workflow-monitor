[CmdletBinding()]
param(
    [string]$AdminUsername = "admin"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$webProject = Join-Path $repositoryRoot "SqlWorkflowMonitor/SqlWorkflowMonitor.csproj"
$workerProject = Join-Path $repositoryRoot "SqlWorkflowMonitor.Worker/SqlWorkflowMonitor.Worker.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró el comando dotnet. Instale el SDK de .NET 10."
}

if ([string]::IsNullOrWhiteSpace($AdminUsername)) {
    throw "AdminUsername no puede estar vacío."
}

$securePassword = Read-Host "Contraseña del administrador (mínimo 12 caracteres)" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)

try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)

    if ($password.Length -lt 12) {
        throw "La contraseña debe tener al menos 12 caracteres."
    }

    # INICIO CORRECCIÓN - Compatible con Windows PowerShell 5.1 y PowerShell 7
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $salt = New-Object byte[] 16
        $rng.GetBytes($salt)

        $apiKeyBytes = New-Object byte[] 48
        $rng.GetBytes($apiKeyBytes)
    }
    finally {
        $rng.Dispose()
    }
    # FIN CORRECCIÓN

    $derive = New-Object System.Security.Cryptography.Rfc2898DeriveBytes(
        $password,
        $salt,
        210000,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256
    )

    try {
        $passwordHash = $derive.GetBytes(32)
    }
    finally {
        $derive.Dispose()
    }

    $apiKey = [Convert]::ToBase64String($apiKeyBytes)

    $passwordHashBase64 = [Convert]::ToBase64String($passwordHash)
    $passwordSaltBase64 = [Convert]::ToBase64String($salt)

    dotnet user-secrets set "Security:ApiKey" $apiKey --project $webProject | Out-Null
    dotnet user-secrets set "Security:Admin:Username" $AdminUsername --project $webProject | Out-Null
    dotnet user-secrets set "Security:Admin:PasswordHash" $passwordHashBase64 --project $webProject | Out-Null
    dotnet user-secrets set "Security:Admin:PasswordSalt" $passwordSaltBase64 --project $webProject | Out-Null
    dotnet user-secrets set "WorkflowMonitorApi:ApiKey" $apiKey --project $workerProject | Out-Null

    Write-Host "Configuración de desarrollo guardada mediante .NET User Secrets."
    Write-Host "No se escribieron credenciales en archivos versionados."
}
finally {
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }

    if (Get-Variable password -ErrorAction SilentlyContinue) {
        $password = $null
    }
}
