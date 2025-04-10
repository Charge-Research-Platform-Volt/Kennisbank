import { render, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import '@/__tests__/custom-matchers'

import LoginPage from '@/app/(auth)/login/page';

import * as authActions from '@/actions/authActions';

vi.mock('next/navigation', () => ({
  useRouter: vi.fn().mockReturnValue({
    push: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
    refresh: vi.fn(),
    replace: vi.fn() 
  }),
}));

describe('Log in page', () => {
  it('renders the page with fields and buttons', async () => {
    const { getByText, getByPlaceholderText, getByRole } = render(
        <LoginPage />
    );
    
    // Check for key elements
    expect(getByText('EMAIL')).toBeTruthy();
    expect(getByPlaceholderText('Email')).toBeTruthy();
    expect(getByText('PASSWORD')).toBeTruthy();
    expect(getByPlaceholderText('Password')).toBeTruthy();
    expect(getByRole('button', { name: 'Sign in'})).toBeTruthy();
  });
});

// Tests for the Login Page
describe('Log in action', () => {
  it('should call the form action with the right parameters', async () => {
    const { getByPlaceholderText, getByRole } = render(
        <LoginPage />
    );

    const loginSpy = vi.spyOn(authActions, 'Login')
    
    // Get input elements
    const email = getByPlaceholderText('Email');
    const password = getByPlaceholderText('Password');
    const sign_in = getByRole('button', { name: 'Sign in'});

    fireEvent.change(email, { target: { value: 'e@mail.com' } });
    fireEvent.change(password, { target: { value: 'password' } });
    fireEvent.click(sign_in);

    // Wait for form submission
    expect(loginSpy).toHaveBeenCalledWith(
      expect.any(Object),
      expect.toHaveFormDataFields({ email: 'e@mail.com', password: 'password' })
    );
  });
});