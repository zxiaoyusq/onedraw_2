# Embedded WXSDK provenance

- Upstream: `https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk`
- Release line: `v0.1.33`
- Base commit: `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`
- License: MIT (`LICENSE` unchanged)
- Embedded by: T110 on 2026-07-13

## Local patch

`Runtime/WXRuntimeExtDef.cs` has one compatibility patch for Unity `6000.5+`:

- Upstream calls `UnityEngine.Object.GetInstanceID()` under `UNITY_2021_3_OR_NEWER`.
- Unity 6000.5.1f1 marks that call as obsolete with `error=true` (CS0619).
- `UNITY_6000_5_OR_NEWER` now returns `UnityEngine.EntityId.ToULong(unityObject.GetEntityId())`.
- Earlier supported Unity versions retain the upstream call unchanged.

Original error evidence: `artifacts/evals/T110/sdk-import-original-errors.log`.

Removal condition: remove the embedded copy and return to the exact Git dependency after an official immutable SDK revision replaces this call and passes the same Unity 6000.5.1f1 compile and test matrix. Until then, upgrade by comparing this directory against the recorded base commit; no other local SDK edits are allowed.
