# One Stroke Demon UI font provenance

- Delivered font: `OneStrokeDemonUI-Regular.ttf`
- Upstream family: Noto Sans SC variable font from the official Google Fonts repository
- Pinned upstream commit: `2894aab31764f10f29c421bdfd2340d3b382d384`
- Upstream file: `ofl/notosanssc/NotoSansSC[wght].ttf`
- Upstream SHA-256: `a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da`
- Upstream size: 17,772,300 bytes (downloaded only as a local build input; not committed)
- License: SIL Open Font License 1.1; see `OFL.txt`
- Delivered subset SHA-256: `9de334f2650055fa13b55c14200a55b5d87486c7f4e0ba5a3d1a23efeff8c0e4`
- Delivered subset size: 126,168 bytes

The delivered font is a modified subset. It is instantiated at weight 500,
renamed to `One Stroke Demon UI` so it does not use the upstream reserved font
name, and reduced to the 299 Unicode code points in
`OneStrokeDemonUI.charset.txt`. That character list is the union of printable
ASCII, non-breaking space, common Simplified Chinese UI punctuation, and every
`texts[].zhCN` value in the generated gameplay configuration.

The original 17 MB variable font is intentionally excluded from the repository
and delivery. The checked-in subset remains under the OFL and exists only inside
the Unity delivery package. TMP authoring further splits it into a 512×512 Latin
primary SDF atlas and a 1024×1024 Chinese fallback SDF atlas; both are static and
single-atlas.

Reproducible subset command parameters:

```text
fontTools.varLib.instancer: wght=500
pyftsubset: --text-file=OneStrokeDemonUI.charset.txt --layout-features='*'
  --glyph-names --symbol-cmap --legacy-cmap --name-IDs='*' --name-legacy
  --name-languages='*' --notdef-glyph --notdef-outline --recommended-glyphs
```
