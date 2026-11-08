// Builds the deck the player's own hand is read from, out of the deck
// everything else uses.
//
// A hand is held overlapped, so all that shows of a card is the corner that
// names it - and the corner the source deck draws is a ninth of the card's
// height, which comes to eight pixels on a phone. This deck trades the
// middle of the card for that corner: both indices grow and are drawn with
// a heavier pen, and the pips and the picture between them shrink to stay
// out of their way.
//
// Generated rather than drawn, so that a change to the source deck can be
// carried over by running this again:
//
//   npm run cards

const fs = require('node:fs');
const path = require('node:path');

const Source = path.join(__dirname, 'public', 'cards');
const Target = path.join(Source, 'corners');

/**
 * Where the source deck puts the index, the same on all fifty-four cards:
 * a rank in the corner and a suit below it. The numbers are how the cards
 * are found, so they are also what says a card has been understood.
 */
const Rank = { size: 32, x: -114.4, y: -156 };
const Suit = { size: 26.769, x: -111.784, y: -119 };

/**
 * Twice the corner leaves a rank of seventeen pixels inside the thirty-four
 * a card shows of itself. Smaller than it was, and easier to read for it:
 * weight carries at this size better than height, and a shorter index needs
 * less of the card. The middle takes back exactly the room it gave up.
 */
const Corner = 2;
const Middle = 0.625;

/**
 * A rank is a stroked path and not a letter, so its weight is one number.
 * Eighty is a hairline once the glyph is read from a sliver of a card held
 * among fourteen others; this is the same shape drawn with a heavier pen.
 *
 * Every glyph reaches exactly four hundred and sixty into a box that ends
 * at five hundred, so eighty is also the widest pen that fits inside it.
 * Anything heavier is cut off at the top of the digit unless the box is
 * told to stop clipping, which is what the style below does - a stylesheet
 * rule, because the browser's own says to clip and an attribute loses to it.
 */
const Weight = 140;

/** The backs name nothing, so there is nothing in them to enlarge. */
const Backs = ['1B.svg', '2B.svg'];

const round = (value) => Number(value.toFixed(3));

/** Grown away from the corner it sits in, so it reaches into the card. */
function grow(element, from) {
  const size = round(from.size * Corner);
  const x = round(Rank.x + (from.x - Rank.x) * Corner);
  const y = round(Rank.y + (from.y - Rank.y) * Corner);

  return element
    .replace(/height="[-\d.]+"/, `height="${size}"`)
    .replace(/width="[-\d.]+"/, `width="${size}"`)
    .replace(/x="[-\d.]+"/, `x="${x}"`)
    .replace(/y="[-\d.]+"/, `y="${y}"`);
}

/**
 * The rank's own symbol and nothing else: a suit is filled rather than
 * stroked, and the picture on a court card is a picture.
 */
function thicken(defs) {
  return defs.replace(/<symbol id="V[^"]*"[\s\S]*?<\/symbol>/, (rank) =>
    rank
      .replace(/stroke-width="[\d.]+"/g, `stroke-width="${Weight}"`)
      .replace(/^<symbol /, '<symbol style="overflow:visible" '),
  );
}

const isRank = (element) => element.includes(`x="${Rank.x}"`) && element.includes(`y="${Rank.y}"`);
const isSuit = (element) => element.includes(`x="${Suit.x}"`) && element.includes(`y="${Suit.y}"`);

/** The elements of one side of the card, told apart by where they sit. */
function sort(elements) {
  const sorted = { rank: [], suit: [], body: [] };

  for (const element of elements) {
    if (isRank(element)) sorted.rank.push(grow(element, Rank));
    else if (isSuit(element)) sorted.suit.push(grow(element, Suit));
    else sorted.body.push(element);
  }

  return sorted;
}

/** Every top-level element, in the order the card draws them. */
function elementsOf(markup) {
  const elements = [];
  let rest = markup;

  while (rest.length > 0) {
    const match = /^(<g\b[^>]*>[\s\S]*?<\/g>|<[a-z]+\b[^>]*(?:\/>|>[\s\S]*?<\/[a-z]+>))/.exec(rest);

    if (match === null) throw new Error(`cannot read: ${rest.slice(0, 80)}`);

    elements.push(match[1]);
    rest = rest.slice(match[1].length).trimStart();
  }

  return elements;
}

/** Drawn last, so that nothing the middle gave up can cross it. */
function indexOn(side) {
  return side.rank.join('') + side.suit.join('');
}

function shrink(elements, turned) {
  if (elements.length === 0) return '';

  const turn = turned ? 'rotate(180) ' : '';

  return `<g transform="${turn}scale(${Middle})">${elements.join('')}</g>`;
}

function rebuild(card) {
  const cut = /<rect width="239"[^>]*>(?:<\/rect>)?/.exec(card);

  if (cut === null) throw new Error('no card face to build on');

  const head = card.slice(0, cut.index + cut[0].length);
  const rest = card.slice(cut.index + cut[0].length).replace(/<\/svg>\s*$/, '');

  const upright = { rank: [], suit: [], body: [] };
  const turned = { rank: [], suit: [], body: [] };

  for (const element of elementsOf(rest.trim())) {
    if (!element.startsWith('<g')) {
      const one = sort([element]);
      upright.rank.push(...one.rank);
      upright.suit.push(...one.suit);
      upright.body.push(...one.body);
      continue;
    }

    const inside = /^<g\b[^>]*>([\s\S]*)<\/g>$/.exec(element)[1];
    const sorted = sort(elementsOf(inside.trim()));

    turned.rank.push(...sorted.rank);
    turned.suit.push(...sorted.suit);
    turned.body.push(...sorted.body);
  }

  return [
    thicken(head),
    shrink(upright.body, false),
    shrink(turned.body, true),
    indexOn(upright),
    turned.rank.length + turned.suit.length > 0
      ? `<g transform="rotate(180)">${indexOn(turned)}</g>`
      : '',
    '</svg>',
  ].join('');
}

fs.mkdirSync(Target, { recursive: true });

let built = 0;

for (const file of fs.readdirSync(Source).filter((name) => name.endsWith('.svg'))) {
  const card = fs.readFileSync(path.join(Source, file), 'utf8');

  fs.writeFileSync(path.join(Target, file), Backs.includes(file) ? card : rebuild(card));
  built += 1;
}

console.log(`${built} cards written to ${path.relative(__dirname, Target)}`);
