# Flight-unity | Building Inspection

A small Unity 6.3 Android viewer for inspecting one Blender-authored Eclipse building from the ground. There is no flying or race course in this first viewing build.

## Controls

- Drag one finger or the mouse to orbit around the building.
- Pinch on Android or use the mouse wheel to zoom.
- Tap **RESET VIEW** or press `R` to restore the starting view.

## Build an APK

The shared Unity runner is [Eclipse-Hollow-Sky-Unity.](https://github.com/tpylman90-ctrl/Eclipse-Hollow-Sky-Unity.). Open its **Actions > Build Selected Unity Project** workflow and use the defaults for this repository. Unity activation secrets remain in the runner repository.

The workflow builds an ARM64 Android APK and attaches it as a downloadable workflow artifact for phone review.

## Blender source

The original Blender 4.x generator is `Blender/BuildingInspection/generate_building.py`. It exports the game asset to `Assets/Resources/Inspection/HaloSpire.fbx`. Re-run it from the repository root with:

```sh
blender --background --factory-startup --python Blender/BuildingInspection/generate_building.py
```

The accompanying PNG is an offline Blender preview used as the asset's visual reference; the Android app renders the FBX in Unity URP.
