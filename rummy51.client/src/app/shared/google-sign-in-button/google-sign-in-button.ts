import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';

/**
 * The mark is Google's own asset from public/, referenced rather than drawn:
 * its colours are fixed and it is a trademark, which is exactly the case our
 * rules send to an img instead of an icon component.
 */
@Component({
  selector: 'app-google-sign-in-button',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './google-sign-in-button.html',
  styleUrl: './google-sign-in-button.scss',
})
export class GoogleSignInButton {
  private readonly auth = inject(AuthService);

  protected start(): void {
    this.auth.startGoogleSignIn();
  }
}
