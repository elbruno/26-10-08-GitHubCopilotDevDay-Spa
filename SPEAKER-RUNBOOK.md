# Speaker runbook, GitHub Copilot Dev Day 2026

Guía de 40 minutos para presentar GitHub Copilot SDK en español.

## Antes de comenzar

Abrir:

1. PowerPoint con `slides\GitHub Copilot SDK - Spanish - 40min-v04.pptx`.
2. VS Code en `D:\events\26-10-08-GitHubCopilotDevDay-Spa`.
3. Una terminal PowerShell en el mismo directorio.

Preparar dependencias sin llamar al modelo:

```powershell
.\examples\Run-All.ps1 -SetupOnly

Set-Location .\sdk-concepts\mcp
npm ci
Set-Location ..\..

dotnet restore .\sdk-concepts\CsharpSdkConcepts.csproj --locked-mode
dotnet build .\sdk-concepts\CsharpSdkConcepts.csproj --no-restore
dotnet run --no-build --project .\sdk-concepts\CsharpSdkConcepts.csproj -- --self-test
dotnet run --no-build --project .\sdk-concepts\CsharpSdkConcepts.csproj -- --preflight
```

Comprobar también:

```powershell
az account show
dotnet user-secrets list --project .\byok-simple\ByokSimpleConsole.csproj
```

No mostrar valores de User Secrets en pantalla durante la sesión.

## 00:00-04:00, apertura

Slides 1 y 2.

Mensaje principal:

> GitHub Copilot SDK permite incorporar el runtime de Copilot dentro de una
> aplicación. La aplicación sigue controlando capacidades, permisos, datos y
> validación.

Explicar el recorrido:

1. Modelo mental del runtime.
2. Hello world.
3. El mismo patrón en cinco lenguajes.
4. Capacidades progresivas del SDK.
5. BYOK con Microsoft Foundry.

## 04:00-08:00, modelo mental

Slide 3.

Recorrer el diagrama de izquierda a derecha:

1. La aplicación crea el cliente.
2. El cliente inicia o conecta con el runtime.
3. La aplicación crea una sesión.
4. El runtime coordina modelo, tools y eventos.
5. La aplicación autoriza acciones y valida resultados.

## 08:00-13:00, hello world en C#

Abrir `hello-csharp\Program.cs`.

Señalar:

1. `CopilotClient`.
2. Inicio del runtime.
3. Creación de la sesión.
4. Envío del prompt.
5. Espera de la respuesta y liberación de recursos.

Ejecutar:

```powershell
dotnet run --project .\hello-csharp\CopilotSdkHello.csproj
```

Si el runtime tarda, continuar explicando el código mientras inicia.

## 13:00-21:00, harness y cinco lenguajes

Slides 4, 5 y 6.

Mostrar las carpetas:

```text
examples\csharp
examples\python
examples\go
examples\typescript
examples\rust
```

Abrir brevemente el archivo principal de dos o tres lenguajes y comparar el
mismo patrón: cliente, sesión, mensaje, respuesta y cierre.

El SDK también está disponible para Java. Este repositorio compara cinco
implementaciones ejecutables.

La ejecución de los cinco ejemplos es opcional porque realiza cinco llamadas
reales al modelo:

```powershell
.\examples\Run-All.ps1 -SkipSetup
```

Para proteger el tiempo, ejecutar el conjunto completo solo si ya se preparó y
la sesión va adelantada.

## 21:00-29:00, capacidades progresivas

Slide 7.

Abrir `sdk-concepts\Program.cs` y explicar que cada etapa habilita una sola
capacidad:

| Etapa | Archivo o capacidad | Idea que explicar |
| --- | --- | --- |
| 01 | Streaming | La aplicación recibe eventos parciales. |
| 02 | System prompt | Las instrucciones guían, pero no conceden permisos. |
| 03 | Tool local | La aplicación define qué función puede invocar el agente. |
| 04 | Permisos | Cada acción sensible se aprueba o rechaza explícitamente. |
| 05 | MCP | El host registra un servidor y limita las tools disponibles. |
| 06 | Skill local | La sesión carga instrucciones reutilizables desde archivos. |
| 07 | WorkIQ opcional | Datos reales requieren validación y alcance mínimo. |

Comando base:

```powershell
dotnet run --no-build --project .\sdk-concepts\CsharpSdkConcepts.csproj -- --stage 01 --model gpt-5.4-mini
```

Cambiar `01` por la etapa que corresponda. En 40 minutos, el recorrido
recomendado es:

1. Ejecutar `01` para mostrar streaming.
2. Ejecutar `03` para mostrar una tool local.
3. Ejecutar `04` para mostrar permisos.
4. Ejecutar `05` solo si queda tiempo para MCP.

No ejecutar WorkIQ como parte del recorrido fijo.

## 26:00-29:00, permisos no son aislamiento

Slide 8.

Mensaje principal:

> Los permisos deciden qué puede pedir el agente. El aislamiento decide dónde
> se ejecuta. Son capas diferentes.

Si las etapas anteriores consumieron más tiempo, usar esta slide como resumen
de 60 segundos y avanzar a BYOK.

## 29:00-34:00, BYOK con Microsoft Foundry

Abrir `byok-simple\Program.cs`.

Señalar:

1. `Foundry:ModelName` y `Foundry:Endpoint` se leen desde User Secrets.
2. `Foundry:ApiKey` es opcional.
3. Sin API key se usa `AzureCliCredential`.
4. Con API key se usa esa credencial.
5. `ProviderConfig` conecta la sesión del SDK con Foundry.

Ejecutar:

```powershell
dotnet run --project .\byok-simple\ByokSimpleConsole.csproj
```

No imprimir tokens, endpoints privados ni API keys.

## 34:00-40:00, recursos y cierre

Slides 9 y 10.

Recursos:

- <https://github.com/github/copilot-sdk>
- <https://github.com/github/copilot-sdk-workshop>
- <https://gh.io/coursera>

Cerrar con:

> El runtime orquesta. La aplicación define capacidades, autoriza acciones y
> valida resultados.

## Plan de recuperación

Si falla una llamada al modelo:

1. Explicar el archivo abierto y el resultado esperado.
2. Ejecutar `--self-test` para demostrar la validación local.
3. Continuar con la siguiente etapa sin reintentar repetidamente.

Si falta tiempo:

1. Mantener el hello world en C#.
2. Mostrar los cinco lenguajes sin ejecutar `Run-All.ps1`.
3. Ejecutar solo las etapas 03 y 04.
4. Mantener BYOK.
5. Cerrar antes del minuto 40.
