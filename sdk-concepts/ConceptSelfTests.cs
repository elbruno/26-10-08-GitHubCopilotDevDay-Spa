// =============================================================================
// ConceptSelfTests — el plan B de la demo y, de paso, una leccion.
// -----------------------------------------------------------------------------
// Si en el directo falla la red, la autenticacion o la cuota, `--self-test`
// sigue funcionando: no llama al modelo ni a WorkIQ. Ejercita solo la logica
// determinista del host.
//
// Lo importante no es que "pasen los tests", sino QUE se prueba: que la
// aplicacion rechaza salidas del modelo que no cumplen el contrato. Eso es lo
// que hace presentable una integracion con datos reales.
// =============================================================================

namespace CsharpSdkConcepts;

public static class ConceptSelfTests
{
    public static void Run()
    {
        var checks = 0;

        // El catalogo de etapas responde por id.
        Check(ConceptStage.Find("01").Title == "Streaming", "stage lookup", ref checks);
        Check(ConceptStage.Find("07").Title == "WorkIQ seguro", "workiq stage", ref checks);

        // Caso valido: respuesta agregada que cumple el contrato.
        var safe = WorkIqSafety.ParseAndValidate(
            """{"totalItems":3,"categories":[{"name":"seguimiento","count":3}],"urgentCount":1,"overlapCount":0}""");
        Check(safe.TotalItems == 3, "safe total", ref checks);
        Check(safe.Categories.Single().Name == "seguimiento", "safe category", ref checks);
        Check(safe.OverlapCount == 0, "safe overlap", ref checks);

        // Fuga por campo extra: aunque el modelo "ayude", el host lo bloquea.
        ExpectFailure(
            """{"totalItems":1,"categories":[{"name":"otro","count":1}],"urgentCount":0,"overlapCount":0,"subject":"privado"}""",
            "reject extra field",
            ref checks);

        // Fuga por nombre propio disfrazado de categoria.
        ExpectFailure(
            """{"totalItems":1,"categories":[{"name":"Proyecto secreto","count":1}],"urgentCount":0,"overlapCount":0}""",
            "reject unsafe category",
            ref checks);

        // Conteo fuera de rango: senal de que el modelo no siguio el contrato.
        ExpectFailure(
            """{"totalItems":99,"categories":[],"urgentCount":0,"overlapCount":0}""",
            "reject unbounded count",
            ref checks);

        // El contrato no admite omitir overlapCount ni inventar subtotales.
        ExpectFailure(
            """{"totalItems":1,"categories":[{"name":"otro","count":1}],"urgentCount":0}""",
            "reject missing field",
            ref checks);
        ExpectFailure(
            """{"totalItems":1,"categories":[{"name":"reuniones","count":1}],"urgentCount":0,"overlapCount":2}""",
            "reject impossible overlap",
            ref checks);

        // La pregunta del presentador tambien se valida: debe ser agregada.
        ExpectRejectedQuestion("¿Cual es el asunto del ultimo correo?", "reject content question", ref checks);
        ExpectRejectedQuestion("hola", "reject too short question", ref checks);

        // Las sugerencias que se muestran en vivo deben pasar su propio filtro.
        foreach (var suggestion in WorkIqSafety.SuggestedQuestions)
        {
            WorkIqSafety.EnsureAggregateQuestion(suggestion);
            checks++;
        }

        Console.WriteLine($"[self-test] PASS {checks} checks; sin llamadas al modelo ni a WorkIQ.");
    }

    /// <summary>Confirma que una salida insegura del modelo se rechaza.</summary>
    private static void ExpectFailure(string json, string name, ref int checks)
    {
        var rejected = false;
        try
        {
            WorkIqSafety.ParseAndValidate(json);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Check(rejected, name, ref checks);
    }

    /// <summary>Confirma que una pregunta que pide contenido se rechaza.</summary>
    private static void ExpectRejectedQuestion(string question, string name, ref int checks)
    {
        var rejected = false;
        try
        {
            WorkIqSafety.EnsureAggregateQuestion(question);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }

        Check(rejected, name, ref checks);
    }

    private static void Check(bool condition, string name, ref int checks)
    {
        if (!condition) throw new InvalidOperationException($"FAIL: {name}");
        checks++;
    }
}
