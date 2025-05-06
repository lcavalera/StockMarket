import { createContext, useContext, useState } from "react";

export type UserRole = 'anonymous' | 'public' | 'premium';

interface AuthContextType {
  role: UserRole;
  login: (newRole: UserRole) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [role, setRole] = useState<UserRole>(() => {
    const stored = sessionStorage.getItem('userRole');
    return (stored as UserRole) || 'anonymous';
  });

  const login = (newRole: UserRole) => {
    setRole(newRole);
    sessionStorage.setItem('userRole', newRole);
  };

  const logout = () => {
    setRole('anonymous');
    sessionStorage.removeItem('userRole');
  };

  return (
    <AuthContext.Provider value={{ role, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};
