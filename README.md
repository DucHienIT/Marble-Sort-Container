# TemplateMobileGame

A Unity template project for casual mobile games, set up with the Universal Render Pipeline (URP), the new Input System, and 2D tooling.

## Requirements

- **Unity** `6000.3.9f1` (Unity 6) — open with the matching editor version for best results.

## Key Packages

- **Universal RP** (`com.unity.render-pipelines.universal`) — rendering pipeline
- **Input System** (`com.unity.inputsystem`) — input handling
- **2D Tooling** — Animation, Aseprite, PSD Importer, Sprite Shape, Tilemap (+ Extras)
- **Timeline**, **Visual Scripting**, **uGUI**
- **Test Framework** — play/edit mode tests
- **Toony Colors Pro 2** — stylized/toon shading (under `Assets/JMO Assets`)
- **Unity MCP** (`com.coplaydev.unity-mcp`) — Model Context Protocol integration for editor automation

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/DucHienIT/TemplateMobileGame.git
   ```
2. Open the project in **Unity Hub** with editor version `6000.3.9f1`.
3. Let Unity import packages and regenerate the `Library/` folder on first launch.

## Project Structure

```
Assets/             # Game assets, scripts, scenes, and third-party packages
Packages/           # Package manifest and lock file
ProjectSettings/    # Unity project configuration
```

> Auto-generated folders (`Library/`, `Temp/`, `obj/`, `Logs/`, IDE/solution files) are excluded via `.gitignore`.
