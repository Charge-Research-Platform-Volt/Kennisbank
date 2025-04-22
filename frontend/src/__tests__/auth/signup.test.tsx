import { render, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import SignupPage from '@/app/(auth)/signup/page';

import * as authActions from '@/actions/authActions'

vi.mock('next/navigation', () => ({
  useRouter: vi.fn().mockReturnValue({
    push: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
    refresh: vi.fn(),
    replace: vi.fn() 
  }),
}));

describe('Sign up page', () => {
  it('renders the page with fields and buttons', async () => {
    const { container, getByText, getByRole } = render(
        <SignupPage />
    );
    
    // Check for key elements
    expect(getByText('FIRST NAME')).toBeTruthy();
    expect(container.querySelector(`input[name="firstname"]`)).toBeTruthy();
    expect(getByText('LAST NAME')).toBeTruthy();
    expect(container.querySelector(`input[name="lastname"]`)).toBeTruthy();
    expect(getByText('EMAIL')).toBeTruthy();
    expect(container.querySelector(`input[name="email"]`)).toBeTruthy();
    expect(getByText('PASSWORD')).toBeTruthy();
    expect(container.querySelector(`input[name="password"]`)).toBeTruthy();
    expect(getByText('RETYPE PASSWORD')).toBeTruthy();
    expect(container.querySelector(`input[name="passwordcheck"]`)).toBeTruthy();
    expect(container.querySelector(`input[name="terms"]`)).toBeTruthy();
    expect(getByRole('button', { name: 'Sign up'})).toBeTruthy();
  });
});

// Tests for the Login Page
describe('Sign up action', () => {
  it('should call the form action with the right parameters', async () => {
    const { container, getByRole } = render(
        <SignupPage />
    );

    const registerSpy = vi.spyOn(authActions, 'Register')

    // Get input elements
    const firstName = container.querySelector(`input[name="firstname"]`)!;
    const lastName = container.querySelector(`input[name="lastname"]`)!;
    const email = container.querySelector(`input[name="email"]`)!
    const password = container.querySelector(`input[name="password"]`)!;
    const passwordre = container.querySelector(`input[name="passwordcheck"]`)!;
    const terms = container.querySelector(`input[name="terms"]`)!;
    const sign_up = getByRole('button', { name: 'Sign up'});

    fireEvent.change(firstName, { target: { value: 'firstName' } });
    fireEvent.change(lastName, { target: { value: 'lastName' } });
    fireEvent.change(email, { target: { value: 'e@mail.com' } });
    fireEvent.change(password, { target: { value: 'password' } });
    fireEvent.change(passwordre, { target: { value: 'password' } });
    fireEvent.click(terms);
    fireEvent.click(sign_up);

    expect(registerSpy).toHaveBeenLastCalledWith(
      expect.any(Object),
      expect.toHaveFormDataFields({
      firstname: 'firstName',
      lastname: 'lastName',
      email: 'e@mail.com',
      password: 'password',
      passwordcheck: 'password',
      terms: 'on',
    }))
  });
});


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


