import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { CATEGORY_LABELS, RentalLocation, SearchCriteria, VEHICLE_CATEGORIES } from '../models';

/** Cross-field rule mirroring the server: return date must be after pickup date. */
function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
  const from = group.get('from')?.value;
  const to = group.get('to')?.value;
  return from && to && to <= from ? { dateRange: true } : null;
}

@Component({
  selector: 'app-search-form',
  imports: [ReactiveFormsModule],
  templateUrl: './search-form.html',
  styleUrl: './search-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchForm {
  readonly locations = input.required<RentalLocation[]>();
  readonly searching = input(false);
  readonly search = output<SearchCriteria>();

  protected readonly categories = VEHICLE_CATEGORIES;
  protected readonly categoryLabels = CATEGORY_LABELS;

  private readonly fb = inject(FormBuilder);
  protected readonly form = this.fb.nonNullable.group(
    {
      pickup: ['', Validators.required],
      from: ['', Validators.required],
      to: ['', Validators.required],
      category: '' as SearchCriteria['category'],
    },
    { validators: dateRangeValidator },
  );

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.search.emit(this.form.getRawValue());
  }

  protected showRequired(name: 'pickup' | 'from' | 'to'): boolean {
    const control = this.form.get(name)!;
    return control.touched && control.hasError('required');
  }

  protected get showDateRangeError(): boolean {
    const to = this.form.get('to')!;
    return to.touched && !to.hasError('required') && this.form.hasError('dateRange');
  }
}
