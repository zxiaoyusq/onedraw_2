# T420 Unity MCP Jobs

Unity: `6000.5.1f1`

Instance: `onedraw_2@272e911286835fad`

## Final passing runs

| Scope | Mode | Job | Passed | Failed | Skipped | Result |
|---|---|---|---:|---:|---:|---|
| T420 category | EditMode | `b2c4b0ef5d734d1e8ee40b13b5dfe019` | 7 | 0 | 0 | PASS |
| T420 category | PlayMode | `f0aed474f6b3462aacb344ce3eb863c2` | 1 | 0 | 0 | PASS |
| Full project | EditMode | `2aedc939d67440ce89213aa0c2871f8b` | 117 | 0 | 0 | PASS |
| Full project | PlayMode | `4d26df95c0374021a0b7dc4a263d2cfa` | 29 | 0 | 0 | PASS |

## Development retries

- `f794c69ef5274c488fc99608562422f9`: 2/7 passed. Failures exposed test-only float precision assumptions, non-monotonic test call order, and missing explicit `Damageable` setup on dynamically created test objects.
- `28778ebd6af24f329e2ff58d02af057f`: 4/7 passed. Remaining failures exposed float-backed boundary literals and a test selecting a phase-3 Boss attack while the base enemy row correctly exposes the phase-1 attack set.
- Tests were corrected to derive exact boundaries from the loaded configuration and exercise `atk_boss_rockfall` from the Boss base attack set. Product configuration and generated artifacts were not changed.

## Compile and Console

- Script refresh/domain reload after product and test changes: PASS.
- Compilation diagnostics: 0 Error / 0 Warning.
- Final Console check after cleanup: 0 Error / 0 Warning.
- PlayMode runner changed `ProjectSettings/EditorSettings.asset:m_EnterPlayModeOptions` transiently; it was restored to the task baseline and is absent from final changes.
- MCP does not export NUnit XML; the job IDs and exact counts above are the retained evidence.
