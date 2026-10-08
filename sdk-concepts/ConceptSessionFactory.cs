// =============================================================================
// ConceptSessionFactory — el corazon de la demo: SessionConfig.
// -----------------------------------------------------------------------------
// Aqui se ve que "configurar Copilot" es escribir un objeto de configuracion,
// no escribir un prompt mas largo. Base() parte de la postura mas restrictiva
// posible (sin tools, sin skills, sin git, permisos denegados) y cada etapa
// enciende exactamente UNA capacidad adicional.
//
// Ese patron -- empezar cerrado y abrir a proposito -- es el mensaje de
// seguridad de toda la sesion.
// =============================================================================

using System.ComponentModel;
using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using Microsoft.Extensions.AI;

namespace CsharpSdkConcepts;

#pragma warning disable GHCP001 // Las decisiones personalizadas son necesarias para enseñar permisos acotados.

public static class ConceptSessionFactory
{
    // El nombre de la tool es un contrato: aparece en el prompt, en
    // AvailableTools y en la comprobacion de permisos.
    private const string ToolName = "sdk_concept_lookup";

    public static SessionConfig Create(ConceptStage stage, string modelId)
    {
        var config = Base(stage, modelId);

        switch (stage.Id)
        {
            // 01 — STREAMING. Un solo flag cambia como llega la respuesta:
            // en vez de un mensaje final, la sesion emite deltas.
            case "01":
                config.Streaming = true;
                break;

            // 02 — SYSTEM PROMPT. Mode.Replace descarta el prompt de sistema por
            // defecto del runtime y deja solo el de la aplicacion. Asi la app
            // controla idioma, tono y formato sin tocar el prompt del usuario.
            case "02":
                config.Streaming = true;
                config.SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Replace,
                    Content = "Eres un instructor para principiantes. Responde en exactamente dos parrafos largos y en español y usando emojis."
                };
                break;

            // 03 — TOOL LOCAL. Tools registra la funcion C#; AvailableTools
            // limita que puede ver el modelo. skipPermission: true evita el
            // prompt de permiso para que se vea la tool pura, sin ruido.
            case "03":
                config.Tools = [CreateConceptTool(skipPermission: true)];
                config.AvailableTools = [ToolName];
                break;

            // 04 — PERMISOS. Misma tool que la 03, pero ahora SkipPermission es
            // false, asi que el runtime pide autorizacion y la aplicacion
            // responde en OnPermissionRequest. Se ve [permission:allow-once].
            case "04":
                config.Tools = [CreateConceptTool(skipPermission: false)];
                config.AvailableTools = [ToolName];
                config.OnPermissionRequest = LocalToolPermissionHandler();
                break;

            // 05 — MCP. La tool ya no vive en el proceso: es un servidor externo
            // que habla Model Context Protocol. El SDK lo lanza y lo conecta.
            case "05":
                config.Streaming = true;
                config.McpServers = new Dictionary<string, McpServerConfig>
                {
                    ["wikipedia"] = WikipediaServer()
                };
                config.AvailableTools = ["wikipedia-search", "wikipedia-readArticle"];
                config.OnPermissionRequest = WikipediaPermissionHandler();
                break;

            // 06 — SKILLS. Instrucciones reutilizables en Markdown (SKILL.md).
            // El prompt no las menciona y aun asi el modelo respeta su formato.
            case "06":
                config.EnableSkills = true;
                config.SkillDirectories = [SkillsDirectory()];
                break;

            // 07 — WORKIQ. Plugin MCP real con datos de Microsoft 365.
            case "07":
                ConfigureWorkIq(config);
                break;
        }

