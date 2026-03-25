import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';

import { PermissionCodes } from '../../core/auth/permission-codes';
import { AuthService } from '../../core/auth/auth.service';
import { CatalogApiService } from '../../core/catalog/catalog-api.service';
import {
  type CatalogItemDetails,
  type CatalogItemDialogResult,
  type CatalogOptions,
  type CatalogItemSummary,
  type Category,
  type PagedResponse,
  type TaxProfile,
} from '../../core/catalog/catalog.models';
import { CatalogItemDialogComponent } from './catalog-item-dialog.component';

@Component({
  selector: 'app-catalog-page',
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './catalog-page.component.html',
  styleUrl: './catalog-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly catalogApiService = inject(CatalogApiService);
  private readonly dialog = inject(MatDialog);
  private readonly formBuilder = inject(FormBuilder);

  private readonly pageState = signal(1);
  private readonly pageSizeState = signal(10);

  protected readonly canManageCatalog = computed(() =>
    this.authService.hasPermission(PermissionCodes.catalogManage),
  );
  protected readonly options = signal<CatalogOptions | null>(null);
  protected readonly categoriesPage = signal<PagedResponse<Category> | null>(null);
  protected readonly taxProfilesPage = signal<PagedResponse<TaxProfile> | null>(null);
  protected readonly itemsPage = signal<PagedResponse<CatalogItemSummary> | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isReferenceSaving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly referenceErrorMessage = signal<string | null>(null);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    search: [''],
    itemType: [''],
    categoryId: [''],
    status: ['all'],
  });

  protected readonly categoryForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', [Validators.maxLength(400)]],
  });

  protected readonly taxProfileForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    rate: [19, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  protected readonly totalItems = computed(() => this.itemsPage()?.totalCount ?? 0);
  protected readonly totalCategories = computed(() => this.categoriesPage()?.totalCount ?? 0);
  protected readonly totalTaxProfiles = computed(() => this.taxProfilesPage()?.totalCount ?? 0);
  protected readonly canOpenItemDialog = computed(() => {
    const options = this.options();

    return !!options && this.canManageCatalog() && options.taxProfiles.length > 0 && options.locations.length > 0;
  });

  ngOnInit(): void {
    void this.loadInitialState();
  }

  protected async applyFilters(): Promise<void> {
    this.pageState.set(1);
    await this.loadItems();
  }

  protected async changePage(event: PageEvent): Promise<void> {
    this.pageState.set(event.pageIndex + 1);
    this.pageSizeState.set(event.pageSize);
    await this.loadItems();
  }

  protected async createCategory(): Promise<void> {
    if (!this.canManageCatalog()) {
      return;
    }

    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.isReferenceSaving.set(true);
    this.referenceErrorMessage.set(null);

    try {
      await this.catalogApiService.createCategory(this.categoryForm.getRawValue());
      this.categoryForm.reset({ name: '', description: '' });
      await this.reloadReferenceData();
    } catch (error) {
      this.referenceErrorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isReferenceSaving.set(false);
    }
  }

  protected async createTaxProfile(): Promise<void> {
    if (!this.canManageCatalog()) {
      return;
    }

    if (this.taxProfileForm.invalid) {
      this.taxProfileForm.markAllAsTouched();
      return;
    }

    this.isReferenceSaving.set(true);
    this.referenceErrorMessage.set(null);

    try {
      await this.catalogApiService.createTaxProfile({
        name: this.taxProfileForm.controls.name.value,
        rate: Number(this.taxProfileForm.controls.rate.value),
      });
      this.taxProfileForm.reset({ name: '', rate: 19 });
      await this.reloadReferenceData();
    } catch (error) {
      this.referenceErrorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isReferenceSaving.set(false);
    }
  }

  protected async openCreateDialog(): Promise<void> {
    const options = this.options();

    if (!this.canOpenItemDialog() || !options) {
      return;
    }

    const dialogResult = await this.openDialog({ mode: 'create', options });

    if (!dialogResult || dialogResult.action !== 'create') {
      return;
    }

    try {
      this.errorMessage.set(null);
      await this.catalogApiService.createItem(dialogResult.payload);
      await this.loadItems();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  protected async openEditDialog(item: CatalogItemSummary): Promise<void> {
    const options = this.options();

    if (!this.canOpenItemDialog() || !options) {
      return;
    }

    try {
      this.errorMessage.set(null);
      const details = await this.catalogApiService.getItem(item.id);
      const dialogResult = await this.openDialog({ mode: 'edit', options, item: details });

      if (!dialogResult || dialogResult.action !== 'update') {
        return;
      }

      await this.catalogApiService.updateItem(dialogResult.itemId, dialogResult.payload);
      await this.loadItems();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  private async loadInitialState(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.referenceErrorMessage.set(null);

    try {
      await Promise.all([this.reloadReferenceData(), this.loadItems()]);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async reloadReferenceData(): Promise<void> {
    const [options, categoriesPage, taxProfilesPage] = await Promise.all([
      this.catalogApiService.getOptions(),
      this.catalogApiService.listCategories({ page: 1, pageSize: 10 }),
      this.catalogApiService.listTaxProfiles({ page: 1, pageSize: 10 }),
    ]);

    this.options.set(options);
    this.categoriesPage.set(categoriesPage);
    this.taxProfilesPage.set(taxProfilesPage);
  }

  private async loadItems(): Promise<void> {
    this.isLoading.set(true);

    try {
      const filters = this.filtersForm.getRawValue();
      const itemsPage = await this.catalogApiService.listItems({
        page: this.pageState(),
        pageSize: this.pageSizeState(),
        search: filters.search.trim() || undefined,
        itemType: filters.itemType || undefined,
        categoryId: filters.categoryId || undefined,
        isActive: parseStatus(filters.status),
      });

      this.itemsPage.set(itemsPage);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async openDialog(data: {
    mode: 'create' | 'edit';
    options: CatalogOptions;
    item?: CatalogItemDetails;
  }): Promise<CatalogItemDialogResult | undefined> {
    const dialogRef = this.dialog.open(CatalogItemDialogComponent, {
      width: '920px',
      maxWidth: 'calc(100vw - 2rem)',
      data,
    });

    return firstValueFrom(dialogRef.afterClosed());
  }
}

function parseStatus(value: string): boolean | undefined {
  return value === 'active' ? true : value === 'inactive' ? false : undefined;
}

function getApiErrorMessage(error: unknown): string {
  if (
    error instanceof HttpErrorResponse &&
    typeof error.error?.detail === 'string' &&
    error.error.detail.length > 0
  ) {
    return error.error.detail;
  }

  return 'The catalog workspace could not be updated right now.';
}
