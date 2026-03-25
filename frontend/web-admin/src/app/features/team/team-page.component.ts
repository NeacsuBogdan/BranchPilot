import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { type LocationSummary } from '../../core/auth/auth.models';
import { PermissionCodes } from '../../core/auth/permission-codes';
import { AuthService } from '../../core/auth/auth.service';
import { TeamApiService } from '../../core/team/team-api.service';
import {
  type PagedResponse,
  type RoleOption,
  type TeamMember,
  type TeamMemberDialogResult,
} from '../../core/team/team.models';
import { TeamMemberDialogComponent } from './team-member-dialog.component';

@Component({
  selector: 'app-team-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    ReactiveFormsModule,
  ],
  templateUrl: './team-page.component.html',
  styleUrl: './team-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly teamApiService = inject(TeamApiService);

  private readonly pageState = signal(1);
  private readonly pageSizeState = signal(10);
  private readonly searchState = signal('');

  protected readonly session = this.authService.session;
  protected readonly canManageUsers = computed(() =>
    this.authService.hasPermission(PermissionCodes.usersManage),
  );
  protected readonly availableLocations = computed<LocationSummary[]>(
    () => this.session()?.locations ?? [],
  );
  protected readonly roleOptions = signal<RoleOption[]>([]);
  protected readonly pageResponse = signal<PagedResponse<TeamMember> | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly searchForm = this.formBuilder.nonNullable.group({
    search: [''],
  });

  ngOnInit(): void {
    void this.loadInitialState();
  }

  protected readonly totalUsers = computed(() => this.pageResponse()?.totalCount ?? 0);

  protected async applySearch(): Promise<void> {
    this.pageState.set(1);
    this.searchState.set(this.searchForm.controls.search.value.trim());
    await this.loadUsers();
  }

  protected async changePage(event: PageEvent): Promise<void> {
    this.pageState.set(event.pageIndex + 1);
    this.pageSizeState.set(event.pageSize);
    await this.loadUsers();
  }

  protected async openCreateDialog(): Promise<void> {
    if (!this.canManageUsers()) {
      return;
    }

    const dialogResult = await this.openDialog({ mode: 'create' });

    if (!dialogResult || dialogResult.action !== 'create') {
      return;
    }

    try {
      this.errorMessage.set(null);
      await this.teamApiService.createUser(dialogResult.payload);
      await this.loadUsers();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  protected async openEditDialog(member: TeamMember): Promise<void> {
    if (!this.canManageUsers()) {
      return;
    }

    const dialogResult = await this.openDialog({ mode: 'edit', member });

    if (!dialogResult || dialogResult.action !== 'update') {
      return;
    }

    try {
      this.errorMessage.set(null);
      const updatedMember = await this.teamApiService.updateMembership(
        dialogResult.userId,
        dialogResult.payload,
      );

      if (updatedMember.isCurrentUser) {
        if (!updatedMember.isActive) {
          await this.authService.logout();
          await this.router.navigateByUrl('/auth/login');
          return;
        }

        await this.authService.reloadSession();
      }

      await this.loadUsers();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  private async loadInitialState(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const [roleOptions, pageResponse] = await Promise.all([
        this.teamApiService.getRoleOptions(),
        this.teamApiService.list({
          page: this.pageState(),
          pageSize: this.pageSizeState(),
          search: this.searchState() || undefined,
        }),
      ]);

      this.roleOptions.set(roleOptions);
      this.pageResponse.set(pageResponse);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadUsers(): Promise<void> {
    this.isLoading.set(true);

    try {
      const pageResponse = await this.teamApiService.list({
        page: this.pageState(),
        pageSize: this.pageSizeState(),
        search: this.searchState() || undefined,
      });

      this.pageResponse.set(pageResponse);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async openDialog(data: {
    mode: 'create' | 'edit';
    member?: TeamMember;
  }): Promise<TeamMemberDialogResult | undefined> {
    const dialogRef = this.dialog.open(TeamMemberDialogComponent, {
      width: '680px',
      maxWidth: 'calc(100vw - 2rem)',
      data: {
        mode: data.mode,
        member: data.member,
        roleOptions: this.roleOptions(),
        locations: this.availableLocations(),
      },
    });

    return firstValueFrom(dialogRef.afterClosed());
  }
}

function getApiErrorMessage(error: unknown): string {
  if (
    error instanceof HttpErrorResponse &&
    typeof error.error?.detail === 'string' &&
    error.error.detail.length > 0
  ) {
    return error.error.detail;
  }

  return 'The team workspace could not be updated right now.';
}
