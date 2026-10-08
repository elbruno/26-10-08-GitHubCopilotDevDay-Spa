# GitHub Copilot SDK en C# | GitHub Copilot Dev Day 2026

Materiales para la sesión online en español **GitHub Copilot Dev Day 2026**. Este repositorio es independiente de cualquier repositorio de demos o workshop.

La sesión está pensada para aproximadamente **40 minutos**. La idea es entender
el modelo mental del GitHub Copilot SDK, comparar el hello world en cinco
lenguajes y recorrer progresivamente sus capacidades principales, sin ejecutar
el workshop completo.

El paso a paso para presentar la sesión está en
[`SPEAKER-RUNBOOK.md`](SPEAKER-RUNBOOK.md).

## Recorrido de 40 minutos

| Tiempo | Tema |
| --- | --- |
| 00-04 | Apertura y agenda. |
| 04-08 | El runtime de Copilot y qué aporta frente a llamar directamente a un modelo. |
| 08-13 | Demo 1, hello world en C#: cliente, runtime, sesión y prompt. |
| 13-21 | Qué incluye el harness y disponibilidad en seis lenguajes. Hello world comparado en cinco lenguajes. |
| 21-29 | El contrato runtime-aplicación, etapa por etapa: streaming, system prompt, tool local, permisos, MCP y skills. Permisos no equivalen a aislamiento. |
| 29-34 | Demo 2, BYOK: conectar una sesión del SDK con un modelo de Microsoft Foundry. |
| 34-40 | Preguntas, recursos y próximos pasos. |

El [workshop oficial de GitHub Copilot SDK](https://github.com/github/copilot-sdk-workshop)
se menciona como siguiente paso para quien quiera construir una aplicación más
completa, pero no se ejecuta durante esta sesión.

## Requisitos

- .NET 10 SDK
- GitHub Copilot CLI instalado y autenticado con `copilot login`
- Para el ejemplo BYOK: Azure CLI con `az login` o una API key de Microsoft Foundry

## Demo 1: hello world en C#

```powershell
dotnet restore .\hello-csharp\CopilotSdkHello.csproj --locked-mode
dotnet run --project .\hello-csharp\CopilotSdkHello.csproj --no-restore
```

El programa muestra el patrón mínimo:

1. Crear `CopilotClient`.
2. Arrancar el runtime local de Copilot.
3. Crear una sesión.
4. Enviar un prompt y esperar la respuesta.

## Hello world en cinco lenguajes

La carpeta [`examples/`](examples/) tiene el mismo patrón mínimo en C#, Python,
Go, TypeScript y Rust. Sirve para mostrar que el SDK es el mismo contrato en
todos los lenguajes. El ejemplo de C# es el mismo código que `hello-csharp`.

Preparar dependencias sin llamar al modelo:

```powershell
.\examples\Run-All.ps1 -SetupOnly
```

Ejecutar los cinco, uno detrás de otro:

```powershell
.\examples\Run-All.ps1 -SkipSetup
```

El detalle de cada lenguaje por separado está en
[`examples/README.md`](examples/README.md). Son cinco llamadas reales al modelo
y pueden consumir cuota.

## Paso a paso de las capacidades del SDK

La carpeta [`sdk-concepts/`](sdk-concepts/) enciende **una sola capacidad por
etapa**, para explicarlas aisladas:

| Etapa | Capacidad |
| --- | --- |
| 01 | Streaming de la respuesta. |
| 02 | System prompt. |
| 03 | Tool local definida en la aplicación. |
| 04 | Permisos, con aprobación y rechazo explícitos. |
| 05 | MCP, servidor de Wikipedia por stdio con lista blanca de tools. |
| 06 | Skill local en `sdk-concepts/Skills`. |
| 07 | WorkIQ con validación de salida, opcional. |

Preparar una vez, fuera de cámara:

```powershell
cd .\sdk-concepts\mcp
npm ci
cd ..
dotnet restore .\CsharpSdkConcepts.csproj --locked-mode
dotnet build .\CsharpSdkConcepts.csproj --no-restore
```

Comprobar sin llamar al modelo:

```powershell
dotnet run --no-build --project .\sdk-concepts\CsharpSdkConcepts.csproj -- --self-test
dotnet run --no-build --project .\sdk-concepts\CsharpSdkConcepts.csproj -- --preflight
```

Ejecutar una etapa en vivo:

```powershell
dotnet run --no-build --project .\sdk-concepts\CsharpSdkConcepts.csproj -- --stage 01 --model gpt-5.4-mini
```

Cambiar `--stage` por `02`, `03`, `04`, `05` o `06` según la capacidad que toque.
La etapa `07` necesita el plugin de WorkIQ configurado por variable de entorno y
no es parte del recorrido fijo.

Si el tiempo aprieta, el mínimo recomendado es `01`, `03` y `04`. La etapa `05`
es la que mejor explica MCP si queda margen.

## Demo 2: BYOK con Microsoft Foundry

El ejemplo `byok-simple` lee estos valores desde .NET User Secrets:

- `Foundry:ModelName`
- `Foundry:Endpoint`
- `Foundry:ApiKey`, opcional

Por defecto usa `AzureCliCredential`. Cuando existe `Foundry:ApiKey`, usa esa
API key en su lugar.

```powershell
dotnet user-secrets set --project .\byok-simple\ByokSimpleConsole.csproj "Foundry:ModelName" "gpt-6.1-sol"
dotnet user-secrets set --project .\byok-simple\ByokSimpleConsole.csproj "Foundry:Endpoint" "https://<resource>.services.ai.azure.com"

az login
dotnet run --project .\byok-simple\ByokSimpleConsole.csproj
```

Para usar una API key:

```powershell
dotnet user-secrets set --project .\byok-simple\ByokSimpleConsole.csproj "Foundry:ApiKey" "<optional-key>"
```

No guardes endpoints privados, tokens, API keys ni resultados de ensayo en el
repositorio.

## Slides

El deck reducido está en [`slides/GitHub Copilot SDK - Spanish - v05.pptx`](slides/GitHub%20Copilot%20SDK%20-%20Spanish%20-%20v05.pptx).
Las notas del orador siguen el recorrido de la tabla anterior, pero la v05 no
fija una duración ni un ritmo obligatorio.
Las versiones [`v01`](slides/GitHub%20Copilot%20SDK%20-%20Spanish%20-%2040min-v01.pptx)
[`v02`](slides/GitHub%20Copilot%20SDK%20-%20Spanish%20-%2040min-v02.pptx) y
[`v03`](slides/GitHub%20Copilot%20SDK%20-%20Spanish%20-%2040min-v03.pptx) se
conservan como referencia. La v04 y la v05 adaptan la portada, agenda y cierre
al template `GitHub Copilot Dev Days Template.pptx`; la v05 elimina las
referencias de tiempo para permitir un ritmo más rápido.

## Referencias

- [GitHub Copilot SDK](https://github.com/github/copilot-sdk)
- [GitHub Copilot SDK workshop](https://github.com/github/copilot-sdk-workshop)
- [Generative AI for Beginners .NET](https://github.com/microsoft/Generative-AI-for-beginners-dotnet)
- [Building Agentic AI Apps with GitHub Copilot SDK](https://gh.io/coursera)

Este repositorio contiene material de demostración para una sesión en vivo. Las
respuestas de los modelos pueden variar y las llamadas pueden consumir cuota.
