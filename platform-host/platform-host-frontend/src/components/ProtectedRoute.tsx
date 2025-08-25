import React, { useEffect } from 'react';
import { Navigate, useLocation } from '@modern-js/runtime/router';
import { useAuth } from '../contexts/AuthContext';
import { CircularProgress, Box } from '@mui/material';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requireTenant?: boolean;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requireTenant = true 
}) => {
  const { isAuthenticated, isLoading, selectedTenant } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="100vh">
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    // Redirect to login with return URL
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(location.pathname)}`} replace />;
  }

  if (requireTenant && !selectedTenant) {
    // User is authenticated but hasn't selected a tenant
    return <Navigate to={`/tenant-selection?returnUrl=${encodeURIComponent(location.pathname)}`} replace />;
  }

  return <>{children}</>;
};

export default ProtectedRoute;