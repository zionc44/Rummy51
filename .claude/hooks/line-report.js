// What the project is made of, counted.
//
// Neither alarm says how things are going. One names the files that are over
// a line and the other names the rules nothing points at, and both are about
// what is wrong. This is the other kind of number: where the lines are, what
// share of them is code, and which files carry the most of it - so that two
// readings a month apart can be put side by side and say which way the
// project is moving.
//
//   node .claude/hooks/line-report.js          the layers, and the twenty biggest
//   node .claude/hooks/line-report.js --all    every file

const { Watched, asPosix, filesUnder, read } = require('./files');

/** Enough to see the shape of it without scrolling past the point. */
const Biggest = 20;

const share = (part, whole) => (whole === 0 ? 0 : Math.round((100 * part) / whole));

const sum = (files, of) => files.reduce((all, one) => all + one[of], 0);

/** Which of the watched roots a file sits under, which is its layer. */
const layerOf = (file) => Watched.find((root) => asPosix(file.file).includes(root)) ?? '?';

function row(name, files) {
  const total = sum(files, 'total');

  return [
    name.padEnd(30),
    String(files.length).padStart(5),
    String(total).padStart(7),
    String(sum(files, 'code')).padStart(7),
    String(sum(files, 'comment')).padStart(8),
    String(sum(files, 'blank')).padStart(6),
    `${String(share(sum(files, 'code'), total)).padStart(5)}%`,
  ].join(' ');
}

function layers(measured) {
  console.log(
    ['', 'files', 'lines', 'code', 'comment', 'blank', 'code'].map((head, at) =>
      head.padStart([0, 5, 7, 7, 8, 6, 6][at] + (at === 0 ? 30 : 1)),
    ).join(''),
  );

  const by = new Map();

  for (const one of measured) {
    const layer = layerOf(one);

    by.set(layer, [...(by.get(layer) ?? []), one]);
  }

  for (const root of Watched) {
    const files = by.get(root);

    if (files !== undefined) {
      console.log(row(root, files));
    }
  }

  console.log(''.padEnd(72, '-'));
  console.log(row('all', measured));
}

function biggest(measured, howMany) {
  console.log(`\n${howMany === null ? 'Every file' : `The ${howMany} biggest`}, longest first:\n`);

  const sorted = [...measured].sort((a, b) => b.total - a.total);

  for (const one of howMany === null ? sorted : sorted.slice(0, howMany)) {
    console.log(
      `${String(one.total).padStart(5)} lines ${String(share(one.code, one.total)).padStart(4)}%` +
        ` code   ${one.name}`,
    );
  }
}

const measured = [...filesUnder()].map(read);

layers(measured);
biggest(measured, process.argv.includes('--all') ? null : Biggest);
