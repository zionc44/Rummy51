import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

// Placeholder: the form arrives in the next step.
@Component({
  selector: 'app-login',
  imports: [RouterLink, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="flex min-h-dvh flex-col items-center justify-center gap-6 bg-felt-950 px-6 text-white">
      <h1 class="text-3xl font-bold">{{ 'landing.login' | translate }}</h1>
      <a routerLink="/" class="text-felt-500 underline underline-offset-4">{{ 'app.title' | translate }}</a>
    </main>
  `,
})
export class Login {}
