import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AccountService } from '../../../core/account/account-service';
import { AuthService } from '../../../core/auth/auth-service';
import { AvatarSource, PlayerSummary } from '../../../core/auth/auth.models';
import { ApiError } from '../../../core/errors/api-error';
import { AvatarCropper } from '../../../shared/avatar-cropper/avatar-cropper';
import { ErrorMessages } from '../../../shared/error-messages/error-messages';
import { PlayerAvatar } from '../../../shared/player-avatar/player-avatar';

/**
 * The picture other players see. A provider's and the player's own can both
 * exist at once, and when they do the player says which of them to show.
 */
@Component({
  selector: 'app-avatar-form',
  imports: [TranslatePipe, AvatarCropper, ErrorMessages, PlayerAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './avatar-form.html',
  styleUrl: './avatar-form.scss',
})
export class AvatarForm {
  private readonly auth = inject(AuthService);
  private readonly account = inject(AccountService);

  protected readonly player = this.auth.currentPlayer;

  /** Set once a file is picked, which is what raises the cropper. */
  protected readonly chosen = signal<File | null>(null);
  protected readonly busy = signal(false);
  protected readonly errors = signal<readonly ApiError[]>([]);

  protected readonly canChoose = computed(() => {
    const me = this.player();

    return me?.providerAvatarUrl != null && me.uploadedAvatarUrl != null;
  });

  protected pick(picker: HTMLInputElement): void {
    const file = picker.files?.[0] ?? null;

    // Cleared so that picking the same file again still counts as a change.
    picker.value = '';

    this.chosen.set(file);
  }

  protected upload(image: Blob): void {
    this.chosen.set(null);
    this.apply(this.account.uploadAvatar(image));
  }

  protected remove(): void {
    this.apply(this.account.removeAvatar());
  }

  protected choose(source: AvatarSource): void {
    this.apply(this.account.chooseAvatar(source));
  }

  private apply(change: Observable<PlayerSummary>): void {
    this.errors.set([]);
    this.busy.set(true);

    change.subscribe({
      next: () => this.busy.set(false),
      error: (errors: ApiError[]) => {
        this.errors.set(errors);
        this.busy.set(false);
      },
    });
  }
}
