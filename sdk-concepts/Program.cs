// =============================================================================
// GitHub Copilot SDK — conceptos en C#
// -----------------------------------------------------------------------------
// Este proyecto es el "deep dive" de la primera mitad de la sesion. Cada etapa
// (01..07) enciende UNA sola capacidad del SDK para que se vea aislada:
//
//   01 Streaming        02 System prompt   03 Tool local   04 Permisos
//   05 MCP              06 Skill local     07 WorkIQ seguro (chat interactivo)
//
// La idea central del SDK: el runtime de Copilot deja de ser un chat y pasa a
// ser una dependencia de MI aplicacion. Yo creo el cliente, yo configuro la
// sesion, yo decido que tools existen, yo apruebo cada permiso y yo valido la
// salida antes de mostrarla.
//
// Uso:
//   --self-test                                  valida la logica sin red
//   --preflight                                  comprueba runtime y modelos
//   --stage 01..07 --model ID                    ejecuta una etapa
//   --stage 07 --model ID --question "texto"     WorkIQ sin modo interactivo
// =============================================================================

using System.Text;
using GitHub.Copilot;
using CsharpSdkConcepts;

// Acentos y signos de pregunta se ven bien en la consola del directo.
// InputEncoding importa tanto como OutputEncoding: sin ella, lo que tecleo en
// el chat de la etapa 07 ("¿Cuántos...") llegaria corrupto al modelo.
Console.OutputEncoding = Encoding.UTF8;
try
{
    Console.InputEncoding = Encoding.UTF8;
}
catch (IOException)
{
    // Entrada redirigida (pipe): no hay consola donde fijar el encoding.
}

// --- Modo offline -----------------------------------------------------------
// Self-test no toca el runtime ni la red: es el plan B si falla la conectividad
// durante el stream. Valida la logica determinista de la aplicacion.
if (args is ["--self-test"])
{
    ConceptSelfTests.Run();
    return;
}

// --- Parseo minimo de argumentos --------------------------------------------
string? GetOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

var isPreflight = args is ["--preflight"];
var stageId = GetOption("--stage");
var modelId = GetOption("--model");
var question = GetOption("--question");

if (!isPreflight && (stageId is null || modelId is null))
{
    Console.Error.WriteLine(
        "Uso: --preflight | --self-test | --stage 01..07 --model ID [--question \"texto\"]");
    Environment.ExitCode = 2;
    return;
}

// Toda la demo corre bajo un token de cancelacion: si algo se cuelga en vivo,
// Ctrl+C corta de forma ordenada en lugar de dejar el runtime abierto.
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(30));
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    // -------------------------------------------------------------------------
    // 1. CLIENTE. CopilotClient levanta el runtime de Copilot como proceso hijo
    //    y habla con el por stdio. Es el unico punto de entrada del SDK.
    // -------------------------------------------------------------------------
    await using var client = new CopilotClient(new CopilotClientOptions
    {
        Connection = RuntimeConnection.ForStdio("copilot"),
        WorkingDirectory = AppContext.BaseDirectory
    });
    await client.StartAsync(cancellation.Token);

    // 2. AUTENTICACION. El SDK reutiliza la sesion de `copilot login`; la app
    //    nunca manipula tokens. Solo pregunta si ya hay identidad valida.
    var auth = await client.GetAuthStatusAsync(cancellation.Token);
    Console.WriteLine($"[auth] authenticated={auth.IsAuthenticated}");
    if (!auth.IsAuthenticated)
        throw new InvalidOperationException("Falta autenticacion. Ejecuta copilot login fuera de camara.");

    // 3. MODELOS. El catalogo lo publica el runtime, no la aplicacion.
    var models = await client.ListModelsAsync(cancellation.Token);
    if (isPreflight)
    {
        Console.WriteLine("[runtime] conectado; modelos disponibles:");
        foreach (var model in models) Console.WriteLine(model.Id);
        Console.WriteLine($"[workiq] plugin configurado={WorkIqSafety.PluginDirectoryIsConfigured()}");
        return;
    }

    var stage = ConceptStage.Find(stageId!);
    if (!models.Any(model => model.Id == modelId))
        throw new ArgumentException("Modelo no disponible. Consulta --preflight.");

    Console.WriteLine($"=== {stage.Id}: {stage.Title} | {modelId} ===");

    // -------------------------------------------------------------------------
    // 4. SESION. SessionConfig es el contrato de la conversacion: modelo,
    //    streaming, system prompt, tools, MCP, skills y politica de permisos.
    //    ConceptSessionFactory arma una configuracion distinta por etapa.
    // -------------------------------------------------------------------------
    await using var session = await client.CreateSessionAsync(
        ConceptSessionFactory.Create(stage, modelId!),
        cancellation.Token);

    if (stage.Id == "07")
    {
        // La etapa 07 es un chat: la misma sesion responde varias preguntas,
        // conservando contexto entre turnos.
        await RunWorkIqChatAsync(session, question, cancellation.Token);
    }
    else
    {
        // El resto de las etapas son de un solo turno: prompt fijo y respuesta.
        Console.WriteLine("Pregunta:");
        Console.WriteLine(stage.Prompt);
        Console.WriteLine();
        Console.WriteLine("Respuesta:");
        await ConceptStreamer.RunAsync(session, stage.Prompt, printContent: true, cancellation.Token);
    }

    Console.WriteLine();
    Console.WriteLine($"[verified] stage={stage.Id}");
}
catch (Exception error) when (error is InvalidOperationException or ArgumentException or
    IOException or TimeoutException or OperationCanceledException or System.Text.Json.JsonException)
{
    // Fail-closed: cualquier fallo se reporta, nunca se "rellena" con texto.
    Console.Error.WriteLine($"[ERROR] {error.GetType().Name}: {error.Message}");
    Environment.ExitCode = 1;
}

