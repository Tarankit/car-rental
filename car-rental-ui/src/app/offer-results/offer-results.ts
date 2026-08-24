import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { CANCELLATION_LABELS, CarOffer, CATEGORY_LABELS } from '../models';

@Component({
  selector: 'app-offer-results',
  imports: [CurrencyPipe],
  templateUrl: './offer-results.html',
  styleUrl: './offer-results.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OfferResults {
  readonly offers = input.required<CarOffer[]>();
  readonly book = output<CarOffer>();

  protected readonly cancellationLabels = CANCELLATION_LABELS;
  protected readonly categoryLabels = CATEGORY_LABELS;

  /** The API returns ascending by total; the toggle re-sorts client-side. */
  protected readonly sortDirection = signal<'asc' | 'desc'>('asc');

  protected readonly sortedOffers = computed(() => {
    const direction = this.sortDirection() === 'asc' ? 1 : -1;
    return [...this.offers()].sort((a, b) => (a.totalPrice - b.totalPrice) * direction);
  });

  protected toggleSort(): void {
    this.sortDirection.update((d) => (d === 'asc' ? 'desc' : 'asc'));
  }
}
