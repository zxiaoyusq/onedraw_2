import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const inputPath = new URL("../../../../Design/Config/GameConfig.xlsx", import.meta.url).pathname;
const outputDir = new URL("./outputs", import.meta.url).pathname;
const previewDir = new URL("./previews", import.meta.url).pathname;
const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);

function requireCell(sheetName, address, expected) {
  const cell = workbook.worksheets.getItem(sheetName).getRange(address);
  const actual = cell.values[0][0];
  if (actual !== expected) {
    throw new Error(`${sheetName}!${address} expected ${expected}, found ${actual}`);
  }
  return cell;
}

function requireAbsent(sheetName, rangeAddress, firstColumnValues) {
  const rows = workbook.worksheets.getItem(sheetName).getRange(rangeAddress).values;
  for (const value of firstColumnValues) {
    if (rows.some((row) => row[0] === value)) {
      throw new Error(`${sheetName}.${value} already exists`);
    }
  }
}

requireCell("README", "B5", 4);
requireCell("README", "B6", "0.4.0-sample").values = [["0.5.0-sample"]];
requireCell("Global", "C5", 4);
requireCell("Global", "E6", "0.4.0-sample").values = [["0.5.0-sample"]];

const globalKeys = [
  "projectile_pool_prewarm_per_type",
  "enemy_pool_exhaustion_policy",
  "projectile_pool_exhaustion_policy",
  "vfx_pool_exhaustion_policy",
  "damage_number_pool_exhaustion_policy",
];
requireAbsent("Global", "A5:H100", globalKeys);
const global = workbook.worksheets.getItem("Global");
for (const [targetRow, sourceRow] of [[17, 15], [18, 16], [19, 15], [20, 16], [21, 15]]) {
  global.getRange(`A${targetRow}:H${targetRow}`).copyFrom(
    global.getRange(`A${sourceRow}:H${sourceRow}`),
    "all",
  );
}
global.getRange("A17:H21").values = [
  ["projectile_pool_prewarm_per_type", "int", 8, null, null, null, "count", "每种投射物池预热数量"],
  ["enemy_pool_exhaustion_policy", "string", null, null, "Reject", null, "enum", "敌人池耗尽时拒绝本次生成"],
  ["projectile_pool_exhaustion_policy", "string", null, null, "Reject", null, "enum", "投射物池耗尽时拒绝本次生成"],
  ["vfx_pool_exhaustion_policy", "string", null, null, "ReuseOldest", null, "enum", "VFX池耗尽时复用最早活动项"],
  ["damage_number_pool_exhaustion_policy", "string", null, null, "ReuseOldest", null, "enum", "伤害数字池耗尽时复用最早活动项"],
];
global.getRange("A1:A100").format.columnWidthPx = 380;

requireAbsent("Enums", "A5:D200", ["PoolExhaustionPolicy"]);
const enums = workbook.worksheets.getItem("Enums");
enums.getRange("A97:D97").copyFrom(enums.getRange("A95:D95"), "all");
enums.getRange("A98:D98").copyFrom(enums.getRange("A96:D96"), "all");
enums.getRange("A97:D98").values = [
  ["PoolExhaustionPolicy", "Reject", "Reject", "达到配置活动容量时拒绝本次租用，不扩容也不回收活动玩法对象。"],
  ["PoolExhaustionPolicy", "ReuseOldest", "ReuseOldest", "达到配置活动容量时先完整回收最早活动项，再服务本次租用。"],
];
enums.getRange("D1:D200").format.columnWidthPx = 500;

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

for (const [sheetId, range] of [
  ["README", "A4:E10"],
  ["Global", "A4:H23"],
  ["Enums", "A90:D100"],
]) {
  const inspection = await workbook.inspect({
    kind: "region,computedStyle",
    sheetId,
    range,
    maxChars: 12000,
    tableMaxRows: 24,
    tableMaxCols: 10,
    tableMaxCellChars: 180,
  });
  console.log(`\n### ${sheetId} ${range}\n${inspection.ndjson}`);
}

const formulaErrors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "T440 final formula error scan",
});
console.log(`\n### Formula errors\n${formulaErrors.ndjson}`);

for (const sheetName of ["README", "Global", "Enums"]) {
  const preview = await workbook.render({
    sheetName,
    autoCrop: "all",
    scale: 1,
    format: "png",
  });
  await fs.writeFile(
    `${previewDir}/${sheetName}.png`,
    new Uint8Array(await preview.arrayBuffer()),
  );
}

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outputDir}/GameConfig.xlsx`);
