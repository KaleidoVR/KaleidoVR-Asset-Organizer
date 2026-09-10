# KaleidoVR Asset Organizer

<p align="center">
  <img src="Unity Organizer Tool/Editor/Icons/Kali_Logo.png" alt="KaleidoVR" width="300">
</p>

A Unity editor tool that sorts the assets of a VRChat avatar into a consistent folder structure, remaps their references, and writes a scene plus an optional prefab that point at those organized copies.

Drop an avatar FBX or prefab into the window, press **Organize Assets**, and its meshes, materials, textures, animations, controllers, menus, and parameters are collected and filed into one output folder.
<p align="center">
<img width="500" height="1046" alt="image" src="https://github.com/user-attachments/assets/9e97d70c-9105-4ddb-88ca-8a8dd571ebb8" />
</p>

- Unity **2022.3.22f1** or newer, including Unity 6 (6000.x)
- VRChat SDK3 Avatars is optional

## Install

1. Download the latest `.unitypackage` from [Releases](https://github.com/KaleidoVR/KaleidoVR-Asset-Organizer/releases).
2. In Unity, choose **Assets > Import Package > Custom Package...** and select the file.
3. Import everything. Files land in `Assets/KaleidoVR/Editor/`.
4. Open the tool from the menu bar: **KaleidoVR > Asset Organizer**.

To install from source instead, copy the `Editor` folder (including its `.meta` files) into `Assets/KaleidoVR/` in your project.

## Requirements

Unity 2022.3.22f1 or newer. The tool only uses editor APIs that exist in the 2022.3 LTS line, so it also compiles and runs on Unity 6.

The VRChat SDK3 Avatars package is only needed for the **Auto-Link FX & Menu** option. Everything else — organizing, copying, moving, and prefab creation — works in a plain Unity project. If the SDK is absent, the descriptor step is skipped with a warning instead of failing.

## Usage

1. Set **Output Directory** with **Select Folder**. It must be inside `Assets`.
2. Drag your avatar into **Objects to Organize**. Project FBX/prefab assets work, and so do scene instances — those resolve back to their source asset.
3. Optionally set **Scene Name** and **Prefab Name**, and drag anything you want untouched into the **Ignore List**.
4. Press **Organize Assets**.

The **Scene Name** and **Prefab Name** fields auto-fill from the first object you drop in. Organizing always writes a scene named after **Scene Name**. **Create Prefab** also writes a prefab into `<output>/Prefabs/` and places that prefab into the scene.

### Export List Options

Each asset type can be set to one of three actions:

- **Copy** — duplicate the asset into the output folder, leaving the original in place. This is the default and the safe choice.
- **Move** — relocate the original into the output folder. Use only when you intend to move your source files.
- **Ignore** — skip the type entirely.

Scripts, DLLs, shaders, and anything under `Packages/` or `Assets/Editor` are always skipped, so the tool will not relocate Poiyomi, the VRChat SDK, or other installed packages.

### Settings

- **Create Prefab** — after organizing, build a prefab in `<output>/Prefabs/` whose components point at the newly organized assets.
- Textures are always sorted into subfolders by suffix (normal, emission, metallic, roughness, AO).
- **Auto-Link FX & Menu** always runs on a single organized object: add or reuse a `VRCAvatarDescriptor` and assign the FX layer, expressions menu, and expression parameters.

All settings persist between sessions via `EditorPrefs`.

## Output structure

```
<output folder>/
├── <Scene Name>.unity
├── FBX/
├── Materials/
├── Textures/                       (Normals, Emissions, Metallic, Roughness, AO)
├── Audio/
├── Prefabs/
├── Other/
└── 3.0/
    ├── Animations/
    ├── BlendTrees/
    ├── Avatar Masks/
    ├── Controllers/
    ├── Menus/
    └── VRCExpressionParameters/
```

## How references are kept intact

Copies are made through the Unity asset database rather than by copying files and their `.meta` on disk. Each copy therefore gets its own GUID, and the tool then rewrites the serialized references of the copied assets and the generated prefab to point at the new files.

This matters because duplicating a `.meta` file duplicates its GUID, which leaves two assets claiming the same identity and causes materials, controllers, and prefabs to resolve to the wrong file.

Every run writes a log of what was copied, moved, and ignored to `Logs/KaleidoVR/Organizer/`.

## Known limitations

Prefab generation expects the avatar root as a single object. Dropping several root objects nests them all under one new parent named after **Prefab Name**.

## Credits

Created and maintained by **KaleidoVR**.

- [kalivr.com](https://kalivr.com)
- [Discord](https://discord.com/invite/cRsufJssTA)

## License

[MIT](LICENSE) — Copyright (c) 2026 KaleidoVR
