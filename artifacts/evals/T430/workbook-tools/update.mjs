import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const inputPath = new URL("../../../../Design/Config/GameConfig.xlsx", import.meta.url).pathname;
const outputDir = new URL("./outputs", import.meta.url).pathname;
const previewDir = new URL("./previews/updated", import.meta.url).pathname;
const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);

function requireSingleRow(sheetName, rangeAddress, firstColumnValue) {
  const sheet = workbook.worksheets.getItem(sheetName);
  const range = sheet.getRange(rangeAddress);
  const values = range.values;
  const matches = [];
  for (let index = 0; index < values.length; index += 1) {
    if (values[index][0] === firstColumnValue) {
      matches.push(index);
    }
  }
  if (matches.length !== 1) {
    throw new Error(`${sheetName}.${firstColumnValue} expected once, found ${matches.length}`);
  }
  return { sheet, row: range.getRow(matches[0]), values: values[matches[0]] };
}

const global = workbook.worksheets.getItem("Global");
global.getRange("C5").values = [[4]];
global.getRange("E6").values = [["0.4.0-sample"]];

const readme = workbook.worksheets.getItem("README");
readme.getRange("B5").values = [[4]];
readme.getRange("B6").values = [["0.4.0-sample"]];
readme.getRange("B8").values = [["Assets/_Game/Config/Generated/gameplay_config.json"]];
readme.getRange("E10").formulas = [["=COUNTA('Buffs'!A5:A1000)"]];
readme.getRange("E17").formulas = [["=COUNTA('Texts'!A5:A1000)"]];

const buffs = workbook.worksheets.getItem("Buffs");
const buffValues = buffs.getRange("A1:I200").values;
if (!buffValues.some((row) => row[0] === "buff_shield_50")) {
  buffs.getRange("A10:I10").copyFrom(buffs.getRange("A9:I9"), "all");
  buffs.getRange("A10:I10").values = [[
    "buff_shield_50",
    "DamageReduction",
    3,
    1,
    0.5,
    0,
    "Refresh",
    "vfx_puppet_shield",
    "text_buff_shield",
  ]];
}

const shieldEffect = requireSingleRow(
  "SkillEffects",
  "A1:K200",
  "fx_puppet_shield",
);
shieldEffect.row.values = [[
  "fx_puppet_shield",
  1,
  "ApplyBuff",
  "Target",
  0,
  0,
  0,
  "buff_shield_50",
  "vfx_puppet_shield",
  "",
  "",
]];

const enums = workbook.worksheets.getItem("Enums");
const enumValues = enums.getRange("A1:D300").values;
if (!enumValues.some((row) => row[0] === "BuffType" && row[1] === "DamageReduction")) {
  enums.getRange("A96:D96").copyFrom(enums.getRange("A93:D93"), "all");
  enums.getRange("A96:D96").values = [[
    "BuffType",
    "DamageReduction",
    "DamageReduction",
    "降低受到的伤害；magnitude 为减伤比例，叠层后按 1-magnitude×stacks 计算并钳制到非负。",
  ]];
}

const texts = workbook.worksheets.getItem("Texts");
const textValues = texts.getRange("A1:D200").values;
if (!textValues.some((row) => row[0] === "text_buff_shield")) {
  texts.getRange("A41:D41").copyFrom(texts.getRange("A40:D40"), "all");
  texts.getRange("A41:D41").values = [[
    "text_buff_shield",
    "护盾",
    "Shield",
    "Buff",
  ]];
}

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(`${outputDir}/GameConfig.xlsx`);

for (const sheetName of ["README", "Global", "Buffs", "SkillEffects", "Texts", "Enums"]) {
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

for (const [sheetId, range] of [
  ["README", "A4:E12"],
  ["Global", "A4:H16"],
  ["Buffs", "A4:I12"],
  ["SkillEffects", "A24:K30"],
  ["Texts", "A35:D45"],
  ["Enums", "A88:D100"],
]) {
  const region = await workbook.inspect({
    kind: "region,computedStyle",
    sheetId,
    range,
    maxChars: 12000,
    tableMaxRows: 20,
    tableMaxCols: 12,
    tableMaxCellChars: 160,
  });
  console.log(`\n### ${sheetId} ${range}\n${region.ndjson}`);
}
