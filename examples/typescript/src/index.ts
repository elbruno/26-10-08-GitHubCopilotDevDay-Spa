// =============================================================================
// Hello world del GitHub Copilot SDK en TypeScript (paquete @github/copilot-sdk).
// -----------------------------------------------------------------------------
// Mismo patron que en C#, Python, Go y Rust:
//   1. crear el cliente   2. arrancar el runtime
//   3. crear una sesion   4. enviar un prompt y esperar la respuesta
//
// En Node el SDK es el mismo runtime que usan los demas lenguajes, por eso la
// respuesta y el comportamiento son equivalentes.
// =============================================================================

import { CopilotClient } from "@github/copilot-sdk";

// 1. CLIENTE y 2. START. Sin opciones: usa el runtime y la sesion de
//    `copilot login` ya existentes.
const client = new CopilotClient();
await client.start();

try {
  const prompt =
    "Explica en una frase qué aporta GitHub Copilot SDK " +
    "para programadores de TypeScript.";
  console.log("Pregunta:");
  console.log(prompt);
  console.log();
  console.log("Respuesta:");

  // 3. SESION. Configuracion vacia: solo un hello world.
  const session = await client.createSession({});
  try {
    // 4. SEND AND WAIT. Devuelve el evento del mensaje final del asistente.
    const response = await session.sendAndWait({
      prompt,
    });
    // El evento es generico; se comprueba que traiga contenido de texto.
    console.log(
      response?.data && "content" in response.data
        ? response.data.content
        : response,
    );
  } finally {
    // Cerrar la sesion y el cliente libera el proceso del runtime.
    await session.disconnect();
  }
} finally {
  await client.stop();
}
