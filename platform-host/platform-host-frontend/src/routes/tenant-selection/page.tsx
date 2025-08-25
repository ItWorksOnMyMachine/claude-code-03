import React, { useCallback } from 'react';
import { useNavigate, useSearchParams } from '@modern-js/runtime/router';
import { Container, Box, Typography } from '@mui/material';
import { TenantSelector } from '../../components/TenantSelector';
import { useAuth } from '../../contexts/AuthContext';

const TenantSelectionPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAuthenticated, isLoading } = useAuth();
  
  // Get the return URL from query params, default to dashboard
  const returnUrl = searchParams.get('returnUrl') || '/dashboard';

  const handleTenantSelect = useCallback((tenantId: string) => {
    // After tenant selection, navigate to the return URL
    navigate(returnUrl);
  }, [navigate, returnUrl]);

  // If not authenticated, redirect to login
  React.useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate('/login?returnUrl=/tenant-selection');
    }
  }, [isAuthenticated, isLoading, navigate]);

  if (isLoading) {
    return (
      <Container maxWidth="md">
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="100vh">
          <Typography>Loading...</Typography>
        </Box>
      </Container>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <Container maxWidth="sm">
      <Box
        display="flex"
        flexDirection="column"
        justifyContent="center"
        minHeight="100vh"
        py={4}
      >
        <Box mb={4} textAlign="center">
          <Typography variant="h3" component="h1" gutterBottom>
            Welcome Back!
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Please select an organization to continue
          </Typography>
        </Box>
        
        <TenantSelector
          onTenantSelect={handleTenantSelect}
          showHeader={false}
        />
      </Box>
    </Container>
  );
};

export default TenantSelectionPage;