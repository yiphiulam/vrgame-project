# CLAUDE.md - Developer Guide

This file outlines build commands, styling rules, and development practices for **"窒息室" (The Breathless Study Room)**.

## 🛠️ Build & Dev Commands

Unity projects are compiled and built inside the Unity Editor, but you can use standard CLI tools for automation, scripting, and syntax validation.

### Unity CLI Batchmode Build (Windows)
To build a standalone desktop target from PowerShell:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe" -batchmode -quit -projectPath . -buildWindowsPlayer build/Windows/TheBreathlessStudyRoom.exe
```

### Static Syntax Check & Roslyn Compilation
To run code analysis or compile C# scripts using .NET CLI (requires generating .csproj via Unity once):
```powershell
dotnet build TheBreathlessStudyRoom.sln /p:Configuration=Debug
```

---

## 🎨 Coding & Style Guidelines

Follow these rules exactly when modifying C# scripts for this codebase:

### 1. Naming Standards
- **Classes, Namespaces, Methods**: PascalCase (`GazeDwellSelector`, `TriggerAnomaly`)
- **Private/Protected Variables**: `_camelCase` with leading underscore (`_dwellTimer`, `_lampRef`)
- **Public Fields/Properties/Events**: PascalCase (`OnGazeSelected`, `StressRate`)
- **Constants**: UPPERCASE (`STRESS_LIMIT_MAX`)

### 2. Formatting & Documentation
- **XML Summaries**: Every class, public function, and serialized property must be annotated with `/// <summary>` XML documentation block.
- **Attributes**: Expose variables to Unity Inspector using `[SerializeField]`, `[Header("...") ]` and `[Tooltip("...")]`. Do NOT make variables public just to expose them to the Inspector.

### 3. Architecture & Decoupling
- **Event-Driven**: Decouple components using Standard C# `System.Action` or `UnityEngine.Events.UnityEvent`.
- **Anti-Pattern**: NEVER use `GameObject.Find()`, `FindObjectOfType()`, or hardcoded path string searches. Bind all references in the Unity Inspector.
- **VR Interactor**: Use Unity XR Interaction Toolkit (XRI) 3.x and map gaze using a secondary offset entity to avoid Transform fights.