// =============================================================================
// Chat WorkIQ: el presentador escribe la pregunta en vivo.
// -----------------------------------------------------------------------------
// Punto didactico: lo que cambia entre turnos es la PREGUNTA, no el CONTRATO.
// La aplicacion siempre exige el mismo JSON agregado y lo valida con codigo
// determinista, asi que ninguna pregunta puede hacer que se filtre contenido.
// =============================================================================
async Task RunWorkIqChatAsync(CopilotSession workIqSession, string? single, CancellationToken token)
{
    // Modo no interactivo: util para ensayos y para el preflight automatizado.
    if (!string.IsNullOrWhiteSpace(single))
    {
        await AskWorkIqAsync(workIqSession, single!, token);
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Chat WorkIQ seguro. Solo se muestran conteos agregados.");
    Console.WriteLine("Escribe tu pregunta, o 'salir' para terminar.");
    Console.WriteLine();
    Console.WriteLine("Preguntas sugeridas (copiar y pegar):");
    foreach (var suggestion in WorkIqSafety.SuggestedQuestions)
        Console.WriteLine($"  - {suggestion}");

    while (!token.IsCancellationRequested)
    {
        Console.WriteLine();
        Console.Write("WorkIQ> ");
        var input = Console.ReadLine();

        // Enter vacio o 'salir' cierran el chat de forma limpia.
        if (input is null) break;
        input = input.Trim();
        if (input.Length == 0 || input.Equals("salir", StringComparison.OrdinalIgnoreCase)) break;

        try
        {
            await AskWorkIqAsync(workIqSession, input, token);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or
            TimeoutException or System.Text.Json.JsonException)
        {
            // Un turno rechazado no derriba el chat: se explica y se sigue.
            Console.WriteLine($"[bloqueado] {error.Message}");
        }
    }
}

async Task AskWorkIqAsync(CopilotSession workIqSession, string userQuestion, CancellationToken token)
{
    // Primera barrera: la pregunta debe ser agregada. Si pide contenido textual
    // (asunto, remitente, cita), se rechaza antes de llamar al modelo.
    WorkIqSafety.EnsureAggregateQuestion(userQuestion);

    Console.WriteLine();
    Console.WriteLine($"Pregunta: {userQuestion}");
    Console.WriteLine("Respuesta:");

    // printContent: false evita mostrar el JSON crudo. Solo imprimimos lo que
    // ya paso por el validador.
    var raw = await ConceptStreamer.RunAsync(
        workIqSession,
        WorkIqSafety.BuildPrompt(userQuestion),
        printContent: false,
        token);

    // Segunda barrera: el host valida campos, categorias y rangos.
    WorkIqSafety.Print(WorkIqSafety.ParseAndValidate(raw));
}
