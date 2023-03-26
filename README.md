# RenameMusic
![RenameMusicIcon](/RenameMusic/Assets/Images/RM39.svg)
&nbsp;&nbsp;&nbsp;&nbsp;
[![Download](https://img.shields.io/static/v1?style=flat-square&logo=windows&label=Download&message=windows-x64&color=000099&labelColor=009900)](https://github.com/IgnacioVeiga/RenameMusic/releases/latest/download/RenameMusic.exe)
![GitHub last commit](https://img.shields.io/github/last-commit/IgnacioVeiga/RenameMusic?color=009900&style=flat-square)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/IgnacioVeiga/RenameMusic?color=darkblue&label=Latest%20release&style=flat-square)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/IgnacioVeiga/RenameMusic/deploy-project.yml?color=009900&logo=github&style=flat-square)
![GitHub license](https://img.shields.io/github/license/IgnacioVeiga/RenameMusic?style=flat-square)
</br>
El objetivo de la app es poder renombrar archivos de música en base a sus "tags" según un criterio definido por el usuario o no (uno predeterminado).

Por ejemplo: tengo una canción con el nombre `AUD-01230101-WA0123.mp3` pero esta misma tiene "tags", entonces puedo decidir que con estos se llame según el siguiente orden: `NroDePista-Titulo-Artista.mp3`

## Capturas:
![Screenshot_0000](/RG39/Assets/Images/Screenshot_0000.png "Screenshot_0000")
![Screenshot_0001](/RG39/Assets/Images/Screenshot_0001.png "Screenshot_0001")
![Screenshot_0002](/RG39/Assets/Images/Screenshot_0002.png "Screenshot_0002")

## Funcionalidades:
- [x] Aquellos archivos que no contengan como mínimo el tag de título los separa en otra pestaña.
- [x] Permite agregar y eliminar carpetas.
- [x] Enseña los nombres de los archivos y en un lado sus futuros nombres.
- [x] Definir la posición de los "tags" como critero para renombrar.
- [x] Tiene un criterio predeterminado para renombrar.
- [x] Caratulas de los archivos.
- [x] Impedir que se repitan archivos y/o directorios.
- [x] Reconoce archivos de formato mp3, m4a, ogg y flac.
- [x] Soporte para más de un idioma.

## Incompleto:
- [ ] Permitir ordenar la lista.
- [ ] Reproducir el archivo desde esta app o una externa.
- [ ] Arreglar los tamaños de los elementos.
- [ ] Usar temas personalizados.

## Requerido:
- Windows 7 o superior (Recomendado Windows 10/11) x86/x64.
- SDK .NET 6.0 (LTS) para compilar y ejecutar.
- Runtime .NET 6.0 para ejecutar.

## Dependencias:
### Frameworks
- Microsoft.NETCore.App **(6.0.x)**
- Microsoft.WindowsDesktop.App.WPF **(6.0.x)**

### Paquetes
- Microsoft.EntityFrameworkCore.Design **(7.0.3)**
- Microsoft.EntityFrameworkCore.Sqlite **(7.0.3)**
- taglib-sharp-netstandard2.0 **(2.1.0)**
- WinCopies.WindowsAPICodePack.Shell **(2.12.0.2)**

## Compilar:
Compilar a través de Visual Studio 2022. La otra opción es ejecutar el comando `dotnet build` en la raíz del repositorio y luego revisar la carpeta "bin".
