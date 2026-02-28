# Estrategia de Reemplazo de Tokens en Plantillas

## Por que existe este documento
Esta nota explica un bug puntual que podia generar nombres incorrectos sin avisar:

- el reemplazo secuencial de tokens (`Replace` en un loop) puede provocar reemplazos en cascada
- valores de metadata que contienen texto similar a tokens (por ejemplo `<Album>`) pueden alterarse sin querer

La implementacion actual en `TemplateRuleService` evita ese comportamiento.

## Resumen del problema
Dada una plantilla con varios tokens, el enfoque anterior hacia esto:

1. Empezar con el string de plantilla.
2. Reemplazar un token.
3. Reemplazar el siguiente token sobre el string ya modificado.
4. Continuar hasta procesar todos los tokens.

Esto es inseguro porque la salida del paso `n` pasa a ser entrada del paso `n+1`.

## Ejemplo concreto de falla
Entradas:

- Plantilla: `"<Title> - <Album>"`
- Title: `"Live <Album>"`
- Album: `"Greatest"`

Algoritmo secuencial anterior:

1. Reemplaza `<Title>` -> `"Live <Album> - <Album>"`
2. Reemplaza `<Album>` -> `"Live Greatest - Greatest"`

Intencion esperada:

- conservar el texto del titulo tal cual
- reemplazar solo los tokens reales de la plantilla

Nombre esperado antes de normalizacion de filename:

- `"Live <Album> - Greatest"`

El algoritmo anterior producia un resultado semantico incorrecto.

## Solucion actual
El fix usa una estrategia en dos fases:

1. Armar una vez un mapa token->valor segun reglas y politica de faltantes.
2. Aplicar reemplazos en una sola pasada sobre la plantilla original usando regex de tokens:
   - `<TrackNum>|<Title>|<Album>|<AlbumArtist>|<Artist>|<Year>`
   - cada match de regex se reemplaza desde el mapa
   - los valores insertados no se vuelven a escanear como tokens

Como los reemplazos se resuelven solo desde los matches originales, no hay cascada.

## Comportamiento con tags faltantes
El mapa de reemplazos se arma despues de validar reglas:

- modo `Strict`:
  - se registran los tokens requeridos faltantes
  - la evaluacion retorna `CanRename = false` antes de producir un nombre final
  - internamente igual se define valor vacio para simplificar el armado del mapa
- modo `UsePlaceholder`:
  - el token requerido faltante toma el placeholder (por ejemplo `Unknown`)

Asi se preservan las reglas actuales de renombrado y se corrige la exactitud del reemplazo.

## Ejemplo completo con comportamiento actual
Entradas:

- Plantilla: `"<Title> - <Album>"`
- Title: `"Live <Album>"`
- Album: `"Greatest"`

Resultado del reemplazo de una sola pasada:

- `"Live <Album> - Greatest"`

Luego de `FilenameFunctions.NormalizeFileName` (caracteres invalidos en Windows):

- `"Live _Album_ - Greatest"`

Ese resultado final es el esperado y esta cubierto por tests.

## Referencia en codigo
- Implementacion:
  - `RenameMusic.Core/Services/TemplateRuleService.cs`
- Test de regresion:
  - `RenameMusic.Tests/TemplateRuleServiceTests.cs`
  - test: `Evaluate_ShouldNotReplaceTagLikeTextInsideMetadataValue`
