import { ChangeDetectionStrategy, Component, effect, inject, input, output } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { CANCELLATION_LABELS, CarOffer, CATEGORY_LABELS, DocumentType, RentalLocation } from '../models';

export interface DriverDetails {
  driverName: string;
  documentType: DocumentType;
  documentNumber: string;
}

@Component({
  selector: 'app-booking-form',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './booking-form.html',
  styleUrl: './booking-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingForm {
  readonly offer = input.required<CarOffer>();
  readonly pickup = input.required<RentalLocation>();
  readonly submitting = input(false);
  /** Server-side rejection (e.g. a 422) shown verbatim under the form. */
  readonly serverError = input<string | null>(null);

  readonly confirmBooking = output<DriverDetails>();
  readonly cancelled = output<void>();

  protected readonly cancellationLabels = CANCELLATION_LABELS;
  protected readonly categoryLabels = CATEGORY_LABELS;

  private readonly fb = inject(FormBuilder);
  protected readonly form = this.fb.nonNullable.group({
    driverName: ['', Validators.required],
    documentType: ['Passport' as DocumentType, Validators.required],
    documentNumber: ['', Validators.required],
  });

  constructor() {
    // Client-side mirror of the server's document rule (spec.md §3): an international
    // pickup only accepts a passport. Re-attach whenever the pickup input changes.
    effect(() => {
      const pickup = this.pickup();
      this.form.controls.documentType.setValidators([
        Validators.required,
        (control: AbstractControl): ValidationErrors | null =>
          pickup.isInternational && control.value !== 'Passport' ? { passportRequired: true } : null,
      ]);
      this.form.controls.documentType.updateValueAndValidity();
    });
  }

  protected get showPassportRequired(): boolean {
    return this.form.controls.documentType.hasError('passportRequired');
  }

  protected showRequired(name: 'driverName' | 'documentNumber'): boolean {
    const control = this.form.get(name)!;
    return control.touched && control.hasError('required');
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.confirmBooking.emit(this.form.getRawValue());
  }
}
