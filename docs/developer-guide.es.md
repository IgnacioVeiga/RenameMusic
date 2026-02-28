# Guía Para Desarrolladores de RenameMusic

## 1. Propósito
RenameMusic renombra archivos de música usando plantillas basadas en metadatos.

## 2. Vista general de la solución
La solución ahora tiene tres proyectos:

1. `RenameMusic` (UI WPF)
2. `RenameMusic.Core` (lógica de negocio)
3. `RenameMusic.Tests` (tests unitarios)

Ver [architecture.es.md](./architecture.es.md) para más detalle.

## 3. Tecnologías principales
- .NET 8
- WPF
- EF Core 8 + SQLite
- CommunityToolkit.Mvvm
- TagLib#
- xUnit

## 4. Flujos principales
- Añadir archivos
- Añadir carpetas
- Cargar sesión previa
- Aplicar regla de plantilla
- Renombrar todo o por ítem
- Resolver conflictos de nombre
- Mover ítems entre listas
- Borrar archivos/carpetas de forma segura (Papelera)

## 5. Comportamiento de reglas
- La plantilla debe contener al menos un tag soportado.
- Los metadatos requeridos se controlan con `MinTagsRequiredIndex`:
  - `None required`
  - `Only marked ones`
  - `All mentioned`
- Estrategia de tags faltantes:
  - `Strict`
  - `Use placeholder` (`Unknown`/localizado)

## 6. Persistencia
- Los datos de sesión se guardan en SQLite.
- Al iniciar se pregunta si restaurar sesión previa.
- Los archivos faltantes pasan a `Do Not Rename` con motivo.
- La ingesta guarda en lotes para listas grandes.

## 7. Conflictos
- Se mantiene el modal de conflictos (`RepeatedFile`).
- La política por defecto es configurable:
  - Ask
  - Replace
  - Skip
  - RenameWithNumber

## 8. Build y test
- El build WPF requiere Windows + .NET Desktop SDK.
- Core y tests se pueden ejecutar en múltiples plataformas.

Comandos:

```bash
dotnet restore RenameMusic.sln
dotnet test RenameMusic.Tests/RenameMusic.Tests.csproj -c Release
```

## 9. Mapa de documentación
- Arquitectura: [architecture.es.md](./architecture.es.md)
- Testing: [testing-guide.es.md](./testing-guide.es.md)
- Estrategia de reemplazo de tokens en plantillas: [template-token-replacement.es.md](./template-token-replacement.es.md)
- Guía EN: [developer-guide.md](./developer-guide.md)

## 10. Convenciones
- Mantener la lógica de negocio en `RenameMusic.Core`.
- Mantener code-behind de WPF al mínimo.
- Usar interfaces de servicios como límites.
- Agregar tests para reglas nuevas.
- Usar comentarios `TODO` en inglés cuando sea necesario.
