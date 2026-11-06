import { signal } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Observable } from 'rxjs';
import { ApiError } from '../errors/api-error';

/**
 * The part of submitting a form that never varies: refuse an invalid one and
 * show why, hold a pending flag while the server answers, and keep whatever it
 * reported where the template can read it. What a success means is the only
 * thing each form still decides for itself.
 */
export class FormSubmission {
  private readonly pending = signal(false);
  private readonly failures = signal<readonly ApiError[]>([]);

  readonly submitting = this.pending.asReadonly();
  readonly errors = this.failures.asReadonly();

  constructor(private readonly form: FormGroup) {}

  /** The request is a function so that an invalid form never builds one. */
  send<T>(request: () => Observable<T>, accept: (value: T) => void): void {
    this.failures.set([]);

    if (this.form.invalid) {
      // Nothing was touched if the player pressed enter straight away, and an
      // untouched control stays quiet. Mark them so the reasons appear.
      this.form.markAllAsTouched();
      return;
    }

    this.pending.set(true);

    request().subscribe({
      next: (value) => {
        this.pending.set(false);
        accept(value);
      },
      error: (errors: ApiError[]) => {
        this.failures.set(errors);
        this.pending.set(false);
      },
    });
  }
}
