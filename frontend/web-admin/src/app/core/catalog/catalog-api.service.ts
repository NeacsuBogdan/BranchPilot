import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  type CatalogItemDetails,
  type CatalogItemSummary,
  type CatalogOptions,
  type CreateCategoryRequest,
  type CreateTaxProfileRequest,
  type Category,
  type GetCatalogItemsQuery,
  type GetCatalogReferencesQuery,
  type PagedResponse,
  type TaxProfile,
  type UpsertCatalogItemRequest,
} from './catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = inject(HttpClient);

  getOptions(): Promise<CatalogOptions> {
    return firstValueFrom(this.http.get<CatalogOptions>('/api/catalog/options'));
  }

  listCategories(query: GetCatalogReferencesQuery): Promise<PagedResponse<Category>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    return firstValueFrom(this.http.get<PagedResponse<Category>>('/api/catalog/categories', { params }));
  }

  createCategory(request: CreateCategoryRequest): Promise<Category> {
    return firstValueFrom(this.http.post<Category>('/api/catalog/categories', request));
  }

  listTaxProfiles(query: GetCatalogReferencesQuery): Promise<PagedResponse<TaxProfile>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    return firstValueFrom(
      this.http.get<PagedResponse<TaxProfile>>('/api/catalog/tax-profiles', { params }),
    );
  }

  createTaxProfile(request: CreateTaxProfileRequest): Promise<TaxProfile> {
    return firstValueFrom(this.http.post<TaxProfile>('/api/catalog/tax-profiles', request));
  }

  listItems(query: GetCatalogItemsQuery): Promise<PagedResponse<CatalogItemSummary>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.itemType) {
      params = params.set('itemType', query.itemType);
    }

    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }

    if (typeof query.isActive === 'boolean') {
      params = params.set('isActive', query.isActive);
    }

    return firstValueFrom(this.http.get<PagedResponse<CatalogItemSummary>>('/api/catalog/items', { params }));
  }

  getItem(itemId: string): Promise<CatalogItemDetails> {
    return firstValueFrom(this.http.get<CatalogItemDetails>(`/api/catalog/items/${itemId}`));
  }

  createItem(request: UpsertCatalogItemRequest): Promise<CatalogItemDetails> {
    return firstValueFrom(this.http.post<CatalogItemDetails>('/api/catalog/items', request));
  }

  updateItem(itemId: string, request: UpsertCatalogItemRequest): Promise<CatalogItemDetails> {
    return firstValueFrom(this.http.put<CatalogItemDetails>(`/api/catalog/items/${itemId}`, request));
  }
}
