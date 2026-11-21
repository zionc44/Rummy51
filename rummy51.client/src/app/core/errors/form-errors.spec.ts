import { FormControl, Validators } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { fieldErrorOf } from './form-errors';

const codes = { required: 'PasswordRequired', minlength: 'PasswordTooShort' };

const typed = (value: string, ...validators: Validators[]) => {
  const control = new FormControl(value, validators as never);

  control.markAsTouched();

  return control;
};

/**
 * A form says what is wrong in the same codes the server does, so one
 * translation table and one component serve both. These hold it to that.
 */
describe('what a field says is wrong', () => {
  it('is a code, worded nowhere near here', () => {
    const error = fieldErrorOf(typed('', Validators.required), codes);

    expect(error?.code).toBe('PasswordRequired');
  });

  it('and carries what the message needs, in order', () => {
    const error = fieldErrorOf(typed('abc', Validators.minLength(8)), codes);

    expect(error).toEqual({ code: 'PasswordTooShort', parameters: ['8'] });
  });

  it('nothing while the field is good', () => {
    expect(fieldErrorOf(typed('a long enough one', Validators.required), codes)).toBeNull();
  });

  it('nothing before the player has had a chance to type', () => {
    const untouched = new FormControl('', Validators.required);

    expect(fieldErrorOf(untouched, codes)).toBeNull();
  });

  it('nothing for a field that is not there at all', () => {
    expect(fieldErrorOf(null, codes)).toBeNull();
  });

  it('and nothing for a failure this field has no code for', () => {
    expect(fieldErrorOf(typed('nope', Validators.email), codes)).toBeNull();
  });
});
