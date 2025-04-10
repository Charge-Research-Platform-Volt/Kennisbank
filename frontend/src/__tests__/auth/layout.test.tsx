import { render } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import LoginLayout from '@/app/(auth)/layout';

// Tests for the auth folder layout
describe('Layout', () => {
  it('renders the auth layout', async () => {
    const { getAllByAltText } = render(
      await LoginLayout({ children: <div></div> }),
    );

    // Check for key elements
    expect(getAllByAltText('image').find((img) => {
      return img.getAttribute('src')?.includes('login-image.jpeg');
    })).toBeTruthy();

    expect(getAllByAltText('logo').find((img) => {
      return img.getAttribute('src')?.includes('Charge-logo-NL-purple.png');
    })).toBeTruthy();
  });
});
