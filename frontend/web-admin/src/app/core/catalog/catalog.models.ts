export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Category {
  id: string;
  name: string;
  description: string;
}

export interface TaxProfile {
  id: string;
  name: string;
  rate: number;
}

export interface LocationOption {
  id: string;
  name: string;
  code: string;
}

export interface CatalogItemTypeOption {
  code: string;
  name: string;
  description: string;
}

export interface CatalogOptions {
  categories: Category[];
  taxProfiles: TaxProfile[];
  locations: LocationOption[];
  itemTypes: CatalogItemTypeOption[];
}

export interface PriceRange {
  minimumAmount: number;
  maximumAmount: number;
  currencyCode: string;
  locationCount: number;
}

export interface CatalogItemSummary {
  id: string;
  name: string;
  code: string;
  itemType: string;
  isActive: boolean;
  durationInMinutes: number | null;
  category: Category | null;
  taxProfile: TaxProfile;
  priceRange: PriceRange;
  activePromotionCount: number;
}

export interface LocationPrice {
  locationId: string;
  locationName: string;
  locationCode: string;
  priceAmount: number;
  currencyCode: string;
}

export interface Promotion {
  id: string;
  name: string;
  locationId: string;
  locationName: string;
  locationCode: string;
  discountPercentage: number;
  startsAtUtc: string;
  endsAtUtc: string;
  isActive: boolean;
}

export interface CatalogItemDetails {
  id: string;
  name: string;
  code: string;
  itemType: string;
  isActive: boolean;
  description: string;
  durationInMinutes: number | null;
  category: Category | null;
  taxProfile: TaxProfile;
  locationPrices: LocationPrice[];
  promotions: Promotion[];
}

export interface GetCatalogItemsQuery {
  page: number;
  pageSize: number;
  search?: string;
  itemType?: string;
  categoryId?: string;
  isActive?: boolean;
}

export interface GetCatalogReferencesQuery {
  page: number;
  pageSize: number;
  search?: string;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
}

export interface CreateTaxProfileRequest {
  name: string;
  rate: number;
}

export interface UpsertCatalogItemRequest {
  name: string;
  code: string;
  itemType: string;
  categoryId: string | null;
  taxProfileId: string;
  description?: string;
  durationInMinutes: number | null;
  isActive: boolean;
  locationPrices: LocationPriceInput[];
  promotions: PromotionInput[];
}

export interface LocationPriceInput {
  locationId: string;
  priceAmount: number;
  currencyCode: string;
}

export interface PromotionInput {
  name: string;
  locationId: string;
  discountPercentage: number;
  startsAtUtc: string;
  endsAtUtc: string;
}

export interface CatalogItemDialogData {
  mode: 'create' | 'edit';
  options: CatalogOptions;
  item?: CatalogItemDetails;
}

export type CatalogItemDialogResult =
  | {
      action: 'create';
      payload: UpsertCatalogItemRequest;
    }
  | {
      action: 'update';
      itemId: string;
      payload: UpsertCatalogItemRequest;
    };
