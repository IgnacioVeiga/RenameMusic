# RenameMusic (Beta)
[![Download](https://img.shields.io/static/v1?style=flat-square&logo=windows&label=Download&message=windows-x64&color=137A7F&labelColor=373B3E)](https://github.com/IgnacioVeiga/RenameMusic/releases/latest/download/RenameMusic.exe)
![GitHub last commit](https://img.shields.io/github/last-commit/IgnacioVeiga/RenameMusic?color=137A7F&style=flat-square)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/IgnacioVeiga/RenameMusic?color=137A7F&label=Latest%20release&style=flat-square)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/IgnacioVeiga/RenameMusic/deploy-project.yml?color=137A7F&logo=github&style=flat-square)
![GitHub license](https://img.shields.io/github/license/IgnacioVeiga/RenameMusic?style=flat-square)

<div>
  <span>English</span> / <a href="README_es.md">Español</a> </a>
</div></br>

![RenameMusicIcon](/RenameMusic/Assets/Images/RM39.svg)
&nbsp;This Software allows you to rename music files according to a rule or pattern set by its "Tags". This rule can be set by the user.

For example: I have an audio file with the name `AUD-01230101-WA0123.mp3` but it has tags, so I can decide to call it according to the following order: `TrackNumber-Title-Artist.mp3`.

## Screenshots:
![Screenshot_0000](/RG39/Assets/Images/Screenshot_0000.png "Screenshot_0000")</br>
![Screenshot_0001](/RG39/Assets/Images/Screenshot_0001.png "Screenshot_0001")</br>
![Screenshot_0002](/RG39/Assets/Images/Screenshot_0002.png "Screenshot_0002")

***

## Functionalities:
- Files that do not meet a minimum (modifiable) requirement are separated in another tab.
- Allows to add and remove folders.
- Shows the names of the files and on one side their future names.
- Can define the position of the tags as a criterion for renaming.
- Shows a cover of the selected file (if exists).
- Does not allow repeated files and/or directories in the lists.
- Recognizes mp3, m4a, ogg and flac file formats.
- The following languages are available (more can be added): English and Spanish.
- Saves the lists in a database file.
- The lists have a page selector.
- Allow to sort the list.
- Theme selector (light, dark and others).
- Play an audio (with default app).

## To do:
- Add a search bar.
- Improve UI/UX.

***

## How to use:

***

## Required:
- Windows 7 or higher (Recommended Windows 10/11) x86/x64.
- .NET SDK 6 (LTS) to compile and run.
- .NET Desktop Runtime 6 to run.

***

## Dependencies:
### Frameworks
- Microsoft.NETCore.App **(6.0.x)**.
- Microsoft.WindowsDesktop.App.WPF **(6.0.x)**

### Packages
- Microsoft.EntityFrameworkCore.Design **(7.0.3)**
- Microsoft.EntityFrameworkCore.Sqlite **(7.0.3)**
- taglib-sharp-netstandard2.0 **(2.1.0)**
- WinCopies.WindowsAPICodePack.Shell **(2.12.0.2)**

***

## Languajes
For adding/modifying languages I highly recommend the **extension** for **Visual Studio 2022** called `ResX Manager`. It makes it much easier to manage multiple languages.
The language `.resx` files are saved in the `.\RenameMusic\Lang\` folder.

***

## Compile:
Compile via **Visual Studio 2022**. The other way is to run the `dotnet build` command from terminal (cmd/powershell) in the root of the repository and then check inside of the `\RenameMusic\bin\` folder.

***

## How contribute:

***

## License:
