"""Hello world del GitHub Copilot SDK en Python (paquete `copilot`).

Mismo patron que en C#, Go, TypeScript y Rust:
    1. crear el cliente   2. arrancar el runtime
    3. crear una sesion   4. enviar un prompt y leer la respuesta

Diferencia didactica: Python no usa un atajo "send and wait", sino que se
suscribe al flujo de EVENTOS de la sesion. Es exactamente lo que hacen por
dentro los otros lenguajes, y es el modelo que se usa en las demos avanzadas.
"""

import asyncio

from copilot import CopilotClient
from copilot.session_events import (
    AssistantMessageData,
    SessionErrorData,
    SessionIdleData,
)


async def main() -> None:
    prompt = (
        "Explica en una frase qué aporta GitHub Copilot SDK "
        "para programadores de Python."
    )
    print("Pregunta:")
    print(prompt)
    print()
    print("Respuesta:")

    # 1 y 2. El context manager crea el cliente y arranca el runtime; al salir
    # lo detiene aunque haya una excepcion.
    async with CopilotClient() as client:
        # 3. Sesion con la configuracion por defecto.
        async with await client.create_session() as session:
            # La sesion no devuelve un string: publica eventos. Hay que esperar
            # explicitamente la señal de "turno terminado".
            done = asyncio.Event()
            error: RuntimeError | None = None

            def on_event(event) -> None:
                nonlocal error
                match event.data:
                    # Mensaje completo del asistente.
                    case AssistantMessageData(content=content):
                        print(content)
                    # El runtime reporto un error: se guarda y se corta la espera.
                    case SessionErrorData(message=message):
                        error = RuntimeError(message)
                        done.set()
                    # Idle = el turno termino. Sin esto, el programa esperaria
                    # para siempre.
                    case SessionIdleData():
                        done.set()

            session.on(on_event)

            # 4. Enviar el turno y esperar a que la sesion quede inactiva.
            await session.send(prompt)
            await done.wait()
            if error is not None:
                raise error


if __name__ == "__main__":
    asyncio.run(main())
