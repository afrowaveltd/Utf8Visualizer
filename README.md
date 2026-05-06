# UTF-8 Unicode Visualizer for Visual Studio

![Icon](Utf8Visualizer/Resources/Icon32.png)

> Visualizes Unicode escape sequences like `\uXXXX` as readable characters directly in the Visual Studio editor.

## Overview

This extension helps when working with files that contain escaped Unicode, especially JSON and localization resources. It renders `\uXXXX` sequences inline as readable characters using editor adornments, while leaving the underlying document unchanged until you explicitly run the toggle command.

## Features

- Inline visualization of `\uXXXX` escape sequences
- Non-destructive editor adornments
- Context-menu command to toggle between escape sequence and decoded character
- Tooltip showing the original escape and resulting Unicode code point
- Support for multiple editor content types including JSON, JavaScript, TypeScript, Python, C#, Java, Go, Rust, Ruby, PHP, Swift, Kotlin, Scala, Perl, Lua, PowerShell, XML, YAML, C/C++, and plain text
- Compatible with Visual Studio 2022 and newer 18.x builds

## Example

### Source text

```json
{
  "API Explorer": "API pr\u016Fzkumn\u00EDk",
  "Afghanistan": "Afgh\u00E1nist\u00E1n"
}
```

### In the editor

The extension shows the decoded characters inline with a teal-styled adornment and a tooltip such as `Unicode escape: \u016F -> ů (U+016F)`.

## Usage

1. Open a supported file containing `\uXXXX` sequences.
2. The extension visualizes matching escapes automatically in the editor.
3. Right-click in the editor and choose `Přepnout UTF-8 sekvenci na znaku`.
4. The command:
   - decodes the escape under the caret,
   - or encodes a non-ASCII character under the caret,
   - or falls back to the first escape on the current line.

## Build from source

### Prerequisites

- Visual Studio 2022 or later
- .NET Framework 4.8 targeting pack / developer tools
- Visual Studio extension development tools

### Build in Visual Studio

1. Open the project in Visual Studio.
2. Restore NuGet packages.
3. Build the `Utf8Visualizer` project.
4. Optionally start debugging to launch the Experimental Instance.

### Build with MSBuild

```powershell
msbuild .\Utf8Visualizer\Utf8Visualizer.csproj /t:Build /p:Configuration=Release /nologo
```

## Install the VSIX

1. Build the project in `Release` configuration.
2. Locate `Utf8Visualizer.vsix` in the output folder.
3. Run the VSIX installer.

## Project structure

- `Utf8Visualizer/ToggleUtf8Command.cs` - editor command for encode/decode toggling
- `Utf8Visualizer/Utf8AdornmentTagger.cs` - inline visualization logic
- `Utf8Visualizer/Utf8AdornmentTaggerProvider.cs` - MEF tagger provider
- `Utf8Visualizer/ToggleUtf8CommandPackage.cs` - Visual Studio package entry point
- `Utf8Visualizer/source.extension.vsixmanifest` - VSIX metadata
- `Utf8Visualizer/Utf8Visualizer.vsct` - command table definition

## GitHub publishing

The workspace is prepared for GitHub publication.

```powershell
git init
git add .
git commit -m "Initial commit"
```

After creating an empty repository on GitHub, connect and push it:

```powershell
git remote add origin https://github.com/<your-account>/utf8visualizer.git
git branch -M main
git push -u origin main
```

## Suggested future improvements

- Add automated tests or integration validation where practical for VSIX behavior
- Add a release workflow that publishes the VSIX artifact
- Make supported content types configurable if broader coverage is needed
- Add marketplace-ready screenshots and usage GIFs

## License

See `Utf8Visualizer/LICENSE.txt`.

## Author

Afrowave
