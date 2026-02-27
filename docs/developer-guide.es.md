# Guía Para Desarrolladores de RenameMusic

## 1. Objetivo del proyecto
RenameMusic es una app de escritorio para renombrar archivos de música usando una plantilla definida por el usuario en base a metadatos.

Objetivos actuales:
- Soportar listas grandes de archivos.
- Persistir sesión en SQLite para recuperación tras cierre de la app.
- Mantener separación clara entre UI y lógica mediante MVVM.

## 2. Stack técnico
- .NET 8
- WPF
- EF Core 8 con SQLite
- CommunityToolkit.Mvvm
- TagLib# (taglib-sharp-netstandard2.0)

## 3. Modelo de arranque
La app ahora inicia desde `App.xaml.cs` y crea explícitamente:
- `RenameMusic.Views.MainWindow`
- `RenameMusic.ViewModels.MainWindowViewModel`

La antigua ventana raíz `MainWindow` fue removida para evitar confusión de arranque.

## 4. Arquitectura MVVM

### 4.1 Vista
`RenameMusic/Views/MainWindow.xaml`
- DataGrid y menú enlazados a comandos del ViewModel y colecciones observables.
- Code-behind mínimo.

### 4.2 ViewModel
`RenameMusic/ViewModels/MainWindowViewModel.cs`
- Coordina inicio, carga de sesión, AddFile, AddFolder, cambios de regla y renombrado masivo.
- Mantiene estado de UI (`ToRenameItems`, `DoNotRenameItems`, `FolderItems`, barra de estado, portada seleccionada).
- Usa comandos asíncronos con `CommunityToolkit.Mvvm`.

`RenameMusic/ViewModels/ReplaceWithViewModel.cs`
- Mantiene el estado y las validaciones del diálogo de plantilla.
- Expone comandos (`InsertTag`, `Apply`, `Cancel`) y evento de cierre, dejando `ReplaceWith.xaml.cs` mínimo.

### 4.3 Servicios
- `SessionService`: persistencia y pipeline de ingreso.
- `TemplateRuleService`: parseo de plantilla y elegibilidad de renombrado.
- `RenameExecutionService`: renombrado físico y resolución de conflictos.
- `DialogService`: mensajes y modales.
- `FilePickerService`: selección de archivos y carpetas.
- El ViewModel principal depende de contratos (`ISessionService`, `ITemplateRuleService`, `IRenameExecutionService`) en lugar de clases concretas.

## 5. Modelo de persistencia

### 5.1 Tablas
Tablas principales:
- `SessionAudios` (`SessionAudioEntity`)
- `SessionFolders` (`SessionFolderEntity`)

### 5.2 Índices
Definidos en `MyContext.OnModelCreating`:
- Único: `SessionAudios.FullPath`
- No único: `SessionAudios.FolderPath`
- No único: `SessionAudios.FileNameWithoutExtension`
- No único: `SessionAudios.CanRename`
- Único: `SessionFolders.FolderPath`

## 6. Reglas de renombrado

Comportamiento actual:
- La plantilla debe contener al menos un tag soportado.
- Los tags usados en la plantilla definen metadatos requeridos en modo estricto.
- Estrategia de faltantes configurable:
  - Estricto
  - Usar placeholder (`Unknown` o equivalente localizado)
- Se permiten tags repetidos, pero se muestra advertencia.

Tags soportados:
- `<TrackNum>`
- `<Title>`
- `<Album>`
- `<AlbumArtist>`
- `<Artist>`
- `<Year>`

## 7. Comportamiento de sesión
- Al iniciar, si existe sesión guardada, se pregunta si cargarla.
- Si el usuario rechaza, se avisa y se elimina la sesión.
- Archivos faltantes pasan a `Do Not Rename` con motivo `File not found.`
- Se muestra aviso general cuando se detectan archivos faltantes.

## 8. Conflictos de nombre
- Se mantiene la ventana modal `RepeatedFile`.
- El renombrado masivo soporta aplicar una decisión al resto del lote actual.
- Existe política de conflicto por defecto configurable en Ajustes:
  - Preguntar siempre
  - Reemplazar siempre
  - Omitir siempre
  - Renombrar con número siempre

## 9. Alcance actual y tareas diferidas

Implementado ahora:
- AddFile
- AddFolder
- Carga de sesión
- Recalculo por cambio de plantilla con confirmación
- Renombrado masivo con resolución de conflictos
- Acciones por archivo desde menú contextual:
  - reproducir archivo
  - editar tags
  - mover entre `To Rename` y `Do Not Rename`
  - renombrar archivo individual
- Remoción de carpetas de sesión incluyendo subcarpetas
- Escaneo robusto de carpetas que omite subdirectorios inaccesibles sin abortar la carga
- Persistencia por lotes durante la carga para reducir presión de memoria en importaciones grandes
- Renombrado con persistencia masiva de estados para reducir roundtrips a base de datos

Diferido de forma intencional:
- Flujos `DeleteFile` y `DeleteFolder` (se deben implementar de forma segura)
- Rediseño UX más profundo fuera del esquema actual por tabs

## 10. Build y pruebas
- El build completo WPF debe ejecutarse en Windows con .NET Desktop SDK.
- En Linux sin `Microsoft.NET.Sdk.WindowsDesktop` no compila el proyecto.
- El CI debe usar runners Windows para validación de build.

## 11. Convenciones para contribuir
- Mantener lógica de negocio en servicios y ViewModels, no en code-behind.
- Centralizar operaciones de filesystem en servicios de sesión y renombrado.
- Usar APIs asíncronas en operaciones largas.
- Dejar comentarios `TODO` en inglés para comportamiento diferido.
- Mantener eliminada la carpeta `RenameMusic_v1` y no reintroducir copias legacy del proyecto.
