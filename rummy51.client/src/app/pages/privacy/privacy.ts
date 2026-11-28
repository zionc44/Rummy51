import { Location } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { PageShell } from '../../shared/page-shell/page-shell';

@Component({
  selector: 'app-privacy',
  imports: [TranslatePipe, PageShell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './privacy.html',
  styleUrl: './privacy.scss',
})
export class Privacy {
  private readonly location = inject(Location);
  private readonly router = inject(Router);

  protected readonly sections = ['collected', 'use', 'cookies', 'google', 'stored', 'deletion'];

  protected readonly contact = 'privacy@playrummy51.com';

  protected back(): void {
    // Opened from a link elsewhere, or in a tab of its own from the sign-up
    // form, the page before this one is not this site's to go back to.
    if (!this.router.lastSuccessfulNavigation()?.previousNavigation) {
      void this.router.navigateByUrl('/');
      return;
    }

    this.location.back();
  }
}
