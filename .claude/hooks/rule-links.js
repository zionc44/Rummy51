// Whether the rules and the code still agree about which rules there are.
//
// Code that enforces a rule names it by its number and says nothing else
// about it, which is only safe while the number means what it meant. Two
// things can break that and neither makes a sound on its own: a rule can be
// deleted under the lines that name it, and a rule can be written and then
// never enforced.
//
// So this asks both ways round, and the second question is the interesting
// one. A rule nothing points at is a rule nobody is holding anybody to - the
// list it prints is the list of what is still open in the game, worked out
// rather than remembered.
//
// A test names rules too, and a rule named only by one is held to by nothing
// that ships, so those are counted apart.
//
//   node .claude/hooks/rule-links.js

const fs = require('node:fs');
const path = require('node:path');
const { Root, filesUnder, read } = require('./files');

const Book = path.join(Root, 'docs', 'rules.md');

/** Where the suites live, which is the one place a pointer proves nothing. */
const Suites = 'Tests/';

/** Every rule the book states, in the order it states them. */
function stated() {
  const text = fs.readFileSync(Book, 'utf8');

  return [...text.matchAll(/^\*\*(\d+\.\d+)\*\*\s+(.{0,52})/gm)].map(([, id, opening]) => ({
    id,
    opening: opening.trim(),
  }));
}

/**
 * The rules a comment names. Only a block that says "rule" or "chapter" is
 * read for numbers, so that a version, a breakpoint or a measurement written
 * out in prose is never mistaken for one.
 *
 * A chapter named without its numbers counts for nothing. It used to stand
 * for every rule in it, which reads well at the top of a file and hid an
 * open rule three times in one day. A heading is not a promise, and a file
 * that really does hold a whole chapter can say so in numbers.
 */
function namedIn(blocks) {
  const rules = new Set();

  for (const block of blocks) {
    if (!/\b(rules?|chapters?)\b/i.test(block)) {
      continue;
    }

    for (const [, id] of block.matchAll(/\b(\d{1,2}\.\d{1,2})\b/g)) {
      rules.add(id);
    }
  }

  return rules;
}

function look() {
  const rules = stated();
  const known = new Set(rules.map((rule) => rule.id));
  const pointedAt = new Map();
  const unknown = new Map();

  const note = (where, id, file) =>
    where.set(id, [...(where.get(id) ?? []), file]).get(id);

  for (const file of filesUnder()) {
    const one = read(file);

    for (const id of namedIn(one.blocks)) {
      note(known.has(id) ? pointedAt : unknown, id, one.name);
    }
  }

  return { rules, pointedAt, unknown };
}

function list(heading, rules) {
  if (rules.length === 0) {
    return;
  }

  console.log(`${heading}:`);
  console.log('');

  for (const rule of rules) {
    console.log(`  ${rule.id.padEnd(6)} ${rule.opening}...`);
  }

  console.log('');
}

function report() {
  const { rules, pointedAt, unknown } = look();

  if (unknown.size > 0) {
    console.log('Named in code, and no such rule:');
    console.log('');

    for (const [id, files] of [...unknown].sort()) {
      console.log(`  ${id.padEnd(6)} ${files.join(', ')}`);
    }

    console.log('');
  }

  const loose = rules.filter((rule) => !pointedAt.has(rule.id));

  list('Written, and nothing points at it', loose);
  list(
    'Held to by a test, and by nothing that ships',
    rules.filter(
      (rule) =>
        pointedAt.has(rule.id) &&
        pointedAt.get(rule.id).every((file) => file.startsWith(Suites)),
    ),
  );

  console.log(
    `${rules.length} rules, ${rules.length - loose.length} of them enforced somewhere.` +
      (unknown.size === 0 ? '' : ` ${unknown.size} numbers in the code name no rule.`),
  );

  return unknown.size === 0 ? 0 : 1;
}

process.exitCode = report();
