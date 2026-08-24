import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CarApi, problemMessage } from './car-api';
import { Booking, CarOffer, RentalLocation, SearchCriteria } from './models';
import { SearchForm } from './search-form/search-form';
import { OfferResults } from './offer-results/offer-results';
import { BookingForm, DriverDetails } from './booking-form/booking-form';
import { Confirmation } from './confirmation/confirmation';

type View = 'search' | 'booking' | 'confirmed';

@Component({
  selector: 'app-root',
  imports: [SearchForm, OfferResults, BookingForm, Confirmation],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly api = inject(CarApi);

  protected readonly view = signal<View>('search');

  protected readonly locations = signal<RentalLocation[]>([]);
  protected readonly locationsError = signal<string | null>(null);

  protected readonly searching = signal(false);
  protected readonly searched = signal(false);
  protected readonly searchError = signal<string | null>(null);
  protected readonly offers = signal<CarOffer[]>([]);
  protected readonly criteria = signal<SearchCriteria | null>(null);

  protected readonly selectedOffer = signal<CarOffer | null>(null);
  protected readonly bookingInProgress = signal(false);
  protected readonly bookingError = signal<string | null>(null);
  protected readonly booking = signal<Booking | null>(null);

  /** The full location record for the current pickup — drives the client-side document rule. */
  protected readonly pickupLocation = computed(() =>
    this.locations().find((l) => l.name === this.criteria()?.pickup) ?? null,
  );

  constructor() {
    this.api.getLocations().subscribe({
      next: (locations) => this.locations.set(locations),
      error: (err) => this.locationsError.set(problemMessage(err, 'Failed to load pickup locations.')),
    });
  }

  protected onSearch(criteria: SearchCriteria): void {
    this.criteria.set(criteria);
    this.searching.set(true);
    this.searchError.set(null);
    this.api.search(criteria).subscribe({
      next: (offers) => {
        this.offers.set(offers);
        this.searched.set(true);
        this.searching.set(false);
      },
      error: (err) => {
        this.offers.set([]);
        this.searched.set(false);
        this.searchError.set(problemMessage(err, 'Search failed. Please try again.'));
        this.searching.set(false);
      },
    });
  }

  protected onSelectOffer(offer: CarOffer): void {
    this.selectedOffer.set(offer);
    this.bookingError.set(null);
    this.view.set('booking');
  }

  protected onConfirmBooking(driver: DriverDetails): void {
    const offer = this.selectedOffer();
    const criteria = this.criteria();
    if (!offer || !criteria) {
      return;
    }
    this.bookingInProgress.set(true);
    this.bookingError.set(null);
    this.api
      .book({
        providerName: offer.providerName,
        vehicleId: offer.vehicleId,
        pickupLocation: criteria.pickup,
        from: criteria.from,
        to: criteria.to,
        ...driver,
      })
      .subscribe({
        next: (booking) => {
          this.booking.set(booking);
          this.bookingInProgress.set(false);
          this.view.set('confirmed');
        },
        error: (err) => {
          this.bookingError.set(problemMessage(err, 'Booking failed. Please try again.'));
          this.bookingInProgress.set(false);
        },
      });
  }

  protected backToResults(): void {
    this.selectedOffer.set(null);
    this.bookingError.set(null);
    this.view.set('search');
  }

  protected startOver(): void {
    this.view.set('search');
    this.searched.set(false);
    this.offers.set([]);
    this.criteria.set(null);
    this.selectedOffer.set(null);
    this.booking.set(null);
    this.searchError.set(null);
    this.bookingError.set(null);
  }
}
