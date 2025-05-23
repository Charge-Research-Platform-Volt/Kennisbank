"use client"

// context/UserRoleContext.tsx
import React, { createContext, useContext, useState, useEffect } from 'react';

// Define the type for the context value
interface UserRoleContextType {
  userRole: string | null;
  loading: boolean;
}

// Create the context with a default value
const UserRoleContext = createContext<UserRoleContextType>({
  userRole: null,
  loading: true,
});

// Create a custom hook to use the UserRoleContext
export const useUserRole = () => useContext(UserRoleContext);

// Create a provider component
export const UserRoleProvider = ({ children }: { children: React.ReactNode }) => {
  // State to hold the user role and loading state
  const [userRole, setUserRole] = useState<string | null>(null);

  // State to hold the loading state
  const [loading, setLoading] = useState(true);

  // Effect to fetch the user role from the API
  useEffect(() => {
    const fetchRole = async () => {
      try {
        const res = await fetch(`api/roles/current`);
        const data = await res.json();
        setUserRole(data.role);
      } catch (error) {
        console.error('Error fetching user role:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchRole();
  }, []);
  
  // pass the user role and loading state to the context
  return (
    <UserRoleContext.Provider value={{ userRole, loading }}>
      {children}
    </UserRoleContext.Provider>
  );
};
