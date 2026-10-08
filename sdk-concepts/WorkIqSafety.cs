// =============================================================================
// WorkIqSafety — la capa de confianza que pone la APLICACION, no el modelo.
// -----------------------------------------------------------------------------
// WorkIQ es un plugin MCP real conectado a Microsoft 365 (correo, calendario,
// Teams). En un stream publico no se puede mostrar nada de eso. Este archivo
// concentra las cuatro barreras que hacen la demo presentable:
//
//   1. Carga explicita del plugin desde WORKIQ_PLUGIN_DIR (nunca una ruta fija).
//   2. Validacion de la PREGUNTA: solo se admiten preguntas agregadas.
//   3. System prompt con un contrato JSON cerrado y categorias permitidas.
//   4. Validacion determinista de la RESPUESTA antes de imprimir nada.
//
// Mensaje para la audiencia: el modelo puede equivocarse o ser manipulado; el
// host es quien decide que se muestra. Esto es fail-closed, no "confia y reza".
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CsharpSdkConcepts;

/// <summary>Categoria agregada: solo un nombre permitido y un conteo acotado.</summary>
public sealed record WorkIqCategory(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("count")] int Count);

/// <summary>Unica forma de salida que la aplicacion acepta mostrar en pantalla.</summary>
public sealed record WorkIqSummary(
    [property: JsonPropertyName("totalItems")] int TotalItems,
    [property: JsonPropertyName("categories")] IReadOnlyList<WorkIqCategory> Categories,
    [property: JsonPropertyName("urgentCount")] int UrgentCount,
    [property: JsonPropertyName("overlapCount")] int OverlapCount);

public static class WorkIqSafety
{
    // Lista blanca de categorias. Cualquier otro nombre bloquea la respuesta,
    // porque un nombre libre podria colar un proyecto, cliente o persona.
    private static readonly HashSet<string> AllowedCategories =
    [
        "reuniones",
        "seguimiento",
        "documentos",
        "administrativo",
        "otro"
    ];

    // Palabras que delatan una pregunta que pide CONTENIDO en lugar de conteos.
    // Se bloquea antes de gastar una llamada al modelo.
    private static readonly string[] ForbiddenQuestionTerms =
    [
        "asunto", "subject", "remitente", "quien", "quién", "de parte de",
        "texto", "cita", "citar", "resumen del correo", "contenido",
        "nombre", "nombres", "direccion", "dirección", "email de", "correo de",
        "mostrar el mensaje", "leer el mensaje", "transcribe", "transcribir",
        "url", "link", "enlace", "adjunto", "empresa", "cliente", "proyecto"
    ];

    /// <summary>Preguntas listas para copiar y pegar durante el directo.</summary>
    public static readonly IReadOnlyList<string> SuggestedQuestions =
    [
        "¿Cuántos temas de correo requieren atención en las últimas 24 horas?",
        "¿En qué categorías generales se agrupan mis temas recientes?",
        "¿Cuántos temas urgentes y cuántos de seguimiento tengo?",
        "¿Cuántas reuniones tengo mañana y cuántas se solapan?"
    ];

    // El system prompt se reemplaza por completo (SystemMessageMode.Replace).
    // Define el objetivo, prohibe las tools de escritura y fija el contrato.
    public const string SystemMessage = """
        Eres un filtro de privacidad para una demo publica.
        Usa exclusivamente workiq-ask y nunca uses tools de escritura.
        Responde exactamente al origen y a la ventana temporal de la pregunta:
        correo, calendario o tareas; ultimas horas, hoy o manana.
        No sustituyas una consulta de calendario por correos recientes.
        No incluyas nombres, direcciones, dominios, asuntos, citas, fragmentos, URLs,
        nombres de empresas, proyectos ni ningun texto procedente de un mensaje.
        Devuelve solamente JSON sin Markdown con este contrato exacto:
        {"totalItems":0,"categories":[{"name":"reuniones","count":0}],"urgentCount":0,"overlapCount":0}
        Los unicos nombres de categoria permitidos son reuniones, seguimiento,
        documentos, administrativo y otro. Los valores deben ser enteros entre 0 y 20.
        Clasifica reuniones y coordinacion de agenda como reuniones; pedidos que
        necesitan respuesta o accion como seguimiento; revisiones o adjuntos como
        documentos; aprobaciones, gastos y tramites como administrativo. Usa otro
        solo cuando ninguna categoria anterior corresponda.
        overlapCount es la cantidad de elementos de calendario que participan en al
        menos un solapamiento dentro de la ventana pedida; usa 0 fuera del calendario.
        urgentCount es 0 cuando la pregunta no pide urgencia o prioridad.
        Las categorias no se repiten y la suma de sus conteos es totalItems.
        Cualquiera que sea la pregunta del usuario, responde siempre con ese mismo
        contrato agregado. Si la pregunta pide contenido textual, devuelve los conteos.
        """;

    /// <summary>Preflight: comprueba si hay plugin configurado, sin cargarlo.</summary>
    public static bool PluginDirectoryIsConfigured()
    {
        var path = Environment.GetEnvironmentVariable("WORKIQ_PLUGIN_DIR");
        return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
    }

