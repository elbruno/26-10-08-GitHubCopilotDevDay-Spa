// =============================================================================
// ConceptStage — catalogo de las 7 etapas del deep dive en C#.
// -----------------------------------------------------------------------------
// Cada etapa es (Id, Titulo, Prompt). El prompt esta escrito a proposito para
// que la capacidad que se esta enseñando sea EVIDENTE en pantalla: por ejemplo
// la 03 y la 04 obligan al modelo a invocar la tool local en lugar de responder
// de memoria, asi se ve el handler ejecutandose.
//
// La etapa 07 no tiene prompt fijo: la pregunta la escribe el presentador en
// vivo y se construye en WorkIqSafety.BuildPrompt.
// =============================================================================

namespace CsharpSdkConcepts;

public sealed record ConceptStage(string Id, string Title, string Prompt)
{
    private static readonly IReadOnlyDictionary<string, ConceptStage> Stages =
        new[]
        {
            // 01 — Streaming: Streaming = true y se ven los deltas llegando.
            new ConceptStage(
                "01",
                "Streaming",
                "Explica en tres frases breves por que una aplicacion puede querer streaming al usar Copilot SDK."),
            // 02 — System prompt: la misma pregunta, pero la app fija el tono,
            // el idioma y el formato. El control es de la aplicacion.
            new ConceptStage(
                "02",
                "System prompt",
                "Explica que es una sesion del Copilot SDK."),
            // 03 — Tool local: el modelo debe llamar a codigo C# tipado.
            new ConceptStage(
                "03",
                "Tool local",
                "Usa sdk_concept_lookup para explicar el concepto streaming. No respondas sin invocar la tool."),
            // 04 — Permisos: misma tool, pero ahora pasa por OnPermissionRequest
            // y se ve el [permission:allow-once] en pantalla.
            new ConceptStage(
                "04",
                "Permisos",
                "Usa sdk_concept_lookup para explicar el concepto permisos. No respondas sin invocar la tool."),
            // 05 — MCP: tool externa (Wikipedia) por protocolo estandar.
            new ConceptStage(
                "05",
                "MCP",
                "Usa Wikipedia para buscar Apollo 11 y responde con un unico dato historico verificable."),
            // 06 — Skill local: instrucciones reutilizables en SKILL.md que el
            // modelo respeta sin que el prompt las repita.
            new ConceptStage(
                "06",
                "Skill local",
                //"Explica como se combinan host, sesion, prompt y eventos en GitHub Copilot SDK."),

                "Explica como se combinan host, sesion, prompt y eventos en GitHub Copilot SDK, responde de forma borde y poco amigable."),
            // 07 — WorkIQ: integracion real con Microsoft 365, en modo chat.
            new ConceptStage(
                "07",
                "WorkIQ seguro",
                string.Empty)
        }.ToDictionary(stage => stage.Id, StringComparer.OrdinalIgnoreCase);

    public static ConceptStage Find(string id) =>
        Stages.TryGetValue(id, out var stage)
            ? stage
            : throw new ArgumentException("Etapa desconocida. Usa 01..07.");
}
