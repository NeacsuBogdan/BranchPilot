import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { PermissionCodes } from '../../core/auth/permission-codes';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-admin-shell',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminShellComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  protected readonly session = this.authService.session;
  protected readonly canViewUsers = computed(() =>
    this.authService.hasPermission(PermissionCodes.usersView),
  );
  protected readonly canViewCatalog = computed(() =>
    this.authService.hasPermission(PermissionCodes.catalogView),
  );
  protected readonly canViewCustomers = computed(() =>
    this.authService.hasPermission(PermissionCodes.customersView),
  );
  protected readonly canViewBookings = computed(() =>
    this.authService.hasPermission(PermissionCodes.bookingsView),
  );
  protected readonly workspaceSubtitle = computed(() => {
    const session = this.session();

    if (!session) {
      return 'Preparing workspace';
    }

    return `${session.membership.role} - ${session.locations.length} locations`;
  });

  protected async signOut(): Promise<void> {
    await this.authService.logout();
    await this.router.navigateByUrl('/auth/login');
  }
}
