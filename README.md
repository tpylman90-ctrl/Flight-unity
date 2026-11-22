# Flight-unity | Building Inspection

A compact Unity 6.3 Android viewer for inspecting one Blender-authored Eclipse building from the ground. This first viewing build has a continuous ground plaza and one architectural landmark; it has no flying or race course.

## Controls

- Drag one finger or the mouse to orbit around the building.
- Pinch on Android or use the mouse wheel to zoom.
- Tap **RESET VIEW** or press `R` to restore the starting view.

## Build an APK

The shared Unity runner is [Eclipse-Hollow-Sky-Unity.](https://github.com/tpylman90-ctrl/Eclipse-Hollow-Sky-Unity.). Open its **Actions > Build Selected Unity Project** workflow and use the defaults for this repository. The Unity license secrets stay in the runner repository.

The runner checks out this project, runs the Blender generator, builds an ARM64 Android APK, and uploads the APK as a workflow artifact for phone review.

## Blender source

The original Blender 4.x generator is `Blender/BuildingInspection/generate_building.py`. It creates `Assets/Resources/Inspection/HaloSpire.obj` and a preview PNG when run from the repository root:

```sh
blender --background --factory-startup --python Blender/BuildingInspection/generate_building.py
```

The build runner executes this generator before Unity imports the asset. The Android viewer renders the model in Unity URP.
