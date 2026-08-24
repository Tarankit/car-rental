import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Booking, CANCELLATION_LABELS, CATEGORY_LABELS } from '../models';

@Component({
  selector: 'app-confirmation',
  imports: [CurrencyPipe],
  templateUrl: './confirmation.html',
  styleUrl: './confirmation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Confirmation {
  readonly booking = input.required<Booking>();
  readonly newSearch = output<void>();

  protected readonly cancellationLabels = CANCELLATION_LABELS;
  protected readonly categoryLabels = CATEGORY_LABELS;
}
