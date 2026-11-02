import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { PageShell } from '../../shared/page-shell/page-shell';

@Component({
  selector: 'app-landing',
  imports: [RouterLink, TranslatePipe, PageShell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing {}
