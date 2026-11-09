// @ts-check

// What the written rules cannot check themselves.
//
// CLAUDE.md says how the code here should read, and a hook counts the lines
// of a file and asks whether it is still one job. Neither of them can see
// inside a function. This can: it reads the code as a tree, so it knows what
// is nested in what and which promise nobody waited for.
//
// The list of our own rules below is deliberately short. A hundred rules
// switched on and then half switched off again is a file nobody understands;
// each one here answers to a line already written in CLAUDE.md, and if it
// ever gets in the way the right move is to delete it rather than to widen
// it.

const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

module.exports = defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    languageOptions: {
      parserOptions: {
        // Reading the types as well, which is what the two rules at the end
        // of the list need: whether something is a promise cannot be seen
        // in the shape of the code alone.
        projectService: true,
        tsconfigRootDir: __dirname,
      },
    },
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'app', style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],

      // "Guard clauses over nesting." Branches first.
      'max-depth': ['error', 3],

      // And the nesting that a depth count cannot see. Three frames inside
      // a timer inside two more frames is the hardest thing to read in this
      // project, and nothing else points at it.
      'max-nested-callbacks': ['error', 3],

      // `complexity` was tried here and taken out again. It counts every `?.`
      // and `??` as a way through a function, and this project writes a lot
      // of both, so the only thing it found in the whole of it was a
      // twenty-line function that reads top to bottom and is plainly one
      // job. A rule that has to be widened until it stops speaking was not
      // saying anything.

      // A promise nobody waited for fails in silence. Say `void` where that
      // is meant, which this project already does.
      '@typescript-eslint/no-floating-promises': 'error',

      // Claiming something is not null is not knowing it. Ask, or narrow.
      '@typescript-eslint/no-non-null-assertion': 'error',

      // Every component here is already OnPush, and this is what keeps it so.
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
