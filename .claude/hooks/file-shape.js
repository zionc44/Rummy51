// An alarm, not a limit. Nothing enforces CLAUDE.md, and no file reaches six
// hundred lines in one go: it grows a reasonable piece at a time, each one
// right where it was put, and nobody ever sees the total. So this counts.
//
// It runs as a hook on the file just written, and with --all over everything.
// The second matters more than it sounds: a hook only ever sees what is being
// touched, so a file that grew and was then left alone is never asked about
// again, and a missed run says nothing.
//
// It measures itself. An alarm exempt from its own rule is the first thing to
// rot past it.

const fs = require('node:fs');
const path = require('node:path');

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
 * Measured from the project rather than chosen: the largest file that was
 * honestly one job was the avatar cropper at 221 lines, the longest template
 * the table at 144. Stylesheets are left out - a long one is usually just a
 * long one.
 */
const Alarm = { '.ts': 250, '.html': 180, '.cs': 250, '.js': 250 };

/**
 * How much of a file may be prose. A fifth is the line for anything of real
 * size: above that, comments have usually stopped explaining the code and
 * started restating it - or restating a rule that docs/rules.md already
 * states, which is the copy that goes stale, being the one nobody opens when
 * the rule changes.
 *
 * It slides, because a percentage of a short file is a percentage of almost
 * nothing. A thirty-line type with one honest trap in it is over a fifth and
 * is right to be, and an alarm that cries at those is an alarm nobody reads.
 */
const Share = [
  [50, 0.4],
  [100, 0.35],
  [150, 0.3],
  [200, 0.25],
  [Infinity, 0.2],
];

const shareFor = (total) => Share.find(([upTo]) => total <= upTo)[1];

/** What a file of each kind should be asked when it has grown. */
const Ask = {
  '.ts':
    'CLAUDE.md: one thing, and the name says what. A component either arranges other components or renders one thing, and logic that is not presentation belongs in a service.',
  '.html': 'CLAUDE.md: a template that needs a comment to follow is two components.',
  '.cs':
    'CLAUDE.md: one thing, and the name says what. One level of abstraction - orchestrating three calls and then parsing a string is two jobs.',
  '.js': 'CLAUDE.md: one thing, and the name says what.',
};

const AskProse =
  'CLAUDE.md: names instead of comments, and comment why rather than what. A game rule belongs in docs/rules.md and is named from the code by its number, never repeated: "// rules 5.7, 5.9" is the whole of what such a method needs. What earns its place is what the code cannot say about itself - a trap that makes correct code look wrong.';

/** Windows spells a path one way and Git Bash another; this ends that. */
const asPosix = (file) => file.replaceAll('\\', '/');

const alarmFor = (file) => Alarm[path.extname(file)];

const skipped = (file) => asPosix(file).split('/').some((part) => Skipped.has(part));

const watched = (file) =>
  alarmFor(file) !== undefined &&
  !skipped(file) &&
  Watched.some((root) => asPosix(file).includes(root));

/** As a line counter counts them: the last line needs no newline after it. */
function lines(text) {
  const broken = text.split('\n').length;

  return text.endsWith('\n') ? broken - 1 : broken;
}

/**
 * Counted by walking the file: the middle of a block says nothing about
 * itself, and a template's comments are very nearly all blocks, so reading
 * each line alone would report a fraction of what is there.
 */
function prose(text, extension) {
  const [opens, closes] = extension === '.html' ? ['<!--', '-->'] : ['/*', '*/'];
  let inside = false;
  let count = 0;

  for (const line of text.split('\n')) {
    const trimmed = line.trim();

    if (inside) {
      count += 1;
      inside = !trimmed.includes(closes);
      continue;
    }

    if (trimmed.startsWith('//')) {
      count += 1;
      continue;
    }

    if (trimmed.startsWith(opens)) {
      count += 1;
      inside = !trimmed.includes(closes);
    }
  }

  return count;
}

function measure(file) {
  const text = fs.readFileSync(file, 'utf8');
  const extension = path.extname(file);
  const total = lines(text);
  const comment = prose(text, extension);

  return {
    file,
    total,
    comment,
    alarm: Alarm[extension],
    budget: Math.floor(total * shareFor(total)),
  };
}

