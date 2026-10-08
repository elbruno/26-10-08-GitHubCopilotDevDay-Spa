# =============================================================================
# Run-All.ps1 — ejecuta el mismo hello world del Copilot SDK en los 5 lenguajes.
# -----------------------------------------------------------------------------
# Se usa en la slide 4 de la sesion: C#, Python, Go, TypeScript y Rust hacen lo
# mismo (cliente -> runtime -> sesion -> prompt) y cada uno pregunta por su
# propio lenguaje, asi que las respuestas son distintas pero el patron es igual.
#
#   -SetupOnly   prepara dependencias sin llamar al modelo (preflight seguro)
#   -SkipSetup   ejecuta asumiendo que ya se preparo todo
# =============================================================================

param(
    [switch]$SkipSetup,
    [switch]$SetupOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# UTF-8 explicito: sin esto los acentos del ejemplo de Python salen corruptos.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$env:PYTHONUTF8 = '1'

$examplesRoot = $PSScriptRoot

# rustup instala cargo/rustc en %USERPROFILE%\.cargo\bin, que no siempre esta
# en PATH en una sesion nueva de PowerShell.
$cargoBin = Join-Path $HOME '.cargo\bin'
if ((Test-Path -LiteralPath $cargoBin) -and ($env:PATH -notlike "*$cargoBin*")) {
    $env:PATH = "$cargoBin;$env:PATH"
}

if ($SkipSetup -and $SetupOnly) {
    throw '-SkipSetup y -SetupOnly no se pueden usar juntos.'
}

# Encabeza cada bloque y falla rapido: en vivo conviene detectar el error en el
# lenguaje exacto en lugar de seguir ejecutando los demas.
function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string]$Label,
        [Parameter(Mandatory)]
        [scriptblock]$Command
    )

    Write-Host ''
    Write-Host ('=' * 72)
    Write-Host $Label
    Write-Host ('=' * 72)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Label terminó con código $LASTEXITCODE."
    }
}

# El runtime de Copilot (`copilot`) es comun a los cinco lenguajes; el resto son
# las cadenas de herramientas de cada uno.
foreach ($command in @('copilot', 'dotnet', 'python', 'go', 'node', 'npm', 'rustup', 'rustc', 'cargo')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "$command no está disponible."
    }
}

if (-not $SkipSetup) {
    # Cada lenguaje instala el SDK con su gestor de paquetes nativo:
    # NuGet, pip, go mod, npm y cargo.
    Invoke-Checked 'Preparando C#' {
        & dotnet restore (Join-Path $examplesRoot 'csharp\CopilotSdkHello.csproj') --locked-mode
    }

    $pythonRoot = Join-Path $examplesRoot 'python'
    $pythonExecutable = Join-Path $pythonRoot '.venv\Scripts\python.exe'
    if (-not (Test-Path -LiteralPath $pythonExecutable)) {
        Invoke-Checked 'Creando el entorno virtual de Python' {
            & python -m venv (Join-Path $pythonRoot '.venv')
        }
    }
    Invoke-Checked 'Preparando Python' {
        & $pythonExecutable -m pip install --disable-pip-version-check -e $pythonRoot
    }

    Invoke-Checked 'Preparando Go' {
        & go -C (Join-Path $examplesRoot 'go') mod download
    }

    Invoke-Checked 'Preparando TypeScript' {
        & npm --prefix (Join-Path $examplesRoot 'typescript') ci --no-audit --no-fund
    }

    Invoke-Checked 'Preparando Rust' {
        # En Windows el toolchain MSVC falla si link.exe del PATH no es el de
        # Visual Studio; el toolchain GNU evita ese problema en el directo.
        & rustup toolchain install stable-x86_64-pc-windows-gnu --profile minimal
        if ($LASTEXITCODE -ne 0) {
            return
        }
        & cargo +stable-x86_64-pc-windows-gnu build --locked `
            --manifest-path (Join-Path $examplesRoot 'rust\Cargo.toml')
    }
}

if ($SetupOnly) {
    Write-Host ''
    Write-Host 'Los cinco ejemplos quedaron preparados; no se llamó al modelo.'
    return
}

# A partir de aqui SI se llama al modelo: cinco respuestas reales, una por
# lenguaje. Consume cuota, por eso el preflight usa -SetupOnly.
Invoke-Checked 'C#' {
    & dotnet run --no-restore --project (Join-Path $examplesRoot 'csharp\CopilotSdkHello.csproj')
}

$pythonExecutable = Join-Path $examplesRoot 'python\.venv\Scripts\python.exe'
if (-not (Test-Path -LiteralPath $pythonExecutable)) {
    throw 'Falta examples\python\.venv. Ejecuta Run-All.ps1 sin -SkipSetup.'
}
Invoke-Checked 'Python' {
    Push-Location (Join-Path $examplesRoot 'python')
    try {
        & $pythonExecutable .\main.py
    }
    finally {
        Pop-Location
    }
}

Invoke-Checked 'Go' {
    & go -C (Join-Path $examplesRoot 'go') run .
}

Invoke-Checked 'TypeScript' {
    & npm --prefix (Join-Path $examplesRoot 'typescript') start
}

Invoke-Checked 'Rust' {
    & cargo +stable-x86_64-pc-windows-gnu run --quiet --locked `
        --manifest-path (Join-Path $examplesRoot 'rust\Cargo.toml')
}

Write-Host ''
Write-Host 'Los cinco ejemplos finalizaron correctamente.'
