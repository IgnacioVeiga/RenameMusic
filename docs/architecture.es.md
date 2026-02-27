# Arquitectura de RenameMusic

## Estructura de la solución
La solución está separada en tres proyectos:

1. `RenameMusic` (WPF)
2. `RenameMusic.Core` (lógica de dominio y aplicación)
3. `RenameMusic.Tests` (tests unitarios)

## Responsabilidad por proyecto

### `RenameMusic` (WPF)
- Inicio de aplicación y composición (`App.xaml.cs`)
- Ventanas y vistas XAML
- ViewModels específicos de WPF
- Servicios de UI (diálogos, file pickers, tema, idioma)
- Recursos localizados (`.resx`)

### `RenameMusic.Core`
- Modelo de persistencia y contexto EF Core
- Evaluación de reglas de renombrado
- Ingesta y persistencia de sesión
- Ejecución de renombrado y manejo de conflictos
- Contratos de servicios compartidos (`ISessionService`, `IDialogService`, etc.)
- Utilidades compartidas como `FilenameFunctions`

### `RenameMusic.Tests`
- Tests unitarios para `RenameMusic.Core`
- Sin dependencia de WPF

## Flujo de ejecución

1. La app WPF inicia en `App.xaml.cs`.
2. Se instancian servicios.
3. `MainWindowViewModel` coordina acciones del usuario.
4. Los servicios Core manejan ingesta, evaluación, persistencia y renombrado.
5. El proyecto WPF se limita a interacción UI y presentación.

## Por qué esta separación
- Mejor mantenibilidad y separación de responsabilidades
- La lógica Core se puede testear sin Windows Desktop SDK
- Ciclo de feedback más rápido con tests unitarios
- Facilita migraciones futuras a otros frontends