const share = (one) => `${Math.round((100 * one.comment) / one.total)}%`;

/** What it is, against what a file that length is allowed. */
const against = (one) => `${share(one)}, over ${Math.round(100 * shareFor(one.total))}%`;

const tooLong = (one) => one.total > one.alarm;

const tooMuchProse = (one) => one.comment > one.budget;

// --- after a write, about the one file that was written -------------------

/** Everything the hook was handed, or null if it cannot be read. */
function given() {
  try {
    return JSON.parse(fs.readFileSync(0, 'utf8'));
  } catch {
    return null;
  }
}

/** The file the tool wrote, under either of the names the hook gives it. */
function writtenFile(input) {
  const file = input?.tool_response?.filePath ?? input?.tool_input?.file_path;

  return typeof file === 'string' && file.length > 0 ? file : null;
}

/**
 * "block" is the mechanism's word and not what happens: on PostToolUse the
 * write has already landed, and the message is only fed back.
 */
function raise(reason, shown) {
  process.stdout.write(JSON.stringify({ decision: 'block', reason, systemMessage: shown }));
}

/** One message even where both are over: that is one conversation, not two. */
function check() {
  const input = given();
  const file = input === null ? null : writtenFile(input);

  if (file === null || !watched(file)) {
    return;
  }

  const one = measure(file);
  const name = path.basename(file);
  const said = [];
  const shown = [];

  if (tooLong(one)) {
    said.push(
      `${name} is now ${one.total} lines. ${Ask[path.extname(file)]} Read the whole file before adding to it again, and say whether it is still one job or has quietly become two.`,
    );
    shown.push(`${one.total} lines`);
  }

  if (tooMuchProse(one)) {
    said.push(
      `${one.comment} of ${name}'s ${one.total} lines are comment, and a file that length is allowed ${Math.round(100 * shareFor(one.total))}%. ${AskProse} Read what is there and say which of it is earning its place.`,
    );
    shown.push(`${share(one)} comment`);
  }

  if (said.length > 0) {
    raise(`${said.join(' ')} These are questions, not refusals.`, `${name}: ${shown.join(', ')}`);
  }
}

// --- on demand, about everything ------------------------------------------

const Root = path.join(__dirname, '..', '..');

function* filesUnder(dir) {
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

const named = (one) => asPosix(path.relative(Root, one.file));

/** Longest first, the length being both the ranking and the question. */
function reportLength(measured) {
  const over = measured.filter(tooLong).sort((a, b) => b.total - a.total);

  for (const one of over) {
    console.log(`${String(one.total).padStart(5)}  (over ${one.alarm})  ${named(one)}`);
  }

  console.log(
    over.length === 0
      ? `\nNothing over the line, of ${measured.length} files measured.`
      : `\n${over.length} of ${measured.length} files are over. Each one is a question, not a verdict:` +
          ` read it and say whether it is still one job.`,
  );
}

/**
 * Ranked by lines over rather than by share, so a short file with a long
 * comment reports a small number and stays out of the way of the real work.
 */
function reportProse(measured) {
  const over = measured
    .filter(tooMuchProse)
    .sort((a, b) => b.comment - b.budget - (a.comment - a.budget));

  for (const one of over) {
    const past = String(one.comment - one.budget).padStart(4);

    console.log(`${past} over   ${named(one)}   ${one.comment} of ${one.total} (${against(one)})`);
  }

  console.log(
    over.length === 0
      ? `Nothing over its share of prose.`
      : `\n${over.length} of ${measured.length} files carry more prose than their length allows. A rule belongs in` +
          ` docs/rules.md, named from the code by its number; what stays is what the code cannot say` +
          ` about itself.`,
  );
}

function sweep() {
  const measured = [...filesUnder(Root)].map(measure);

  reportLength(measured);
  console.log('');
  reportProse(measured);
}

// --- which of the two ------------------------------------------------------

if (process.argv.includes('--all')) {
  sweep();
} else {
  try {
    check();
  } catch {
    // A file that has moved, or input in a shape this does not know. An alarm
    // that fails loudly would be worse than the growth it watches for.
  }
}
