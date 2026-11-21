import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import {
  NETWORK_ERROR,
  UNEXPECTED_ERROR,
  apiErrorsOf,
  interpolationOf,
  translationKeyOf,
} from './api-error';

const responded = (status: number, body: unknown) =>
  new HttpErrorResponse({ status, error: body });

/**
 * The client's half of the error contract. The server never sends display
 * text, so everything here is about turning a code into something the
 * translation table can be asked for - and about the two cases where there
 * is no code to turn.
 */
describe('a failure the server reported', () => {
  it('is the codes it sent, in the order it sent them', () => {
    const errors = apiErrorsOf(
      responded(400, {
        errors: [
          { code: 'EmailRequired', parameters: [] },
          { code: 'PasswordTooShort', parameters: ['8'] },
        ],
      }),
    );

    expect(errors.map((error) => error.code)).toEqual(['EmailRequired', 'PasswordTooShort']);
    expect(errors[1].parameters).toEqual(['8']);
  });

  it('status nought is the server never answering, which has no body to read', () => {
    expect(apiErrorsOf(responded(0, null))).toEqual([NETWORK_ERROR]);
  });

  it('and a response carrying no codes is one the screen cannot word', () => {
    expect(apiErrorsOf(responded(500, null))).toEqual([UNEXPECTED_ERROR]);
    expect(apiErrorsOf(responded(400, { errors: [] }))).toEqual([UNEXPECTED_ERROR]);
    expect(apiErrorsOf(responded(400, 'a page of html'))).toEqual([UNEXPECTED_ERROR]);
  });
});

describe('wording one', () => {
  it('a code is a key under errors.', () => {
    expect(translationKeyOf({ code: 'EmailTaken', parameters: [] })).toBe('errors.EmailTaken');
  });

  it('and the ordered parameters become {{0}}, {{1}}', () => {
    expect(interpolationOf({ code: 'Between', parameters: ['8', '64'] })).toEqual({
      0: '8',
      1: '64',
    });
  });

  it('a code that needs none interpolates nothing', () => {
    expect(interpolationOf({ code: 'EmailRequired', parameters: [] })).toEqual({});
  });
});
