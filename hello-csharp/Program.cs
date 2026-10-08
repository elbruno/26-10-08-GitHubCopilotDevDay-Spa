// =============================================================================
// Hello world del GitHub Copilot SDK en C# (paquete NuGet GitHub.Copilot).
// -----------------------------------------------------------------------------
// El mismo programa existe en Python, Go, TypeScript y Rust en las carpetas
// hermanas. Se muestran los cinco seguidos para que la audiencia vea que el
// patron es siempre el mismo:
//
//   1. crear el cliente         2. arrancar el runtime
//   3. crear una sesion         4. enviar un prompt y esperar la respuesta
//
// Cambia la sintaxis, no el modelo mental.
// =============================================================================

using GitHub.Copilot;

// 1. CLIENTE. El SDK lanza el runtime de Copilot como proceso hijo y se
//    comunica con el por stdio. No hay HTTP ni claves en el codigo.
await using var client = new CopilotClient(new CopilotClientOptions
{
    Connection = RuntimeConnection.ForStdio("copilot"),
    WorkingDirectory = AppContext.BaseDirectory
});

// 2. START. Levanta el runtime y reutiliza la identidad de `copilot login`.
await client.StartAsync();

// 3. SESION. Un hello world arranca con todo apagado: sin descubrir
//    configuracion del disco, sin skills y sin operaciones de Git.
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    EnableConfigDiscovery = false,
    EnableSkills = false,
    EnableHostGitOperations = false
});

const string prompt = "Explica en una frase qué aporta GitHub Copilot SDK " +
    "para programadores de C#.";

// Se imprime la pregunta para que en pantalla quede claro que responde cada
// lenguaje a su propia version del prompt.
Console.WriteLine("Pregunta:");
Console.WriteLine(prompt);
Console.WriteLine();
Console.WriteLine("Respuesta:");

// 4. SEND AND WAIT. Atajo sincronico sobre el flujo de eventos: envia el turno
//    y espera el mensaje final. En las demos avanzadas se usan los eventos.
var response = await session.SendAndWaitAsync(
    new MessageOptions
    {
        Prompt = prompt
    },
    TimeSpan.FromSeconds(90));

Console.WriteLine(response?.Data.Content ?? "La sesión terminó sin respuesta.");
