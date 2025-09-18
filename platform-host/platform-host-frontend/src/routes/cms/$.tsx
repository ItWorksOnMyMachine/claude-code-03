import React, { useEffect } from 'react';
import { useNavigate, useLocation } from '@modern-js/runtime/router';
import { Box, CircularProgress, Typography } from '@mui/material';
import { useAuth } from '@/contexts/AuthContext';
import CmsModuleLoader from '@/components/CmsModuleLoader';

/**
 * Catch-all route for CMS module paths
 * Handles all routes under /cms/* and loads the CMS microfrontend
 */
const CmsRoute: React.FC = () => {
  const { isAuthenticated, isLoading, user, selectedTenant } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    // Redirect to login if not authenticated
    if (!isLoading && !isAuthenticated) {
      const currentPath = window.location.pathname;
      navigate(`/login?redirect=${encodeURIComponent(currentPath)}`);
    }
  }, [isAuthenticated, isLoading, navigate]);

  // Show loading while checking authentication
  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100vh',
          gap: 2,
        }}
      >
        <CircularProgress />
        <Typography>Checking authentication...</Typography>
      </Box>
    );
  }

  // Don't render anything if not authenticated
  if (!isAuthenticated) {
    return null;
  }

  // Get user info to pass to CMS module
  // Authentication is handled via shared cookie, not tokens
  const tenantId = selectedTenant?.id || undefined;
  const userId = user?.id || undefined;

  // Navigation handler for the CMS module
  const handleNavigate = (path: string) => {
    navigate(path);
  };

  // Render the CMS module with user context and routing info
  // The authentication cookie is automatically sent with requests
  return (
    <CmsModuleLoader
      tenantId={tenantId}
      userId={userId}
      currentPath={location.pathname}
      onNavigate={handleNavigate}
    />
  );
};

export default CmsRoute;