# T693 Verification

## Result

PASS. Android Player Settings now use Auto Rotation with only Landscape Left and Landscape Right enabled. Portrait and Portrait Upside Down are disabled.

## Evidence

- Focused EditMode regression: 1/1 passed.
- Unity compilation before the test: 0 console errors.
- Android Build Profile has an empty Player Settings override list.
- Android APK build: succeeded with 0 errors after one transient, empty-diagnostic IL2CPP linker retry.
- APK: 46,258,919 bytes; SHA-256 `94d33cdbd978b2d62a530a767640c9828cb2737f12b24283a443410d9cf52ec0`.
- Final merged AndroidManifest: `UnityPlayerGameActivity` has `screenOrientation=11`; Android SDK 36 maps value 11 to `userLandscape`.
- Static settings audit and `git diff --check`: PASS.

## Scope

No scenes, prefabs, Build Profiles, WeChat settings, gameplay configuration, or runtime gameplay code were changed. Existing user-owned ProjectSettings and `Assets/Resources.meta` changes remain unstaged and uncommitted.
