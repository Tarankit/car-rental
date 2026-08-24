// Frontend mirrors of the API wire models (spec.md §2/§5). Enums arrive as strings.

export type VehicleCategory = 'Economy' | 'Compact' | 'Suv' | 'Minivan';
export type CancellationPolicy = 'FreeCancellation48h' | 'NonRefundable';
export type InsuranceType = 'Comprehensive' | 'Basic';
export type DocumentType = 'Passport' | 'NationalId';

export const VEHICLE_CATEGORIES: VehicleCategory[] = ['Economy', 'Compact', 'Suv', 'Minivan'];

export interface RentalLocation {
  name: string;
  isInternational: boolean;
}

export interface SearchCriteria {
  pickup: string;
  from: string; // yyyy-MM-dd
  to: string;   // yyyy-MM-dd
  category: VehicleCategory | '';
}

export interface CarOffer {
  providerName: string;
  vehicleId: string;
  category: VehicleCategory;
  perDayRate: number;
  totalPrice: number;
  currency: string;
  cancellationPolicy: CancellationPolicy;
  insurance: InsuranceType;
}

export interface BookRequest {
  providerName: string;
  vehicleId: string;
  pickupLocation: string;
  from: string;
  to: string;
  driverName: string;
  documentType: DocumentType;
  documentNumber: string;
}

export interface Booking {
  reference: string;
  providerName: string;
  category: VehicleCategory;
  pickupLocation: string;
  from: string;
  to: string;
  driverName: string;
  documentType: DocumentType;
  totalPrice: number;
  currency: string;
  cancellationPolicy: CancellationPolicy;
}

export const CANCELLATION_LABELS: Record<CancellationPolicy, string> = {
  FreeCancellation48h: 'Free cancellation up to 48h before pickup',
  NonRefundable: 'Non-refundable',
};

export const CATEGORY_LABELS: Record<VehicleCategory, string> = {
  Economy: 'Economy',
  Compact: 'Compact',
  Suv: 'SUV',
  Minivan: 'Minivan',
};
