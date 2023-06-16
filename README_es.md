# RenameMusic (Beta)
[![Download](https://img.shields.io/static/v1?style=flat-square&logo=windows&label=Descargar&message=windows-x64&color=137A7F&labelColor=373B3E)](https://github.com/IgnacioVeiga/RenameMusic/releases/latest/download/RenameMusic_x64.zip)
[![Download](https://img.shields.io/static/v1?style=flat-square&logo=windows&label=Descargar&message=windows-x86&color=137A7F&labelColor=373B3E)](https://github.com/IgnacioVeiga/RenameMusic/releases/latest/download/RenameMusic_x86.zip)
![GitHub last commit](https://img.shields.io/github/last-commit/IgnacioVeiga/RenameMusic?color=137A7F&style=flat-square)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/IgnacioVeiga/RenameMusic?color=137A7F&label=Latest%20release&style=flat-square)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/IgnacioVeiga/RenameMusic/deploy-project.yml?color=137A7F&logo=github&style=flat-square)
![GitHub license](https://img.shields.io/github/license/IgnacioVeiga/RenameMusic?style=flat-square)

<img src="/RenameMusic/Assets/Icons/icon.ico" width="128" height="128">
<div>
  <a href="README.md">English</a> / <span>Español</span></a>
</div></br>

Este Software permite renombrar archivos de música en función de una regla o patrón establecido por sus "Etiquetas". Esta regla puede ser establecida por el usuario.

Por ejemplo: Tengo un archivo de audio con el nombre `AUD-01230101-WA0123.mp3` pero tiene etiquetas, por lo que puedo decidir llamarlo según el siguiente orden: `NúmeroDePista-Título-Artista.mp3`.

## Capturas de pantalla:
![Screenshot_0000](/RG39/Assets/Images/Screenshot_0000.png "Screenshot_0000")</br>
![Screenshot_0001](/RG39/Assets/Images/Screenshot_0001.png "Screenshot_0001")</br>
![Screenshot_0002](/RG39/Assets/Images/Screenshot_0002.png "Screenshot_0002")

***

## Funcionalidades:
- Los archivos que no cumplen un requisito mínimo (modificable) se separan en otra pestaña.
- Permite añadir y quitar carpetas.
- Muestra los nombres de los archivos y en un lado sus nombres futuros.
- Puede definir la posición de las etiquetas como criterio para renombrar.
- Muestra una carátula del archivo seleccionado (si existe).
- No permite archivos y/o directorios repetidos en las listas.
- Reconoce los formatos de archivo mp3, m4a, ogg y flac.
- Están disponibles los siguientes idiomas (pueden añadirse más): Inglés y español.
- Guarda las listas en un archivo de base de datos.
- Las listas tienen un selector de página.
- Permite ordenar la lista.
- Selector de temas (claro, oscuro y otros).
- Reproducir audio (desde app predeterminada).

## Para hacer:
- Añadir una barra de búsqueda.
- Mejorar UI/UX.

***

## Como usar:

***

## Requerido:
- Windows 7 o superior (Recomendado Windows 10/11) x86/x64.
- .NET SDK 6 (LTS) para compilar y ejecutar.
- Entorno de ejecución de escritorio de .NET solo si es para ejecutar.

***

## Dependencias:
### Frameworks
- Microsoft.NETCore.App **(6.0.x)**.
- Microsoft.WindowsDesktop.App.WPF **(6.0.x)**

### Paquetes
- Microsoft.EntityFrameworkCore.Design **(7.0.7)**
- Microsoft.EntityFrameworkCore.Sqlite **(7.0.7)**
- taglib-sharp-netstandard2.0 **(2.1.0)**
- WinCopies.WindowsAPICodePack.Shell **(2.12.0.2)**

***

## Idiomas
Para añadir/modificar idiomas recomiendo ampliamente la **extensión** para **Visual Studio 2022** llamada `ResX Manager`. Hace mucho más facil manejar varios idiomas.
Los arhivos `.resx` de idioma se guardan en la carpeta `.\RenameMusic\Lang\`.

***

## Compilar:
Compilar a través de **Visual Studio 2022**. La otra forma es ejecutar el comando `dotnet build` desde el terminal (cmd/powershell) en la raíz del repositorio y luego comprobar dentro de la carpeta `\RenameMusic\bin\`.

***

## Como contribuir:

***

## Licencia:
