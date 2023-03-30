# RenameMusic (Beta)
![RenameMusicIcon](/RenameMusic/Assets/Images/RM39.svg)
&nbsp;&nbsp;&nbsp;&nbsp;
[![Download](https://img.shields.io/static/v1?style=flat-square&logo=windows&label=Download&message=windows-x64&color=000099&labelColor=009900)](https://github.com/IgnacioVeiga/RenameMusic/releases/latest/download/RenameMusic.exe)
![GitHub last commit](https://img.shields.io/github/last-commit/IgnacioVeiga/RenameMusic?color=009900&style=flat-square)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/IgnacioVeiga/RenameMusic?color=darkblue&label=Latest%20release&style=flat-square)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/IgnacioVeiga/RenameMusic/deploy-project.yml?color=009900&logo=github&style=flat-square)
![GitHub license](https://img.shields.io/github/license/IgnacioVeiga/RenameMusic?style=flat-square)
</br>
The objective of the app is to be able to rename music files based on a criteria defined by their "Tags". This criteria can be set by the user.

For example: I have an audio file with the name `AUD-01230101-WA0123.mp3` but it has tags, so I can decide to call it according to the following order: `TrackNumber-Title-Artist.mp3`.

## Screenshots:
![Screenshot_0000](/RG39/Assets/Images/Screenshot_0000.png "Screenshot_0000")
![Screenshot_0001](/RG39/Assets/Images/Screenshot_0001.png "Screenshot_0001")
![Screenshot_0002](/RG39/Assets/Images/Screenshot_0002.png "Screenshot_0002")

## Functionalities:
- [x] Those files that do not contain at least the title tag separates them in another tab.
- [x] Allows to add and delete folders.
- [x] Shows the names of the files and on one side their future names.
- [x] Define the position of the tags as a criterion for renaming.
- [x] Has a default criteria to use.
- [x] Shows a cover of the selected file.
- [x] Does not allow repeated files and/or directories in the lists.
- [x] Recognizes mp3, m4a, ogg and flac file formats.
- [x] The following languages are available (more can be added): English and Spanish.
- [x] Saves the lists in a database file.
- [x] The lists have a page selector and a size selector for each page.

## To do:
- [ ] Allow to sort the list.
- [ ] Play the file from this app or an external one.
- [ ] Improve UI/UX.
- [ ] Use theme selector (light, dark and more).
- [ ] And more!

## Required:
- Windows 7 or higher (Recommended Windows 10/11) x86/x64.
- .NET SDK 6.0 (LTS) to compile and run.
- Runtime .NET 6.0 to run.

## Dependencies:
### Frameworks
- Microsoft.NETCore.App **(6.0.x)**.
- Microsoft.WindowsDesktop.App.WPF **(6.0.x)**

### Packages
- Microsoft.EntityFrameworkCore.Design **(7.0.3)**
- Microsoft.EntityFrameworkCore.Sqlite **(7.0.3)**
- taglib-sharp-netstandard2.0 **(2.1.0)**
- WinCopies.WindowsAPICodePack.Shell **(2.12.0.2)**

## Compile:
Compile via **Visual Studio 2022**. The other way is to run the `dotnet build` command from terminal (cmd/powershell) in the root of the repository and then check inside of the `\RenameMusic\bin\` folder.
