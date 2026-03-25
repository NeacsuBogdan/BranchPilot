import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'overview',
  },
  {
    path: 'overview',
    loadComponent: () =>
      import('./features/overview/overview-page.component').then((m) => m.OverviewPageComponent),
  },
  {
    path: '**',
    redirectTo: 'overview',
  },
];
