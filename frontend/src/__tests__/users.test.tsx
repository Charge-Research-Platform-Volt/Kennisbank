import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { toast } from 'sonner';
import UsersPage from '@/app/(knowledgebank)/users/page';
import InvitationCard from '@/app/(knowledgebank)/users/components/invitation-card';
import { Invite } from '@/actions/adminActions';
import UsersList from '@/app/(knowledgebank)/users/components/UsersList';
import { ListUsersPaged } from '@/actions/userActions';

// Mock dependencies
vi.mock('@/components/auth/RoleGuard', () => ({
  PageRoleGuard: ({ children }: { children: React.ReactNode }) => <div>{children}</div>
}));

vi.mock('next/navigation', () => ({
  useRouter: vi.fn().mockReturnValue({
    push: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
    refresh: vi.fn(),
    replace: vi.fn() 
  }),
  useSearchParams: vi.fn().mockReturnValue(new URLSearchParams()), 
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

// Mock ListUsersPaged once for all tests
vi.mock('@/actions/userActions', () => ({
  ListUsersPaged: vi.fn()
}));

// Tests for the Invite Page
describe('InvitePage', () => {
  it('renders the page with InvitationCard inside PageRoleGuard', async () => {
    const { getByText, getByPlaceholderText } = render(await UsersPage());
    
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
    act(() => {
      fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
      fireEvent.click(submitButton);
    })

    // Wait for form submission
    await waitFor(() => {
      expect(mockInvite).toHaveBeenCalled();
      expect(toast.success).toHaveBeenCalledWith('Invitation sent successfully');
    });
  });

  it('does not submit the form with an invalid email', async () => {
    // Mock the Invite action to return an unsuccessful response
    const mockInvite = vi.mocked(Invite).mockResolvedValue({
      success: false,
      message: 'Invalid email address'
    });

    render(<InvitationCard />);
    
    // Find input and submit button
    const emailInput = screen.getByPlaceholderText('Enter email address');
    const submitButton = screen.getByRole('button', { name: /send invitation/i });

    // Simulate user input
    act(() => {
      fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
      fireEvent.click(submitButton);
    })

    // Wait for form submission
    await waitFor(() => {
      // Invite should not be called because email verification is done client-side
      expect(mockInvite).not.toHaveBeenCalled(); 
      // Should also not give a toast error
      expect(toast.error).not.toHaveBeenCalledWith('Invalid email address');
    });
  });
});

// Tests for the UsersList component
describe('UsersList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    render(<UsersList />);
    expect(screen.getByText('Loading users...')).toBeTruthy();
  });

  it('displays users when data is loaded', async () => {
    // Mocking the ListUsersPaged function with a resolved value
    vi.mocked(ListUsersPaged).mockResolvedValue({
      success: true,
      message: 'Users fetched successfully',
      users: [
        { id: crypto.randomUUID(), email: 'user1@example.com', username: 'user1@example.com',role: 'user', emailConfirmed: true },
        { id: crypto.randomUUID(), email: 'user2@example.com', username: 'user2@example.com', role: 'admin', emailConfirmed: true }
      ],
      pageCount: 2
    });

    render(<UsersList />);
    
    await waitFor(() => {
      expect(screen.getByText('user1@example.com')).toBeTruthy();
      expect(screen.getByText('user2@example.com')).toBeTruthy();
    });
  });

  it('handles API error gracefully', async () => {
    // Mocking an error response for ListUsersPaged
    vi.mocked(ListUsersPaged).mockResolvedValue({ success: false, message: 'Error fetching users' });
    
    render(<UsersList />);
    
    await waitFor(() => {
      expect(screen.getByText('Error fetching users')).toBeTruthy();
    });
  });

  it('navigates pages correctly', async () => {
    // Mocking a successful response with multiple pages
    vi.mocked(ListUsersPaged).mockResolvedValue({
      success: true,
      message: 'Users fetched successfully',
      users: [
        { id: crypto.randomUUID(), email: 'user1@example.com', username: 'user1@example.com', role: 'user', emailConfirmed: true },
      ],
      pageCount: 3
    });

    render(<UsersList />);
    
    await waitFor(() => expect(screen.getByText('user1@example.com')).toBeTruthy());
    
    const nextButton = screen.getByRole('button', { name: /next/i });
    act(() => {
      fireEvent.click(nextButton);
    })
    
    await waitFor(() => expect(vi.mocked(ListUsersPaged)).toHaveBeenCalledWith(2, ""));
  });

  it('Searches correctly', async () => {
    // Mocking a successful response with multiple pages
    vi.mocked(ListUsersPaged).mockImplementation(async (_page, searchQuery) => {
      {
        const users = [
          { id: crypto.randomUUID(), email: 'user1@example.com', username: 'user1@example.com', role: 'user', emailConfirmed: true },
          { id: crypto.randomUUID(), email: 'user2@example.com', username: 'user2@example.com', role: 'user', emailConfirmed: true },
        ];

        const filteredUsers = users.filter(user => user.email.includes(searchQuery));
        return {
          success: true,
          message: 'Users fetched successfully',
          users: filteredUsers,
          pageCount: 3
        };
      }
    });

    render(<UsersList />);
    
    await waitFor(() => expect(screen.getByText('user1@example.com')).toBeTruthy());
    await waitFor(() => expect(screen.getByText('user2@example.com')).toBeTruthy());
    
    const searchBar = screen.getByPlaceholderText('Search');
    await act(() => {
      fireEvent.change(searchBar, { target: { value: 'user1' } });
    });

    await new Promise(resolve => setTimeout(resolve, 1000));
    
    await waitFor(() => expect(vi.mocked(ListUsersPaged)).toHaveBeenCalledWith(1, "user1"));
    await waitFor(() => expect(screen.getByText('user1@example.com')).toBeTruthy());
    await waitFor(() => expect(screen.queryByText('user2@example.com')).not.toBeInTheDocument());
  });
});



// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


