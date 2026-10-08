// =============================================================================
// ConceptStreamer — como la aplicacion CONSUME la respuesta del SDK.
// -----------------------------------------------------------------------------
// Una sesion del Copilot SDK no devuelve un string: publica un flujo de eventos.
// Aqui se suscribe un unico handler y se traducen los eventos a pantalla:
//
//   AssistantMessageDeltaEvent  -> texto parcial (streaming)
//   AssistantMessageEvent       -> respuesta completa (sin streaming)
//   ToolExecutionStartEvent     -> el modelo decidio llamar a una tool
//   ToolExecutionCompleteEvent  -> la tool termino, con exito o no
//   SessionIdleEvent            -> el turno termino
//   SessionErrorEvent           -> el runtime reporto un error
//
// Punto clave para la audiencia: SessionIdleEvent es la señal de "ya termino".
// Sin el, la aplicacion no sabe cuando dejar de esperar.
// =============================================================================

using System.Text;
using GitHub.Copilot;

namespace CsharpSdkConcepts;

public static class ConceptStreamer
{
    /// <summary>
    /// Ejecuta un turno y devuelve el texto acumulado.
    /// <paramref name="printContent"/> en false permite capturar la respuesta
    /// sin mostrarla: lo usa WorkIQ para validar ANTES de imprimir.
    /// </summary>
    public static async Task<string> RunAsync(
        CopilotSession session,
        string prompt,
        bool printContent,
        CancellationToken cancellationToken)
    {
        var response = new StringBuilder();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedDelta = false;
        string? failedTool = null;

        // Una sola suscripcion para todos los eventos de la sesion.
        using var subscription = session.On<SessionEvent>(sessionEvent =>
        {
            switch (sessionEvent)
            {
                // Streaming: cada delta es un fragmento de texto recien generado.
                case AssistantMessageDeltaEvent delta when !string.IsNullOrEmpty(delta.Data.DeltaContent):
                    receivedDelta = true;
                    response.Append(delta.Data.DeltaContent);
                    if (printContent) Console.Write(delta.Data.DeltaContent);
                    break;

                // Sin streaming llega el mensaje completo. Si ya hubo deltas se
                // ignora, porque seria el mismo texto repetido.
                case AssistantMessageEvent message when !receivedDelta && !string.IsNullOrEmpty(message.Data.Content):
                    response.Clear();
                    response.Append(message.Data.Content);
                    if (printContent) Console.Write(message.Data.Content);
                    break;

                // Estas dos lineas son las que hacen visible el "agent loop":
                // se ve que el modelo pidio una tool y que la tool respondio.
                case ToolExecutionStartEvent tool:
                    Console.WriteLine($"\n[tool:start] {tool.Data.ToolName}");
                    break;
                case ToolExecutionCompleteEvent tool:
                    Console.WriteLine($"[tool:done] success={tool.Data.Success}");
                    if (!tool.Data.Success)
                        failedTool = tool.Data.ToolCallId;
                    break;

                case SessionIdleEvent:
                    completed.TrySetResult();
                    break;
                case SessionErrorEvent error:
                    completed.TrySetException(new InvalidOperationException(error.Data.Message));
                    break;
            }
        });

        // SendAsync solo envia el turno; el resultado llega por eventos.
        await session.SendAsync(new MessageOptions { Prompt = prompt }, cancellationToken);

        // Timeout defensivo: en un directo no se puede esperar indefinidamente.
        var finished = await Task.WhenAny(
            completed.Task,
            Task.Delay(TimeSpan.FromMinutes(3), cancellationToken));
        if (finished != completed.Task)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException("La respuesta alcanzo el limite de tiempo.");
        }

        await completed.Task;

        // Fail-closed: si una tool fallo, la respuesta del modelo pudo ser
        // inventada. Se descarta en lugar de mostrar algo plausible pero falso.
        if (failedTool is not null)
            throw new InvalidOperationException(
                "Una tool requerida fallo; la aplicacion bloqueo cualquier respuesta posterior.");

        if (printContent) Console.WriteLine();
        return response.ToString();
    }
}
