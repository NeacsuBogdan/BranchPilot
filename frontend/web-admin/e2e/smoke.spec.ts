import { expect, test } from '@playwright/test';

test('signs in and opens the tenant dashboard', async ({ page }) => {
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
  };

  await page.route('**/api/auth/login', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: 'stage-1-access-token',
        accessTokenExpiresAtUtc: '2026-03-25T12:00:00Z',
        refreshToken: 'stage-1-refresh-token',
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

  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(
    page.getByRole('heading', { level: 1, name: 'Northwind Operations Group' }),
  ).toBeVisible();
  await expect(page.getByText('owner@branchpilot.demo', { exact: true }).first()).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Bucharest Central' })).toBeVisible();
});
