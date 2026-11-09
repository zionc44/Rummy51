// An alarm, not a limit.
//
// Nothing enforces the rules in CLAUDE.md. They are read once at the start of
// a session and then compete with whatever problem is in front of whoever is
// writing, and a file does not reach six hundred lines in one go. It grows a
// working, reasonable piece at a time, each one small enough to look right
// where it was put, and nobody ever sees the total.
//
// So this counts, and says so. The number is not the rule and is not a budget
// to write up to: it is only how the question gets asked, and the question is
// the one CLAUDE.md already asks. Nothing is blocked either - the write has
// landed by the time this runs, and all it does is make somebody look.
//
// Two ways to run it:
//
//   as a hook   after every write, on the one file that was written
//   --all       over everything at once, which is the other half
//
// The second matters more than it sounds. A hook only ever sees what is being
// touched, so a file that grew and was then left alone is never asked about
// again - and if the hook ever misses a run, nothing says so. The sweep is
// what makes a deliberate look cheap: it says which files to read rather than
// asking anybody to read them all.

const fs = require('node:fs');
const path = require('node:path');

/** Everything written by hand here. Anything else is generated or installed. */
const Watched = [
  'rummy51.client/src/app',
  'Rummy51.Domain',
  'Rummy51.Application',
  'Rummy51.Infrastructure',
  'Rummy51.Server',
];

/** Not ours to judge: built output, installed packages, generated migrations. */
const Skipped = new Set(['node_modules', 'obj', 'bin', 'dist', 'Migrations', '.git', '.vs']);

/**
 * Measured from the project rather than chosen. On the client the largest
 * file that is honestly one job is the avatar cropper at 221 lines and the
 * longest template is the table at 144; on the server it is the avatar
 * service at 209. Above all of those, and far below anything that had
 * stopped being one thing. Stylesheets are left out - a long one is usually
 * just a long one.
 */
const Alarm = { '.ts': 250, '.html': 180, '.cs': 250 };

/** What a file of each kind should be asked, in the words of the rule. */
const Ask = {
  '.ts':
    'CLAUDE.md: one thing, and the name says what. A component either arranges other components or renders one thing, and logic that is not presentation belongs in a service.',
  '.html': 'CLAUDE.md: a template that needs a comment to follow is two components.',
  '.cs':
    'CLAUDE.md: one thing, and the name says what. One level of abstraction - orchestrating three calls and then parsing a string is two jobs.',
};

/** Windows spells a path one way and Git Bash another; this ends that. */
const asPosix = (file) => file.replaceAll('\\', '/');

const alarmFor = (file) => Alarm[path.extname(file)];

const skipped = (file) => asPosix(file).split('/').some((part) => Skipped.has(part));

const watched = (file) =>
  alarmFor(file) !== undefined &&
  !skipped(file) &&
  Watched.some((root) => asPosix(file).includes(root));

/** As a line counter counts them: the last line needs no newline after it. */
function lines(file) {
  const text = fs.readFileSync(file, 'utf8');
  const broken = text.split('\n').length;

  return text.endsWith('\n') ? broken - 1 : broken;
}

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
 * Said back to Claude, and shown to whoever is watching. "block" is the
 * mechanism's word and not what happens: on PostToolUse the write is already
 * done, and the message is only fed back.
 */
function raise(file, count) {
  const name = path.basename(file);

  process.stdout.write(
    JSON.stringify({
      decision: 'block',
      reason: `${name} is now ${count} lines. ${Ask[path.extname(file)]} Read the whole file before adding to it again, and say whether it is still one job or has quietly become two. If it is still one, say so in a line and carry on - this is a question, not a refusal.`,
      systemMessage: `${name}: ${count} lines - is it still one job?`,
    }),
  );
}

function check() {
  const input = given();
  const file = input === null ? null : writtenFile(input);

  if (file === null || !watched(file)) {
    return;
  }

  const count = lines(file);

  if (count > alarmFor(file)) {
    raise(file, count);
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

function sweep() {
  const measured = [...filesUnder(Root)]
    .map((file) => ({ file, count: lines(file), alarm: alarmFor(file) }))
    .sort((one, two) => two.count - one.count);

  const over = measured.filter((one) => one.count > one.alarm);

  for (const { file, count, alarm } of over) {
    console.log(`${String(count).padStart(5)}  (over ${alarm})  ${asPosix(path.relative(Root, file))}`);
  }

  console.log(
    over.length === 0
      ? `\nNothing over the line, of ${measured.length} files measured.`
      : `\n${over.length} of ${measured.length} files are over. Each one is a question, not a verdict:` +
          ` read it and say whether it is still one job.`,
  );
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
