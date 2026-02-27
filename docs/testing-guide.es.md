# Guía de Testing

## Objetivo
Este proyecto usa `xUnit` para tests unitarios en `RenameMusic.Tests`.

El objetivo actual es validar el comportamiento del Core sin depender del runtime WPF.

## Primeros tests incluidos
- `TemplateRuleServiceTests`
- `FilenameFunctionsTests`

Son tests simples a propósito, para que sirvan como plantilla para agregar más.

## Ejecutar tests
Desde la raíz del repositorio:

```bash
dotnet test RenameMusic.Tests/RenameMusic.Tests.csproj -c Release
```

## Agregar un nuevo test
1. Crear un archivo nuevo en `RenameMusic.Tests`.
2. Crear una clase terminada en `Tests`.
3. Agregar métodos con `[Fact]` para escenarios puntuales.
4. Usar nombres claros tipo `Metodo_DeberiaHacerX_CuandoY`.

## Próximos tests recomendados
1. Manejo de duplicados en ingesta de `SessionService`
2. Comportamiento de políticas de conflicto en `RenameExecutionService`
3. Comportamiento de archivos faltantes en snapshots de sesión
