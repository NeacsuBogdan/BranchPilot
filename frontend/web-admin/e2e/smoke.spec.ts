import { expect, test } from '@playwright/test';

test('signs in, opens the dashboard, and reaches team management and catalog', async ({ page }) => {
  const session = {
    user: {
      id: '17f1f0c0-7d73-4a8e-92a7-9e9ef9330d89',
      firstName: 'Nora',
      lastName: 'West',
      fullName: 'Nora West',
      email: 'owner@branchpilot.demo',
    },
    tenant: {
      id: '94cfef63-3d1e-4f2e-8d8f-78471d5d52d9',
      name: 'Northwind Operations Group',
      slug: 'northwind-operations-group',
    },
    locations: [
      {
        id: 'ab1e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Bucharest Central',
        code: 'BUC-CENTRAL',
        timeZone: 'Europe/Bucharest',
      },
      {
        id: '90888e1b-b596-40a1-9092-3ca6f2e8c2cf',
        name: 'Cluj North',
        code: 'CLJ-NORTH',
        timeZone: 'Europe/Bucharest',
      },
    ],
    membership: {
      id: '703f4211-691c-4f2b-b2ae-7d78c65c86bd',
      role: 'Owner',
      permissions: [
        'dashboard.view',
        'locations.view',
        'locations.manage',
        'catalog.view',
        'catalog.manage',
        'users.view',
        'users.manage',
      ],
      assignedLocations: [
        {
          id: 'ab1e53ab-bbda-4d80-baf5-e93f80b7924c',
          name: 'Bucharest Central',
          code: 'BUC-CENTRAL',
          timeZone: 'Europe/Bucharest',
        },
        {
          id: '90888e1b-b596-40a1-9092-3ca6f2e8c2cf',
          name: 'Cluj North',
          code: 'CLJ-NORTH',
          timeZone: 'Europe/Bucharest',
        },
      ],
    },
  };

  const teamRoster = {
    items: [
      {
        id: '17f1f0c0-7d73-4a8e-92a7-9e9ef9330d89',
        firstName: 'Nora',
        lastName: 'West',
        fullName: 'Nora West',
        email: 'owner@branchpilot.demo',
        isActive: true,
        isCurrentUser: true,
        membership: {
          id: '703f4211-691c-4f2b-b2ae-7d78c65c86bd',
          role: 'Owner',
          permissions: [
            'dashboard.view',
            'locations.view',
            'locations.manage',
            'catalog.view',
            'catalog.manage',
            'users.view',
            'users.manage',
          ],
          assignedLocations: [
            {
              id: 'ab1e53ab-bbda-4d80-baf5-e93f80b7924c',
              name: 'Bucharest Central',
              code: 'BUC-CENTRAL',
            },
            {
              id: '90888e1b-b596-40a1-9092-3ca6f2e8c2cf',
              name: 'Cluj North',
              code: 'CLJ-NORTH',
            },
          ],
        },
      },
      {
        id: '5685ff96-983f-4e78-a267-3f25f431e1bb',
        firstName: 'Adrian',
        lastName: 'Cole',
        fullName: 'Adrian Cole',
        email: 'admin@branchpilot.demo',
        isActive: true,
        isCurrentUser: false,
        membership: {
          id: '4eb5217d-c47e-4425-b6a1-cb4c8e0179f7',
          role: 'Admin',
          permissions: [
            'dashboard.view',
            'locations.view',
            'locations.manage',
            'catalog.view',
            'catalog.manage',
            'users.view',
            'users.manage',
          ],
          assignedLocations: [
            {
              id: 'ab1e53ab-bbda-4d80-baf5-e93f80b7924c',
              name: 'Bucharest Central',
              code: 'BUC-CENTRAL',
            },
            {
              id: '90888e1b-b596-40a1-9092-3ca6f2e8c2cf',
              name: 'Cluj North',
              code: 'CLJ-NORTH',
            },
          ],
        },
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 2,
  };

  const roleOptions = [
    {
      code: 'Owner',
      name: 'Owner',
      description: 'Full tenant administration including protected access changes.',
      permissions: [
        'dashboard.view',
        'locations.view',
        'locations.manage',
        'catalog.view',
        'catalog.manage',
        'users.view',
        'users.manage',
      ],
    },
    {
      code: 'Admin',
      name: 'Admin',
      description: 'Operational administration across locations and team management.',
      permissions: [
        'dashboard.view',
        'locations.view',
        'locations.manage',
        'catalog.view',
        'catalog.manage',
        'users.view',
        'users.manage',
      ],
    },
  ];

  const catalogOptions = {
    categories: [
      {
        id: 'a12e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Services',
        description: 'Consultation and advisory services.',
      },
      {
        id: 'b12e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Retail',
        description: 'Retail products for branch operations.',
      },
    ],
    taxProfiles: [
      {
        id: 'c12e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Standard VAT 19%',
        rate: 19,
      },
    ],
    locations: [
      {
        id: 'ab1e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Bucharest Central',
        code: 'BUC-CENTRAL',
      },
      {
        id: '90888e1b-b596-40a1-9092-3ca6f2e8c2cf',
        name: 'Cluj North',
        code: 'CLJ-NORTH',
      },
    ],
    itemTypes: [
      {
        code: 'Service',
        name: 'Service',
        description: 'A scheduled service item with an operational duration.',
      },
      {
        code: 'Product',
        name: 'Product',
        description: 'A stocked product sold or consumed by a location.',
      },
    ],
  };

  const categoriesPage = {
    items: catalogOptions.categories,
    page: 1,
    pageSize: 10,
    totalCount: 2,
  };

  const taxProfilesPage = {
    items: catalogOptions.taxProfiles,
    page: 1,
    pageSize: 10,
    totalCount: 1,
  };

  const catalogItemsPage = {
    items: [
      {
        id: 'd12e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Premium Consultation',
        code: 'CONSULT-PREMIUM',
        itemType: 'Service',
        isActive: true,
        durationInMinutes: 45,
        category: catalogOptions.categories[0],
        taxProfile: catalogOptions.taxProfiles[0],
        priceRange: {
          minimumAmount: 220,
          maximumAmount: 260,
          currencyCode: 'EUR',
          locationCount: 2,
        },
        activePromotionCount: 1,
      },
      {
        id: 'e12e53ab-bbda-4d80-baf5-e93f80b7924c',
        name: 'Retail Care Kit',
        code: 'RETAIL-CARE-KIT',
        itemType: 'Product',
        isActive: true,
        durationInMinutes: null,
        category: catalogOptions.categories[1],
        taxProfile: catalogOptions.taxProfiles[0],
        priceRange: {
          minimumAmount: 35,
          maximumAmount: 39,
          currencyCode: 'EUR',
          locationCount: 2,
        },
        activePromotionCount: 0,
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 2,
  };

  await page.route('**/api/auth/login', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: 'stage-2-access-token',
        accessTokenExpiresAtUtc: '2026-03-25T12:00:00Z',
        refreshToken: 'stage-2-refresh-token',
        refreshTokenExpiresAtUtc: '2026-04-08T12:00:00Z',
        session,
      }),
    });
  });

  await page.route('**/api/auth/me', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(session),
    });
  });

  await page.route('**/api/users/roles', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(roleOptions),
    });
  });

  await page.route(/\/api\/users(\?.*)?$/, async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(teamRoster),
    });
  });

  await page.route('**/api/catalog/options', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(catalogOptions),
    });
  });

  await page.route(/\/api\/catalog\/categories(\?.*)?$/, async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(categoriesPage),
    });
  });

  await page.route(/\/api\/catalog\/tax-profiles(\?.*)?$/, async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(taxProfilesPage),
    });
  });

  await page.route(/\/api\/catalog\/items(\?.*)?$/, async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify(catalogItemsPage),
    });
  });

  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(
    page.getByRole('heading', { level: 1, name: 'Northwind Operations Group' }),
  ).toBeVisible();
  await expect(page.getByText('owner@branchpilot.demo', { exact: true }).first()).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Bucharest Central' })).toBeVisible();

  await page.getByRole('link', { name: 'Team management' }).click();

  await expect(
    page.getByRole('heading', { level: 1, name: 'Roles, memberships, and team access' }),
  ).toBeVisible();
  await expect(page.getByRole('button', { name: 'Add user' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Adrian Cole admin@branchpilot.demo' })).toBeVisible();

  await page.getByRole('link', { name: 'Catalog and pricing' }).click();

  await expect(
    page.getByRole('heading', { level: 1, name: 'Catalog, pricing, and promotions' }),
  ).toBeVisible();
  await expect(page.getByRole('button', { name: 'Add item' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Premium Consultation CONSULT-PREMIUM' })).toBeVisible();
});
