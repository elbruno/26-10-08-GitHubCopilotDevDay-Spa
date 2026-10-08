// =============================================================================
// Hello world del GitHub Copilot SDK en Rust (crate github-copilot-sdk).
// -----------------------------------------------------------------------------
// Mismo patron que en C#, Python, Go y TypeScript:
//   1. crear/arrancar el cliente   2. crear una sesion
//   3. enviar un prompt            4. leer la respuesta
//
// Nota de la demo: en Windows este ejemplo usa el toolchain GNU
// (stable-x86_64-pc-windows-gnu); ver rust-toolchain.toml y Run-All.ps1.
// =============================================================================

use github_copilot_sdk::session_events::AssistantMessageData;
use github_copilot_sdk::types::SessionConfig;
use github_copilot_sdk::{Client, ClientOptions};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    // El prompt ancla el contexto a proposito: sin la primera frase, el modelo
    // llega a negar que exista un SDK de Rust mientras lo esta ejecutando.
    // Buen ejemplo en vivo de por que el host debe dar contexto explicito.
    let prompt = "Este programa ya usa el crate oficial github-copilot-sdk 1.0.11. \
        Explica en una frase qué aporta GitHub Copilot SDK para programadores de Rust.";

    println!("Pregunta:");
    println!("{prompt}");
    println!();
    println!("Respuesta:");

    // 1. Cliente y runtime en un solo paso.
    let client = Client::start(ClientOptions::default()).await?;

    // 2. Sesion con la configuracion por defecto.
    let session = client.create_session(SessionConfig::default()).await?;

    // 3. Enviar el turno y esperar el mensaje final.
    let response = session.send_and_wait(prompt).await?;

    // 4. El evento es generico: typed_data devuelve Option, no Result. Si no es
    //    un mensaje del asistente, se muestra un texto neutro en vez de fallar.
    let content = response
        .as_ref()
        .and_then(|event| event.typed_data::<AssistantMessageData>())
        .map(|data| data.content)
        .unwrap_or_else(|| "(sin respuesta)".to_owned());
    println!("{content}");

    // Cerrar la sesion y detener el runtime.
    session.disconnect().await?;
    client.stop().await?;
    Ok(())
}