        return config;
    }

    /// <summary>
    /// Postura base: todo apagado. Si una etapa no enciende algo, no existe.
    /// Incluso el handler de permisos por defecto RECHAZA cualquier solicitud.
    /// </summary>
    private static SessionConfig Base(ConceptStage stage, string modelId) => new()
    {
        ClientName = $"csharp-sdk-concepts-{stage.Id}",
        Model = modelId,
        Streaming = false,
        EnableConfigDiscovery = false,   // no leer configuracion del disco
        EnableSkills = false,            // sin skills salvo que la etapa lo pida
        EnableHostGitOperations = false, // la demo nunca toca Git
        OnPermissionRequest = (_, _) => Task.FromResult(
            PermissionDecision.Reject("Esta etapa no autoriza tools."))
    };

    /// <summary>
    /// Una tool es simplemente una funcion C#. CopilotTool.DefineTool toma la
    /// firma y los [Description] y genera el esquema que ve el modelo. El tipado
    /// lo pone .NET: el modelo no puede inventar parametros.
    /// </summary>
    private static AIFunction CreateConceptTool(bool skipPermission) => CopilotTool.DefineTool(
        ([Description("Concepto: session, streaming, tool, permisos o MCP.")] string concept) =>
        {
            // Catalogo cerrado y de solo lectura: la tool no consulta la red ni
            // escribe nada. Asi es segura para mostrar en directo.
            var explanation = concept.Trim().ToLowerInvariant() switch
            {
                "session" => "Una sesion conserva configuracion, contexto y eventos de una conversacion.",
                "streaming" => "Streaming entrega deltas mientras el modelo genera la respuesta.",
                "tool" => "Una tool permite que el host ejecute codigo tipado y devuelva el resultado al modelo.",
                "permisos" or "permissions" => "El host decide cada operacion sensible antes de ejecutarla.",
                "mcp" => "MCP conecta tools externas mediante un protocolo estandar.",
                _ => "Concepto no incluido en el catalogo acotado de esta demo."
            };
            // Esta linea demuestra en pantalla que el codigo C# se ejecuto de verdad.
            Console.WriteLine($"\n[handler:local-tool] concept={concept}");
            return Task.FromResult(explanation);
        },
        toolOptions: new CopilotToolOptions { SkipPermission = skipPermission },
        factoryOptions: new AIFunctionFactoryOptions
        {
            Name = ToolName,
            Description = "Consulta un catalogo local y de solo lectura sobre conceptos del Copilot SDK."
        });

    /// <summary>
    /// Handler de permisos: se autoriza por TIPO y por NOMBRE, no por "si".
    /// ApproveOnce vale para una sola invocacion; no deja permiso persistente.
    /// </summary>
    private static Func<PermissionRequest, PermissionInvocation, Task<PermissionDecision>>
        LocalToolPermissionHandler() => (request, _) =>
        {
            var allowed = request is PermissionRequestCustomTool { ToolName: ToolName };
            Console.WriteLine($"\n[permission:{(allowed ? "allow-once" : "deny")}] {request.Kind}");
            return Task.FromResult(allowed
                ? PermissionDecision.ApproveOnce()
                : PermissionDecision.Reject("Esta etapa autoriza solamente la tool local."));
        };

    /// <summary>
    /// Servidor MCP local por stdio. Tools = lista blanca: aunque el servidor
    /// ofreciera mas operaciones, la sesion solo expone search y readArticle.
    /// </summary>
    private static McpStdioServerConfig WikipediaServer()
    {
        var server = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "mcp", "node_modules", "wikipedia-mcp", "dist", "index.js"));
        if (!File.Exists(server))
            throw new FileNotFoundException("Falta Wikipedia MCP. Ejecuta npm ci en sdk-concepts\\mcp.", server);

        return new McpStdioServerConfig
        {
            Command = "node",
            Args = [server],
            WorkingDirectory = Directory.GetCurrentDirectory(),
            Tools = ["search", "readArticle"]
        };
    }

    /// <summary>
    /// Para MCP la solicitud trae ServerName y ToolName: se comprueban los dos.
    /// Autorizar "el servidor wikipedia" entero seria demasiado amplio.
    /// </summary>
    private static Func<PermissionRequest, PermissionInvocation, Task<PermissionDecision>>
        WikipediaPermissionHandler() => (request, _) =>
        {
            var allowed = request is PermissionRequestMcp { ServerName: "wikipedia" } mcp &&
                mcp.ToolName is "search" or "readArticle" or "wikipedia-search" or "wikipedia-readArticle";
            Console.WriteLine($"\n[permission:{(allowed ? "allow-once" : "deny")}] {request.Kind}");
            return Task.FromResult(allowed
                ? PermissionDecision.ApproveOnce()
                : PermissionDecision.Reject("Solo se permiten busqueda y lectura en Wikipedia."));
        };

    /// <summary>Carpeta Skills\ del proyecto; contiene audience-friendly\SKILL.md.</summary>
    private static string SkillsDirectory() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "Skills"));

    /// <summary>
    /// WorkIQ es el caso mas delicado: datos reales de correo y calendario.
    /// La configuracion reduce la superficie al minimo antes de abrir nada.
    /// </summary>
    private static void ConfigureWorkIq(SessionConfig config)
    {
        // La ruta del plugin llega por variable de entorno, no por el repo.
        var pluginDirectory = WorkIqSafety.GetPluginDirectory();
        config.PluginDirectories = [pluginDirectory];
        config.EnableSkills = true;

        // De todas las tools de WorkIQ (leer, crear, actualizar, borrar) solo se
        // expone la operacion semantica de lectura.
        config.AvailableTools = ["workiq-ask"];

        // Sin streaming: queremos la respuesta COMPLETA para validarla antes de
        // imprimir nada. Con deltas, el texto crudo ya estaria en pantalla.
        config.Streaming = false;

        config.OnPermissionRequest = (request, _) =>
        {
            var allowed = request is PermissionRequestMcp { ServerName: "workiq" } mcp &&
                mcp.ToolName is "ask" or "workiq-ask";
            Console.WriteLine($"\n[permission:{(allowed ? "allow-once" : "deny")}] {request.Kind}");
            return Task.FromResult(allowed
                ? PermissionDecision.ApproveOnce()
                : PermissionDecision.Reject("La demo WorkIQ permite solamente la operacion semantica ask."));
        };

        // El system prompt fija el contrato JSON agregado (ver WorkIqSafety).
        config.SystemMessage = new SystemMessageConfig
        {
            Mode = SystemMessageMode.Replace,
            Content = WorkIqSafety.SystemMessage
        };
    }
}
