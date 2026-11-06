import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, input, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { IconRotate } from '../icons/icon-rotate/icon-rotate';

/** What the server stores, and what the circle is drawn from. */
const SIDE = 256;

/** How far past filling the frame the slider still goes. */
const CLOSEST = 3;

const QUARTER_TURN = 90;

@Component({
  selector: 'app-avatar-cropper',
  imports: [TranslatePipe, IconRotate],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './avatar-cropper.html',
  styleUrl: './avatar-cropper.scss',
  host: {
    // A rotated phone is a different frame, and every measurement here is in
    // the frame's own pixels.
    '(window:resize)': 'measure()',
    '(document:keydown.escape)': 'cancelled.emit()',
  },
})
export class AvatarCropper {
  readonly file = input.required<File>();

  readonly cropped = output<Blob>();
  readonly cancelled = output<void>();

  protected readonly source = signal<string | null>(null);
  protected readonly angle = signal(0);

  /** Picture pixels to frame pixels. Absolute, not a multiple of anything. */
  protected readonly scale = signal(0);
  protected readonly offset = signal({ x: 0, y: 0 });

  private readonly frameElement = viewChild.required<ElementRef<HTMLElement>>('frame');
  private readonly picture = signal<ImageBitmap | null>(null);
  private readonly frame = signal(0);
  private dragFrom: { x: number; y: number } | null = null;

  /**
   * The whole picture inside the frame, and the frame filled by it. The first
   * is where the slider starts: a portrait photograph that opened already
   * cropped gives the player no way to see what they are choosing from.
   */
  protected readonly limits = computed(() => {
    const picture = this.picture();
    const frame = this.frame();

    if (!picture || frame === 0) {
      return { whole: 1, filled: 1 };
    }

    const [width, height] = this.turned(picture);

    return { whole: frame / Math.max(width, height), filled: frame / Math.min(width, height) };
  });

  protected readonly closest = computed(() => this.limits().filled * CLOSEST);

  protected readonly placement = computed(() => {
    const picture = this.picture();
    const frame = this.frame();
    const scale = this.scale();

    if (!picture || scale === 0) {
      return null;
    }

    const width = picture.width * scale;
    const height = picture.height * scale;
    const { x, y } = this.offset();

    // Positioned by its centre and then turned about that same centre, which
    // is what the canvas does when it draws the result.
    return {
      width,
      height,
      transform: `translate(${frame / 2 + x - width / 2}px, ${frame / 2 + y - height / 2}px)`
        + ` rotate(${this.angle()}deg)`,
    };
  });

  constructor() {
    effect((onCleanup) => {
      const chosen = this.file();

      // from-image, because a phone writes its orientation into the file
      // rather than into the pixels. Without it a photograph taken upright
      // arrives lying on its side.
      void createImageBitmap(chosen, { imageOrientation: 'from-image' }).then((bitmap) => {
        this.picture.set(bitmap);
        this.showWhole();
      });

      const url = URL.createObjectURL(chosen);
      this.source.set(url);

      onCleanup(() => URL.revokeObjectURL(url));
    });
  }

  protected measure(): void {
    this.frame.set(this.frameElement().nativeElement.clientWidth);
    this.showWhole();
  }

  protected rotate(): void {
    this.angle.update((angle) => (angle + QUARTER_TURN) % 360);
    this.showWhole();
  }

  protected startDrag(event: PointerEvent): void {
    (event.target as HTMLElement).setPointerCapture(event.pointerId);

    const { x, y } = this.offset();
    this.dragFrom = { x: event.clientX - x, y: event.clientY - y };
  }

  protected drag(event: PointerEvent): void {
    if (!this.dragFrom) {
      return;
    }

    this.moveTo(event.clientX - this.dragFrom.x, event.clientY - this.dragFrom.y);
  }

  protected endDrag(): void {
    this.dragFrom = null;
  }

  protected zoomTo(value: string): void {
    this.scale.set(Number(value));

    // Zooming out can leave the picture hanging off an edge it no longer
    // reaches, so the rule that bounds a drag has to be applied again.
    const { x, y } = this.offset();
    this.moveTo(x, y);
  }

  protected save(): void {
    const picture = this.picture();
    const frame = this.frame();
    const context = squareCanvas();

    if (!picture || !context || frame === 0) {
      return;
    }

    const { x, y } = this.offset();
    const toCanvas = SIDE / frame;
    const drawn = this.scale() * toCanvas;

    context.translate(SIDE / 2 + x * toCanvas, SIDE / 2 + y * toCanvas);
    context.rotate((this.angle() * Math.PI) / 180);
    context.scale(drawn, drawn);
    context.drawImage(picture, -picture.width / 2, -picture.height / 2);

    // Zoomed out past the frame, the corners are simply not covered. They stay
    // transparent rather than being painted a colour that would then have to
    // match every background the circle is shown on.
    context.canvas.toBlob(
      (blob) => blob && this.cropped.emit(blob),
      'image/webp',
      0.9,
    );
  }

  /** The picture's extent as the frame sees it, which a quarter turn swaps. */
  private turned(picture: ImageBitmap): [number, number] {
    return this.angle() % 180 === 0
      ? [picture.width, picture.height]
      : [picture.height, picture.width];
  }

  private moveTo(x: number, y: number): void {
    const picture = this.picture();
    const frame = this.frame();

    if (!picture || frame === 0) {
      return;
    }

    const [width, height] = this.turned(picture);
    const scale = this.scale();

    // Nothing to position in a direction the picture no longer overflows, so
    // it is centred there instead.
    this.offset.set({
      x: clamp(x, (width * scale - frame) / 2),
      y: clamp(y, (height * scale - frame) / 2),
    });
  }

  private showWhole(): void {
    if (this.frame() === 0 || !this.picture()) {
      return;
    }

    this.scale.set(this.limits().whole);
    this.offset.set({ x: 0, y: 0 });
  }
}

/** Symmetric about zero, and zero wide when the picture already fits inside. */
const clamp = (value: number, slack: number): number => {
  const bound = Math.max(0, slack);

  return Math.min(bound, Math.max(-bound, value));
};

const squareCanvas = (): CanvasRenderingContext2D | null => {
  const canvas = document.createElement('canvas');
  canvas.width = SIDE;
  canvas.height = SIDE;

  return canvas.getContext('2d');
};
