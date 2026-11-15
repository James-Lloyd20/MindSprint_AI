// Chạy: node tools/vocab/extract-vocab.cjs ../../MindSprint-AI-D-n/frontend Data/Vocab/vocab.json
// Đọc vocab_data.js + defaultFlashcards trong script.js của nhóm (chỉ đọc) → một tệp JSON gọn.
const fs = require('fs'), vm = require('vm');
const [, , dir, out] = process.argv;
const ctx = {};
vm.runInNewContext(fs.readFileSync(dir + '/vocab_data.js', 'utf8') + '\nthis.OXFORD = OXFORD_VOCAB_DATA;', ctx);
const script = fs.readFileSync(dir + '/script.js', 'utf8');
const m = script.match(/const defaultFlashcards = (\[[\s\S]*?\n    \]);/);
const defaults = m ? vm.runInNewContext('(' + m[1] + ')') : [];
const names = { english: 'Tiếng Anh', programming: 'Lập trình', general: 'Kiến thức chung' };
const cards = [...defaults.map(c => ({ ...c, subCategory: c.subCategory || 'Cơ bản' })), ...ctx.OXFORD]
  .filter(c => names[c.category] && c.question && c.answer)
  .map(c => ({ c: c.category, s: c.subCategory.trim(), q: String(c.question).trim(), a: String(c.answer).trim(), e: c.example ? String(c.example).trim() : null }));
fs.writeFileSync(out, JSON.stringify({ source: 'MindSprint-AI-D-n frontend/vocab_data.js (3000 từ Oxford theo chủ đề, ZIM Academy) + defaultFlashcards trong script.js', categories: names, cards }));
const by = {}; cards.forEach(c => by[c.c] = (by[c.c] || 0) + 1);
console.log('defaults', defaults.length, 'total', cards.length, JSON.stringify(by), 'bytes', fs.statSync(out).size);
