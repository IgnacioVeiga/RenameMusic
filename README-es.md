# RenameMusic (Beta)
[![Download](https://img.shields.io/static/v1?style=flat-square&logo=windows&label=Descargar&message=windows-x64&color=000099&labelColor=009900)](https://github.com/IgnacioVeiga/RenameMusic/releases/latest/download/RenameMusic.exe)
![GitHub last commit](https://img.shields.io/github/last-commit/IgnacioVeiga/RenameMusic?color=009900&style=flat-square)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/IgnacioVeiga/RenameMusic?color=darkblue&label=Latest%20release&style=flat-square)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/IgnacioVeiga/RenameMusic/deploy-project.yml?color=009900&logo=github&style=flat-square)
![GitHub license](https://img.shields.io/github/license/IgnacioVeiga/RenameMusic?style=flat-square)

<div>
  <a href="README-es.md">English</a> / <span>Español</span></a>
</div></br>

![RenameMusicIcon](/RenameMusic/Assets/Images/RM39.svg)
&nbsp;El objetivo de la aplicación es poder renombrar archivos de música en función de un criterio definido por sus "Etiquetas". Este criterio puede ser establecido por el usuario.

Por ejemplo: Tengo un archivo de audio con el nombre `AUD-01230101-WA0123.mp3` pero tiene etiquetas, por lo que puedo decidir llamarlo según el siguiente orden: `NúmeroDePista-Título-Artista.mp3`.

## Capturas de pantalla:
![Screenshot_0000](/RG39/Assets/Images/Screenshot_0000.png "Screenshot_0000")</br>
![Screenshot_0001](/RG39/Assets/Images/Screenshot_0001.png "Screenshot_0001")</br>
![Screenshot_0002](/RG39/Assets/Images/Screenshot_0002.png "Screenshot_0002")

## Funcionalidades:
- [x] Los archivos que no contienen al menos la etiqueta de título los separa en otra pestaña.
- [x] Permite añadir y eliminar carpetas.
- [x] Muestra los nombres de los archivos y en un lado sus nombres futuros.
- [x] Puede definir la posición de las etiquetas como criterio para renombrar.
- [x] Tiene un criterio por defecto para utilizar.
- [x] Muestra una carátula del archivo seleccionado (si existe).
- [x] No permite archivos y/o directorios repetidos en las listas.
- [x] Reconoce los formatos de archivo mp3, m4a, ogg y flac.
- [x] Están disponibles los siguientes idiomas (pueden añadirse más): Inglés y español.
- [x] Guarda las listas en un archivo de base de datos.
- [x] Las listas tienen un selector de página y un selector de tamaño para cada página.
- [x] Permite ordenar la lista.

## Para hacer:
- [ ] Reproducir un archivo de audio desde esta aplicación o externa.
- [ ] Utiliza la barra de búsqueda.
- [ ] Mejorar UI/UX.
- [ ] Utilizar selector de temas (claro, oscuro y otros).
- [ ] Y más!

## Como usar:

## Requerido:
- Windows 7 o superior (Recomendado Windows 10/11) x86/x64.
- .NET SDK 6 (LTS) para compilar y ejecutar.
- Entorno de ejecución de escritorio de .NET solo si es para ejecutar.

## Dependencias:
### Frameworks
- Microsoft.NETCore.App **(6.0.x)**.
- Microsoft.WindowsDesktop.App.WPF **(6.0.x)**

### Paquetes
- Microsoft.EntityFrameworkCore.Design **(7.0.3)**
- Microsoft.EntityFrameworkCore.Sqlite **(7.0.3)**
- taglib-sharp-netstandard2.0 **(2.1.0)**
- WinCopies.WindowsAPICodePack.Shell **(2.12.0.2)**

## Compilar:
Compilar a través de **Visual Studio 2022**. La otra forma es ejecutar el comando `dotnet build` desde el terminal (cmd/powershell) en la raíz del repositorio y luego comprobar dentro de la carpeta `\RenameMusic\bin\`.

## Como contribuir:

## Licencia: