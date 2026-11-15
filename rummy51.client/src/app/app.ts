import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LanguageService } from './core/i18n/language-service';
import { SignedInElsewhere } from './shared/signed-in-elsewhere/signed-in-elsewhere';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SignedInElsewhere],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // Constructing it is what applies the stored language and sets the document
  // direction, before the first page renders.
  private readonly language = inject(LanguageService);
}
