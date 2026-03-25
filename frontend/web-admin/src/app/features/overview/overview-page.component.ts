import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';

interface Pillar {
  title: string;
  description: string;
  icon: string;
}

interface StackGroup {
  label: string;
  items: string[];
}

@Component({
  selector: 'app-overview-page',
  imports: [MatCardModule, MatChipsModule, MatDividerModule, MatIconModule],
  templateUrl: './overview-page.component.html',
  styleUrl: './overview-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewPageComponent {
  protected readonly highlights = signal<Pillar[]>([
    {
      title: 'Backend foundation',
      description:
        'Clean backend layering is scaffolded under Domain, Application, Infrastructure, and Api with Swagger, health checks, and Serilog.',
      icon: 'dns',
    },
    {
      title: 'Admin workspace',
      description:
        'Angular 19 standalone components, signals, Material, linting, formatting, and Playwright are ready for the staged UI buildout.',
      icon: 'dashboard',
    },
    {
      title: 'Local dependencies',
      description:
        'Docker Compose provisions PostgreSQL and Redis so future tenancy, auth, and jobs can be developed against realistic services.',
      icon: 'storage',
    },
    {
      title: 'Delivery pipeline',
      description:
        'A base GitHub Actions workflow will build and test both backend and frontend on pushes and pull requests to main or dev.',
      icon: 'verified',
    },
  ]);

  protected readonly stageMarkers = signal([
    'Angular 19',
    '.NET 9',
    'PostgreSQL',
    'Redis',
    'Swagger',
    'Health checks',
  ]);

  protected readonly stack = signal<StackGroup[]>([
    {
      label: 'Frontend',
      items: ['Angular 19', 'Standalone components', 'Signals', 'Angular Material'],
    },
    {
      label: 'Backend',
      items: ['ASP.NET Core 9', 'Project layering', 'Serilog', 'Health checks'],
    },
    {
      label: 'Tooling',
      items: ['Docker Compose', 'ESLint + Prettier', 'Playwright', 'GitHub Actions'],
    },
  ]);

  protected readonly checklist = signal([
    'Work is isolated on the dev branch.',
    'The solution and Angular workspace build from a clean bootstrap.',
    'Live and readiness endpoints are exposed for the API.',
    'The repo is prepared for Stage 1 auth and tenancy work.',
  ]);
}
