import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Booking, BookRequest, CarOffer, RentalLocation, SearchCriteria } from './models';

@Injectable({ providedIn: 'root' })
export class CarApi {
  private readonly http = inject(HttpClient);

  getLocations(): Observable<RentalLocation[]> {
    return this.http.get<RentalLocation[]>('/cars/locations');
  }

  search(criteria: SearchCriteria): Observable<CarOffer[]> {
    let params = new HttpParams()
      .set('pickup', criteria.pickup)
      .set('from', criteria.from)
      .set('to', criteria.to);
    if (criteria.category) {
      params = params.set('category', criteria.category);
    }
    return this.http.get<CarOffer[]>('/cars/search', { params });
  }

  book(request: BookRequest): Observable<Booking> {
    return this.http.post<Booking>('/cars/book', request);
  }
}

/** Extracts a readable message from an RFC 7807 ProblemDetails error response. */
export function problemMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const problem = err.error;
    if (problem && typeof problem === 'object') {
      if (typeof problem.detail === 'string' && problem.detail) {
        return problem.detail;
      }
      if (problem.errors && typeof problem.errors === 'object') {
        const messages = Object.values(problem.errors as Record<string, string[]>).flat();
        if (messages.length) {
          return messages.join(' ');
        }
      }
    }
    if (err.status === 0) {
      return 'Cannot reach the Car Rental API. Is the backend running?';
    }
  }
  return fallback;
}
