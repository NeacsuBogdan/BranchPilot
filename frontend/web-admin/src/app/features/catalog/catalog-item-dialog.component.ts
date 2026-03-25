import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import {
  type CatalogItemDialogData,
  type CatalogItemDialogResult,
  type LocationPrice,
  type Promotion,
} from '../../core/catalog/catalog.models';

@Component({
  selector: 'app-catalog-item-dialog',
  imports: [
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './catalog-item-dialog.component.html',
  styleUrl: './catalog-item-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogItemDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<CatalogItemDialogComponent, CatalogItemDialogResult | undefined>>(MatDialogRef);

  protected readonly data = inject<CatalogItemDialogData>(MAT_DIALOG_DATA);
  protected readonly isEditMode = this.data.mode === 'edit';
  protected readonly selectedItemType = computed(
    () => this.data.options.itemTypes.find((option) => option.code === this.form.controls.itemType.value) ?? null,
  );

  protected readonly form = this.formBuilder.group({
    name: [this.data.item?.name ?? '', [Validators.required, Validators.maxLength(120)]],
    code: [
      this.data.item?.code ?? '',
      [Validators.required, Validators.maxLength(32), Validators.pattern(/^[A-Za-z0-9-]+$/)],
    ],
    itemType: [this.data.item?.itemType ?? 'Service', [Validators.required]],
    categoryId: [this.data.item?.category?.id ?? ''],
    taxProfileId: [this.data.item?.taxProfile.id ?? '', [Validators.required]],
    description: [this.data.item?.description ?? '', [Validators.maxLength(1000)]],
    durationInMinutes: [
      this.data.item?.durationInMinutes ?? null,
      [Validators.min(5), Validators.max(480)],
    ],
    isActive: [this.data.item?.isActive ?? true, [Validators.required]],
    locationPrices: this.formBuilder.array([]),
    promotions: this.formBuilder.array([]),
  });

  constructor() {
    const existingPrices = this.data.item?.locationPrices ?? [];
    const existingPromotions = this.data.item?.promotions ?? [];

    if (existingPrices.length > 0) {
      for (const locationPrice of existingPrices) {
        this.priceRows.push(this.createPriceGroup(locationPrice.locationId, locationPrice.priceAmount, locationPrice.currencyCode));
      }
    } else {
      this.addLocationPrice();
    }

    if (existingPromotions.length > 0) {
      for (const promotion of existingPromotions) {
        this.promotionRows.push(this.createPromotionGroup(promotion));
      }
    }

    this.applyItemTypeMode(this.form.controls.itemType.value ?? 'Service');
    this.form.controls.itemType.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((itemType) => this.applyItemTypeMode(itemType ?? 'Service'));
  }

  protected get priceRows(): FormArray {
    return this.form.controls.locationPrices as FormArray;
  }

  protected get promotionRows(): FormArray {
    return this.form.controls.promotions as FormArray;
  }

  protected addLocationPrice(): void {
    const fallbackLocationId = this.data.options.locations[0]?.id ?? '';
    this.priceRows.push(this.createPriceGroup(fallbackLocationId, null, 'EUR'));
  }

  protected removeLocationPrice(index: number): void {
    this.priceRows.removeAt(index);
  }

  protected addPromotion(): void {
    const fallbackLocationId = this.data.options.locations[0]?.id ?? '';
    this.promotionRows.push(
      this.createPromotionGroup({
        id: '',
        name: '',
        locationId: fallbackLocationId,
        locationName: '',
        locationCode: '',
        discountPercentage: 10,
        startsAtUtc: new Date().toISOString(),
        endsAtUtc: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        isActive: false,
      }),
    );
  }

  protected removePromotion(index: number): void {
    this.promotionRows.removeAt(index);
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected submit(): void {
    if (this.form.invalid || this.priceRows.length === 0) {
      this.form.markAllAsTouched();
      this.priceRows.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const locationPrices = (value.locationPrices ?? []) as Pick<
      LocationPrice,
      'locationId' | 'priceAmount' | 'currencyCode'
    >[];
    const promotions = (value.promotions ?? []) as Pick<
      Promotion,
      'name' | 'locationId' | 'discountPercentage' | 'startsAtUtc' | 'endsAtUtc'
    >[];
    const payload = {
      name: (value.name ?? '').trim(),
      code: (value.code ?? '').trim(),
      itemType: value.itemType ?? 'Service',
      categoryId: value.categoryId ? value.categoryId : null,
      taxProfileId: value.taxProfileId ?? '',
      description: (value.description ?? '').trim(),
      durationInMinutes:
        value.itemType === 'Service' && typeof value.durationInMinutes === 'number'
          ? value.durationInMinutes
          : null,
      isActive: value.isActive ?? true,
      locationPrices: locationPrices.map((locationPrice) => ({
        locationId: locationPrice.locationId ?? '',
        priceAmount: Number(locationPrice.priceAmount),
        currencyCode: (locationPrice.currencyCode ?? 'EUR').trim().toUpperCase(),
      })),
      promotions: promotions.map((promotion) => ({
        name: (promotion.name ?? '').trim(),
        locationId: promotion.locationId ?? '',
        discountPercentage: Number(promotion.discountPercentage),
        startsAtUtc: toIsoDateTime(promotion.startsAtUtc ?? ''),
        endsAtUtc: toIsoDateTime(promotion.endsAtUtc ?? ''),
      })),
    };

    if (this.isEditMode && this.data.item) {
      this.dialogRef.close({
        action: 'update',
        itemId: this.data.item.id,
        payload,
      });
      return;
    }

    this.dialogRef.close({
      action: 'create',
      payload,
    });
  }

  private createPriceGroup(locationId: string, priceAmount: number | null, currencyCode: string) {
    return this.formBuilder.group({
      locationId: [locationId, [Validators.required]],
      priceAmount: [priceAmount, [Validators.required, Validators.min(0.01)]],
      currencyCode: [
        currencyCode,
        [Validators.required, Validators.minLength(3), Validators.maxLength(3), Validators.pattern(/^[A-Za-z]{3}$/)],
      ],
    });
  }

  private createPromotionGroup(promotion: Promotion) {
    return this.formBuilder.group({
      name: [promotion.name, [Validators.required, Validators.maxLength(120)]],
      locationId: [promotion.locationId, [Validators.required]],
      discountPercentage: [promotion.discountPercentage, [Validators.required, Validators.min(0.01), Validators.max(100)]],
      startsAtUtc: [toLocalDateTimeValue(promotion.startsAtUtc), [Validators.required]],
      endsAtUtc: [toLocalDateTimeValue(promotion.endsAtUtc), [Validators.required]],
    });
  }

  private applyItemTypeMode(itemType: string): void {
    if (itemType === 'Service') {
      this.form.controls.durationInMinutes.enable({ emitEvent: false });
      this.form.controls.durationInMinutes.addValidators([Validators.required, Validators.min(5), Validators.max(480)]);
      this.form.controls.durationInMinutes.updateValueAndValidity({ emitEvent: false });
      return;
    }

    this.form.controls.durationInMinutes.setValue(null, { emitEvent: false });
    this.form.controls.durationInMinutes.clearValidators();
    this.form.controls.durationInMinutes.disable({ emitEvent: false });
    this.form.controls.durationInMinutes.updateValueAndValidity({ emitEvent: false });
  }
}

function toLocalDateTimeValue(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function toIsoDateTime(value: string): string {
  return new Date(value).toISOString();
}
