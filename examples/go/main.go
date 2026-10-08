// =============================================================================
// Hello world del GitHub Copilot SDK en Go (modulo github.com/github/copilot-sdk/go).
// -----------------------------------------------------------------------------
// Mismo patron que en C#, Python, TypeScript y Rust:
//   1. crear el cliente   2. arrancar el runtime
//   3. crear una sesion   4. enviar un prompt y esperar la respuesta
// =============================================================================

package main

import (
	"context"
	"fmt"

	copilot "github.com/github/copilot-sdk/go"
)

func main() {
	// 1. CLIENTE. LogLevel "error" mantiene la consola limpia para el directo.
	client := copilot.NewClient(&copilot.ClientOptions{LogLevel: "error"})

	// 2. START. Levanta el runtime de Copilot y reutiliza `copilot login`.
	if err := client.Start(context.Background()); err != nil {
		panic(err)
	}
	defer client.Stop()

	// 3. SESION. Configuracion por defecto: es solo un hello world.
	session, err := client.CreateSession(context.Background(), &copilot.SessionConfig{})
	if err != nil {
		panic(err)
	}
	defer session.Disconnect()

	prompt := "Explica en una frase qué aporta GitHub Copilot SDK para programadores de Go."
	fmt.Println("Pregunta:")
	fmt.Println(prompt)
	fmt.Println()
	fmt.Println("Respuesta:")

	// 4. SEND AND WAIT. Envia el turno y bloquea hasta el mensaje final.
	response, err := session.SendAndWait(context.Background(), copilot.MessageOptions{
		Prompt: prompt,
	})
	if err != nil {
		panic(err)
	}

	// La respuesta llega como un evento generico; hay que convertirla al tipo
	// concreto del mensaje del asistente.
	if response != nil {
		if message, ok := response.Data.(*copilot.AssistantMessageData); ok {
			fmt.Println(message.Content)
		}
	}
}
