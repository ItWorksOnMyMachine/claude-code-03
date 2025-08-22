import React from 'react';
import { Navigate, useLocation } from '@modern-js/runtime/router';
import { useAuth } from '../../contexts/AuthContext';
import { CircularProgress, Box, Typography } from '@mui/material';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermissions?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredPermissions = [] 
}) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <Box
        display="flex"
        flexDirection="column"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
      >
        <CircularProgress />
        <Typography variant="body1" sx={{ mt: 2 }}>
          Checking authentication...
        </Typography>
      </Box>
    );
  }

  if (!isAuthenticated) {
    // Save the attempted location for redirecting after login
    sessionStorage.setItem('redirectAfterLogin', location.pathname);
    return <Navigate to="/login" replace />;
  }

  // Check permissions if required
  if (requiredPermissions.length > 0 && user?.claims) {
    const hasRequiredPermissions = requiredPermissions.every(permission =>
      user.claims?.[permission] === 'true' || user.claims?.['role'] === 'admin'
    );

    if (!hasRequiredPermissions) {
      return (
        <Box
          display="flex"
          flexDirection="column"
          justifyContent="center"
          alignItems="center"
          minHeight="100vh"
        >
          <Typography variant="h5" color="error">
            Access Denied
          </Typography>
          <Typography variant="body1" sx={{ mt: 2 }}>
            You do not have permission to view this page.
          </Typography>
        </Box>
      );
    }
  }

  return <>{children}</>;
};