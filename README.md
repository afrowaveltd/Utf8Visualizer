# UTF-8 Unicode Visualizer

[![VS Version](https://img.shields.io/badge/VS-2022%2B-blueviolet)](https://visualstudio.microsoft.com/)
[![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](Utf8Visualizer/LICENSE.txt)

**A Visual Studio extension that makes Unicode escape sequences readable at a glance.**  
See `\uXXXX` rendered as actual glyphs directly in your editor — non-destructively and beautifully.

---

<p align="center">
  <img src="Utf8Visualizer/Resources/Preview.png" alt="UTF-8 Visualizer preview" />
</p>

---

## ✨ Features

### 🎨 Two-Layer Visualization (Color-Coded)

| Layer | Color | What It Shows |
|-------|-------|---------------|
| **Teal** | `\uXXXX` → real glyph | The data is an *escape sequence*; you see the actual character |
| **Orange** | Non-ASCII char in source | The data already *contains the character*; you get a warning highlight |

> You always know whether the underlying bytes are an escape sequence or a literal non-ASCII character.

### ⚡ Commands at Your Fingertips

| Command | Shortcut | Where |
|---------|----------|-------|
| **Toggle visualization** (global) | Ctrl+Shift+U | Context menu · Toolbar · Tools menu |
| **Toggle per-document** | — | Context menu · Tools menu |
| **Convert at cursor / selection** | Ctrl+Alt+U | Context menu · Tools menu |
| **Convert entire document** | Ctrl+Shift+Alt+U | Context menu · Toolbar · Tools menu |

### 🧠 Smart Conversion

- **Cursor position** — converts the escape or non-ASCII character right under your cursor
- **Selection** — converts everything inside the selected text
- **Entire document** — auto-detects direction: escapes → glyphs, or glyphs → escapes

### 🌍 Full Unicode Support

- `\uXXXX` (BMP, 4 hex digits)
- `\UXXXXXXXX` (supplementary planes, 8 hex digits)
- **Surrogate pairs** (emoji, historic scripts, mathematical symbols)
- Works across **all file types** — JSON, JavaScript, TypeScript, C#, Python, Java, Go, Rust, XML, YAML, plain text, and more

### 🛡️ Non-Destructive

Adornments are **WPF overlays** rendered on top of the text buffer. Your source code is never modified until you explicitly choose to convert.

---

## 📦 Installation

### Visual Studio Marketplace *(coming soon)*

1. Open Visual Studio (2022 or newer)
2. Go to **Extensions → Manage Extensions**
3. Search for **"UTF-8 Unicode Visualizer"**
4. Click **Download** and restart Visual Studio

### Manual Install

1. Download the latest `.vsix` from the [Releases page](https://github.com/afrowaveltd/Utf8Visualizer/releases)
2. Double-click the `.vsix` file
3. Follow the installer prompts

---

## 🚀 Quick Start

1. Open any file containing `\uXXXX` escape sequences (e.g., a JSON translation file)
2. **Right-click** in the editor → you'll see the **UTF-8 Visualizer** commands
3. Toggle visualization on/off with **Ctrl+Shift+U**
4. Place your cursor on an escape or non-ASCII character and press **Ctrl+Alt+U** to convert it
5. Press **Ctrl+Shift+Alt+U** to convert the entire document in one go

---

## 🔧 Development

``bash
# Clone
git clone https://github.com/afrowaveltd/Utf8Visualizer.git

# Open in Visual Studio
# Build → the .vsix will be generated in bin\Debug\
# Press F5 to launch the Experimental Instance
``

### Requirements

- Visual Studio 2022 or 2026 (Community / Pro / Enterprise)
- .NET Framework 4.8
- Visual Studio SDK (included with the VS workload "Visual Studio extension development")

### Project Structure

`
Utf8Visualizer/
├── ToggleUtf8Command.cs           — all command handlers & conversion logic
├── ToggleUtf8CommandPackage.cs    — AsyncPackage entry point
├── Utf8AdornmentTagger.cs         — WPF adornment rendering & regex matching
├── Utf8AdornmentTaggerProvider.cs — MEF export, per-buffer tagger creation
├── Utf8VisualizationState.cs      — global & per-document state management
├── Utf8Visualizer.vsct            — menus, toolbars, context menu, keybindings
├── source.extension.vsixmanifest  — VSIX package manifest
└── Resources/
    ├── Icon.png
    ├── Icon32.png
    └── Preview.png
`

---

## ❓ FAQ

**Q: Does this modify my source code?**  
No. Adornments are rendered on top of the editor surface. Your files stay untouched unless you explicitly run a convert command.

**Q: Which file types are supported?**  
All text-based files — the tagger binds to the `"text"` content type, covering every language in Visual Studio.

**Q: Can I turn off visualization for just one file?**  
Yes! Right-click and choose **"Disable visualization for this document"**. The global toggle still affects everything else.

**Q: What about emoji and surrogate pairs?**  
Fully supported. `\U0001F600` (😀) and similar supplementary characters are handled correctly in both visualization and conversion.

---

## 📄 License

This project is licensed under the MIT License — see [LICENSE.txt](Utf8Visualizer/LICENSE.txt) for details.

---

## 🤝 Contributing

Pull requests are warmly welcome!  
Found a bug? Have a feature idea? [Open an issue](https://github.com/afrowaveltd/Utf8Visualizer/issues).

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/afrowaveltd">Afrowave</a>
</p>
