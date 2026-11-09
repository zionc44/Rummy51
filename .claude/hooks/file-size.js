// An alarm, not a limit.
//
// Nothing checks the rules in CLAUDE.md. They are read once at the start of a
// session and then compete with whatever problem is in front of whoever is
// writing - and a file does not reach six hundred lines in one go. It grows a
// working, reasonable piece at a time, each one small enough to look fine
// where it was put, and nobody ever sees the total.
//
// So this counts, and says so. The number is not the rule and is not a budget
// to write up to: it is only how the question gets asked, and the question is
// the one CLAUDE.md already asks. Nothing is blocked. The edit has landed by
// the time this runs; all it does is make somebody look.

const fs = require('node:fs');
const path = require('node:path');

/**
 * Where the rules about one job and one component apply. The server has its
 * own shape, and the corner deck is generated rather than written.
 */
const Watched = 'rummy51.client/src/app';

/**
 * Measured from the project rather than chosen: the largest file here that is
 * honestly one job is the avatar cropper at 221 lines, and the longest
 * template is the table at 144. Above those, and far below the two that grew
 * past being readable. A stylesheet is left out - a long one is usually just
 * a long one.
 */
const Alarm = { '.ts': 250, '.html': 180 };

/** What a file of each kind should be asked, in the words of the rule. */
const Ask = {
  '.ts':
    'CLAUDE.md: one thing, and the name says what. A component either arranges other components or renders one thing, and logic that is not presentation belongs in a service.',
  '.html': 'CLAUDE.md: a template that needs a comment to follow is two components.',
};

/** Windows spells a path one way and Git Bash another; this ends that. */
const asPosix = (file) => file.replaceAll('\\', '/');

const alarmFor = (file) => Alarm[path.extname(file)];

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

const watched = (file) => asPosix(file).includes(Watched) && alarmFor(file) !== undefined;

/** As a line counter counts them: the last line needs no newline after it. */
function lines(file) {
  const text = fs.readFileSync(file, 'utf8');
  const broken = text.split('\n').length;

  return text.endsWith('\n') ? broken - 1 : broken;
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

try {
  check();
} catch {
  // A file that has moved, or input in a shape this does not know. An alarm
  // that fails loudly would be worse than the growth it watches for.
}
