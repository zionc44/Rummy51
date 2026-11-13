// Which files are ours to judge, and how to read one.
//
// Three tools want the same walk - the shape alarm, the rule-link check and
// the line report - so it is here rather than three times. Each of them puts
// its own questions on top; none of them should have an opinion about what a
// comment looks like or which directories are somebody else's.

const fs = require('node:fs');
const path = require('node:path');

const Root = path.join(__dirname, '..', '..');

/** Everything written by hand here. Anything else is generated or installed. */
const Watched = [
  '.claude/hooks',
  'rummy51.client/src/app',
  'Rummy51.Domain',
  'Rummy51.Application',
  'Rummy51.Infrastructure',
  'Rummy51.Server',
];

/** Not ours to judge: built output, installed packages, generated migrations. */
const Skipped = new Set(['node_modules', 'obj', 'bin', 'dist', 'Migrations', '.git', '.vs']);

/**
 * Stylesheets are left out. A long one is usually just a long one, and the
 * prose in one is nearly always a heading saying which part of the page is
 * below it.
 */
const Kinds = new Set(['.ts', '.html', '.cs', '.js']);

/** Windows spells a path one way and Git Bash another; this ends that. */
const asPosix = (file) => file.replaceAll('\\', '/');

const named = (file) => asPosix(path.relative(Root, file));

const skipped = (file) => asPosix(file).split('/').some((part) => Skipped.has(part));

const watched = (file) =>
  Kinds.has(path.extname(file)) &&
  !skipped(file) &&
  Watched.some((root) => asPosix(file).includes(root));

function* filesUnder(dir = Root) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      if (!Skipped.has(entry.name)) {
        yield* filesUnder(full);
      }
    } else if (watched(full)) {
      yield full;
    }
  }
}

/**
 * A file counted and its comments gathered, in one pass. Blocks are walked
 * rather than each line read on its own: the middle of a block says nothing
 * about itself, and a template's comments are very nearly all blocks.
 *
 * `blocks` keeps each run of comment together, because a sentence that
 * started on one line is still the same sentence on the next.
 */
function read(file) {
  const text = fs.readFileSync(file, 'utf8');
  const [opens, closes] = path.extname(file) === '.html' ? ['<!--', '-->'] : ['/*', '*/'];
  const lines = text.split('\n');
  const blocks = [];
  let running = [];
  let inside = false;
  let comment = 0;

  const breakOff = () => {
    if (running.length > 0) {
      blocks.push(running.join(' '));
      running = [];
    }
  };

  for (const line of lines) {
    const trimmed = line.trim();
    const open = !inside && trimmed.startsWith(opens);

    if (inside || open || trimmed.startsWith('//')) {
      comment += 1;
      running.push(trimmed);
      inside = (inside || open) && !trimmed.includes(closes);
      continue;
    }

    breakOff();
  }

  breakOff();

  const total = text.endsWith('\n') ? lines.length - 1 : lines.length;
  const blank = lines.slice(0, total).filter((line) => line.trim() === '').length;

  return { file, name: named(file), total, comment, blank, code: total - comment - blank, blocks };
}

module.exports = { Root, Watched, Skipped, Kinds, asPosix, named, watched, filesUnder, read };
