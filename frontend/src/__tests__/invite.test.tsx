import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { toast } from 'sonner';
import InvitePage from '@/app/(knowledgebank)/invite/page';
import InvitationCard from '@/app/(knowledgebank)/invite/components/invitation-card';
import { Invite } from '@/actions/adminActions';


// Mock dependencies
vi.mock('@/components/auth/RoleGuard', () => ({
  PageRoleGuard: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn()
  }
}));

vi.mock('@/actions/adminActions', () => ({
  Invite: vi.fn()
}));


// Tests for the Invite Page
describe('InvitePage', () => {
  it('renders the page with InvitationCard inside PageRoleGuard', async () => {
    const { getByText, getByPlaceholderText } = render(await InvitePage());
    
    // Check for key elements
    expect(getByText('Invite New User')).toBeTruthy();
    expect(getByPlaceholderText('Enter email address')).toBeTruthy();
  });
});


// Tests for the InvitationCard component
describe('InvitationCard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the invitation form correctly', () => {
    render(<InvitationCard />);
    
    // Check key elements are present
    expect(screen.getByText('Invite New User')).toBeTruthy();
    expect(screen.getByPlaceholderText('Enter email address')).toBeTruthy();
    expect(screen.getByRole('button', { name: /send invitation/i })).toBeTruthy();
  });

  it('submits the form with an email', async () => {
    // Mock the Invite action to return a successful response
    const mockInvite = vi.mocked(Invite).mockResolvedValue({
      success: true,
      message: 'Invitation sent successfully'
    });

    render(<InvitationCard />);
    
    // Find input and submit button
    const emailInput = screen.getByPlaceholderText('Enter email address');
    const submitButton = screen.getByRole('button', { name: /send invitation/i });

    // Simulate user input
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.click(submitButton);

    // Wait for form submission
    await waitFor(() => {
      expect(mockInvite).toHaveBeenCalled();
      expect(toast.success).toHaveBeenCalledWith('Invitation sent successfully');
    });
  });

  it('does not submit the form with an invalid email', async () => {
    // Mock the Invite action to return an unsuccesful response
    const mockInvite = vi.mocked(Invite).mockResolvedValue({
      success: false,
      message: 'Invalid email address'
    });

    render(<InvitationCard />);
    
    // Find input and submit button
    const emailInput = screen.getByPlaceholderText('Enter email address');
    const submitButton = screen.getByRole('button', { name: /send invitation/i });

    // Simulate user input
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.click(submitButton);

    // Wait for form submission
    await waitFor(() => {
      // Invite should not be called because email verification is done client-side
      expect(mockInvite).not.toHaveBeenCalled(); 
      // Should also not give a toast error
      expect(toast.error).not.toHaveBeenCalledWith('Invalid email address');
    });
  });
});