import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

/**
 * A failure as the server reports it. The server never sends display text —
 * only a stable code and the values a translated message needs — so the
 * wording lives in the i18n files and follows the chosen language.
 */
export interface ApiError {
  readonly code: string;
  readonly parameters: readonly string[];
}

/** Returned when the server could not be reached at all. */
export const NETWORK_ERROR: ApiError = { code: 'NetworkError', parameters: [] };

/** Returned when a response carried no codes we can show. */
export const UNEXPECTED_ERROR: ApiError = { code: 'UnexpectedError', parameters: [] };

export const apiErrorsOf = (response: HttpErrorResponse): readonly ApiError[] => {
  // status 0 is the browser saying the request never completed: the server is
  // down, or the connection dropped. There is no body to read.
  if (response.status === 0) {
    return [NETWORK_ERROR];
  }

  const errors = (response.error as { errors?: ApiError[] } | null)?.errors;

  return errors?.length ? errors : [UNEXPECTED_ERROR];
};

/**
 * ngx-translate interpolates by name, while the server sends an ordered list.
 * `["8"]` becomes `{ "0": "8" }`, so a message reads "at least {{0}} characters".
 */
export const interpolationOf = (error: ApiError): Record<string, string> =>
  Object.fromEntries(error.parameters.map((value, index) => [index, value]));

export const translationKeyOf = (error: ApiError): string => `errors.${error.code}`;

/** Replaces the transport-level error with the codes the server sent. */
export const failWithApiErrors = <T>() =>
  catchError<T, Observable<never>>((response: HttpErrorResponse) =>
    throwError(() => apiErrorsOf(response) as ApiError[]),
  );
