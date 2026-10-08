# GitHub Copilot SDK en C# | GitHub Copilot Dev Day 2026

Materiales para la sesión online en español **GitHub Copilot Dev Day 2026**. Este repositorio es independiente de cualquier repositorio de demos o workshop.

La sesión está pensada para aproximadamente **40 minutos**. La idea es entender
el modelo mental del GitHub Copilot SDK y ejecutar dos ejemplos pequeños, sin
recorrer el workshop completo.

## Recorrido de 40 minutos

| Tiempo | Tema |
| --- | --- |
| 00-05 | Qué es el Copilot SDK y qué aporta frente a llamar directamente a un modelo. |
| 05-12 | Hello world en C#: cliente, runtime, sesión y prompt. |
| 12-22 | Conceptos para construir una aplicación: streaming, system prompt, tools, permisos y MCP. Se muestran en código y ejemplos breves. |
| 22-32 | BYOK: conectar una sesión del SDK con un modelo de Microsoft Foundry. |
| 32-40 | Preguntas, recursos y próximos pasos. |

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

El deck reducido está en [`slides/GitHub Copilot SDK - Spanish - 40min-v01.pptx`](slides/GitHub%20Copilot%20SDK%20-%20Spanish%20-%2040min-v01.pptx).

## Referencias

- [GitHub Copilot SDK](https://github.com/github/copilot-sdk)
- [GitHub Copilot SDK workshop](https://github.com/github/copilot-sdk-workshop)
- [Generative AI for Beginners .NET](https://github.com/microsoft/Generative-AI-for-beginners-dotnet)
- [Building Agentic AI Apps with GitHub Copilot SDK](https://gh.io/coursera)

Este repositorio contiene material de demostración para una sesión en vivo. Las
respuestas de los modelos pueden variar y las llamadas pueden consumir cuota.

