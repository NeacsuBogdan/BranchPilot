import { expect, test } from '@playwright/test';

test('shows the bootstrap overview', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', {
      name: 'Professional bootstrap for a portfolio-grade operations platform.',
    }),
  ).toBeVisible();
  await expect(page.getByText('Backend foundation')).toBeVisible();
  await expect(page.getByText('Current outcome')).toBeVisible();
});
