# Hello, GitHub Copilot SDK

Cinco primeras sesiones mínimas para comparar el mismo patrón en C#, Python,
Go, TypeScript y Rust. Están adaptadas del
[workshop oficial](https://github.com/github/copilot-sdk-workshop/tree/716bdaaf629817606873b4e22d551e900985c92c)
y fijan GitHub Copilot SDK `1.0.11`.

Cada ejemplo muestra primero `Pregunta:` y después `Respuesta:`. Pregunta qué
aporta el SDK específicamente a los programadores de su lenguaje, mantiene un
cliente, crea una sesión, envía un mensaje y libera los recursos. No incluye
tools ni permisos.

## Ejecutar los cinco ejemplos

Desde este directorio:

```powershell
.\Run-All.ps1
```

El script prepara las dependencias, ejecuta C#, Python, Go, TypeScript y Rust en
ese orden, y muestra cada resultado bajo un encabezado independiente. Son cinco
llamadas reales al modelo y pueden consumir cuota.

En Windows, Rust usa el toolchain `stable-x86_64-pc-windows-gnu` para no depender
de Visual Studio C++ Build Tools. El setup lo instala mediante `rustup`.

Para repetirlos después de la primera preparación:

```powershell
.\Run-All.ps1 -SkipSetup
```

Para preparar dependencias sin llamar al modelo:

```powershell
.\Run-All.ps1 -SetupOnly
```

## Ejecutar uno por uno

### C#

```powershell
dotnet restore .\csharp\CopilotSdkHello.csproj --locked-mode
dotnet run --no-restore --project .\csharp\CopilotSdkHello.csproj
```

### Python

```powershell
Set-Location .\python
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e .
.\.venv\Scripts\python.exe .\main.py
```

### Go

```powershell
Set-Location .\go
go mod download
go run .
```

### TypeScript

```powershell
Set-Location .\typescript
npm ci
npm start
```

### Rust

```powershell
rustup toolchain install stable-x86_64-pc-windows-gnu --profile minimal
cargo +stable-x86_64-pc-windows-gnu run --locked `
  --manifest-path .\rust\Cargo.toml
```
