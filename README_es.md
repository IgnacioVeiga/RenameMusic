# RenameMusic (Beta)

RenameMusic es una app de escritorio para renombrar archivos de música usando plantillas de metadatos.

Idioma: [English](./README.md) / **Español**

## Qué hace
- Lee metadatos de audio (`mp3`, `m4a`, `ogg`, `flac`)
- Construye nombres destino desde una plantilla
- Separa ítems en `To Rename` y `Do Not Rename`
- Persiste la sesión en SQLite
- Resuelve conflictos de nombre con política configurable
- Soporta renombrado por ítem y por lote

## Estructura de la solución
- `RenameMusic` -> app WPF (UI)
- `RenameMusic.Core` -> dominio y lógica de negocio
- `RenameMusic.Tests` -> tests unitarios

## Requisitos
- Windows 10/11 recomendado para ejecutar la app WPF
- .NET SDK 8
- .NET Desktop Runtime 8 (para ejecutar la app)

## Build y test
Desde la raíz del repositorio:

```bash
# Restaurar todos los proyectos
dotnet restore RenameMusic.sln

# Ejecutar tests unitarios (multiplataforma)
dotnet test RenameMusic.Tests/RenameMusic.Tests.csproj -c Release
```

Build de la app WPF (solo Windows):

```bash
dotnet build RenameMusic/RenameMusic.csproj -c Release
```

## CI
GitHub Actions valida:
- restore
- build
- tests unitarios

(Las ramas `master` y `main` están excluidas por configuración del workflow.)

## Documentación
- Guía dev (EN): [docs/developer-guide.md](./docs/developer-guide.md)
- Guía dev (ES): [docs/developer-guide.es.md](./docs/developer-guide.es.md)
- Arquitectura (EN): [docs/architecture.md](./docs/architecture.md)
- Arquitectura (ES): [docs/architecture.es.md](./docs/architecture.es.md)
- Guía de testing (EN): [docs/testing-guide.md](./docs/testing-guide.md)
- Guía de testing (ES): [docs/testing-guide.es.md](./docs/testing-guide.es.md)

## Licencia
Ver [LICENSE.md](./LICENSE.md).
