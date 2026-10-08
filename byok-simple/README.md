# BYOK simple con Microsoft Foundry

Aplicación de consola .NET 10 que usa GitHub Copilot SDK con un modelo de
Microsoft Foundry. La configuración se lee desde .NET User Secrets.

## Configuración

```powershell
dotnet user-secrets set --project .\ByokSimpleConsole.csproj "Foundry:ModelName" "gpt-6.1-sol"
dotnet user-secrets set --project .\ByokSimpleConsole.csproj "Foundry:Endpoint" "https://<resource>.services.ai.azure.com"
```

La aplicación usa `AzureCliCredential` por defecto. Ejecuta `az login` antes
de iniciar la demo. Si agregas `Foundry:ApiKey`, usa la API key en lugar de
Azure CLI.

```powershell
dotnet user-secrets set --project .\ByokSimpleConsole.csproj "Foundry:ApiKey" "<optional-key>"
```

## Ejecución

```powershell
dotnet run --project .\ByokSimpleConsole.csproj
dotnet run --project .\ByokSimpleConsole.csproj -- "Explica BYOK en una frase."
```
