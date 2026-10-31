import { AbstractControl } from '@angular/forms';
import { ApiError } from './api-error';

/** Maps a validator's key to the error code shown for this particular field. */
export type FieldCodes = Readonly<Record<string, string>>;

/**
 * Describes a failed control the same way the server describes a failure, so
 * one translation table and one template cover both. Validating here as well
 * as on the server is deliberate: it answers before the round trip, and the
 * server still has the final say.
 */
export const fieldErrorOf = (
  control: AbstractControl | null,
  codes: FieldCodes,
): ApiError | null => {
  // Quiet until the player has had a chance to type: complaining about an
  // empty field the moment the form appears is noise, not help.
  if (!control || control.valid || !(control.dirty || control.touched)) {
    return null;
  }

  const [key] = Object.keys(control.errors ?? {});
  const code = codes[key];

  if (!code) {
    return null;
  }

  const detail = control.errors?.[key] as { requiredLength?: number } | undefined;

  return {
    code,
    parameters: detail?.requiredLength === undefined ? [] : [String(detail.requiredLength)],
  };
};