    /// <summary>
    /// Resuelve el directorio del plugin WorkIQ. La ruta vive en una variable de
    /// entorno para que no quede escrita en el repositorio ni visible en pantalla.
    /// </summary>
    public static string GetPluginDirectory()
    {
        var path = Environment.GetEnvironmentVariable("WORKIQ_PLUGIN_DIR");
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "Define WORKIQ_PLUGIN_DIR con la ruta absoluta del plugin WorkIQ instalado.");
        }

        // Se verifica que realmente sea un plugin: manifiesto MCP y skills.
        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath) ||
            !File.Exists(Path.Combine(fullPath, ".mcp.json")) ||
            !Directory.Exists(Path.Combine(fullPath, "skills")))
        {
            throw new InvalidOperationException(
                "WORKIQ_PLUGIN_DIR no apunta a un plugin WorkIQ valido.");
        }

        return fullPath;
    }

    /// <summary>
    /// Barrera 2: rechaza preguntas que piden contenido textual. Es defensa en
    /// profundidad; aunque el modelo obedeciera, el validador tambien bloquearia.
    /// </summary>
    public static void EnsureAggregateQuestion(string question)
    {
        var normalized = question.Trim();
        if (normalized.Length is < 5 or > 200)
            throw new ArgumentException("La pregunta debe tener entre 5 y 200 caracteres.");

        var lowered = normalized.ToLowerInvariant();
        var hit = ForbiddenQuestionTerms.FirstOrDefault(term => lowered.Contains(term, StringComparison.Ordinal));
        if (hit is not null)
        {
            throw new ArgumentException(
                $"Pregunta rechazada: pide contenido ('{hit}'). Pregunta por cantidades o categorias.");
        }
    }

    /// <summary>
    /// Envuelve la pregunta del presentador con el recordatorio del contrato.
    /// El texto del usuario se transporta, no se ejecuta como configuracion.
    /// </summary>
    public static string BuildPrompt(string question) => $"""
        Invoca workiq-ask para responder esta pregunta de forma agregada:
        {question}

        Respeta literalmente el origen y la ventana temporal solicitados. Si pregunta
        por reuniones de manana, consulta el calendario de manana. Si pregunta por
        correos de las ultimas 24 horas, consulta solo ese correo y ese intervalo.
        Sigue el contrato JSON y las restricciones de privacidad del system prompt.
        No muestres ni repitas ningun dato original.
        """;

    /// <summary>
    /// Barrera 4: el host parsea y valida. Si algo no encaja, lanza y no se
    /// imprime nada. Esta es la diferencia entre "confiar" y "verificar".
    /// </summary>
    public static WorkIqSummary ParseAndValidate(string content)
    {
        // El modelo suele envolver el JSON en un bloque Markdown; se desenvuelve.
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd < 0 || lastFence <= firstLineEnd)
                throw new InvalidOperationException("WorkIQ no devolvio JSON valido.");
            trimmed = trimmed[(firstLineEnd + 1)..lastFence].Trim();
        }

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;

        // Lista blanca de campos: un campo extra seria una fuga de datos.
        var allowedFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "totalItems", "categories", "urgentCount", "overlapCount"
        };
        var actualFields = root.ValueKind == JsonValueKind.Object
            ? root.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
            : [];
        if (root.ValueKind != JsonValueKind.Object || !actualFields.SetEquals(allowedFields))
        {
            throw new InvalidOperationException("La respuesta no cumple el contrato de campos requerido.");
        }

        var summary = JsonSerializer.Deserialize<WorkIqSummary>(trimmed)
            ?? throw new InvalidOperationException("La respuesta agregada esta vacia.");
        ValidateCount(summary.TotalItems, "totalItems");
        ValidateCount(summary.UrgentCount, "urgentCount");
        ValidateCount(summary.OverlapCount, "overlapCount");
        if (summary.UrgentCount > summary.TotalItems ||
            summary.OverlapCount > summary.TotalItems)
        {
            throw new InvalidOperationException("Los subconteos no pueden superar totalItems.");
        }

        // Categorias: nombre en lista blanca, en minusculas y conteo acotado.
        // Las mayusculas se bloquean porque delatan un nombre propio.
        if (summary.Categories.Count > AllowedCategories.Count ||
            summary.Categories.Select(category => category.Name).Distinct().Count() != summary.Categories.Count ||
            summary.Categories.Any(category =>
                !AllowedCategories.Contains(category.Name) ||
                category.Name.Any(char.IsUpper) ||
                category.Count is < 0 or > 20) ||
            summary.Categories.Sum(category => category.Count) != summary.TotalItems)
        {
            throw new InvalidOperationException("La respuesta contiene categorias o conteos no permitidos.");
        }

        return summary;
    }

    /// <summary>Unico punto del programa autorizado a escribir datos de WorkIQ.</summary>
    public static void Print(WorkIqSummary summary)
    {
        Console.WriteLine($"Temas agregados: {summary.TotalItems}");
        Console.WriteLine($"Urgentes: {summary.UrgentCount}");
        Console.WriteLine($"Elementos con solapamiento: {summary.OverlapCount}");
        foreach (var category in summary.Categories.OrderByDescending(item => item.Count))
            Console.WriteLine($"- {category.Name}: {category.Count}");
    }

    private static void ValidateCount(int value, string field)
    {
        if (value is < 0 or > 20)
            throw new InvalidOperationException($"{field} queda fuera del rango seguro.");
    }
}
