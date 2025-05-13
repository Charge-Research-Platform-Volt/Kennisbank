// context/UserRoleContext.tsx
import React, { createContext, useContext, useState, useEffect } from 'react';

interface UserRoleContextType {
  userRole: string | null;
  loading: boolean;
}

const UserRoleContext = createContext<UserRoleContextType>({
  userRole: null,
  loading: true,
});

export const useUserRole = () => useContext(UserRoleContext);

export const UserRoleProvider = ({ children }: { children: React.ReactNode }) => {
  const [userRole, setUserRole] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchRole = async () => {
      try {
        const res = await fetch(`api/roles/current`);
        console.log('Response:', res);
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

  return (
    <UserRoleContext.Provider value={{ userRole, loading }}>
      {children}
    </UserRoleContext.Provider>
  );
};
