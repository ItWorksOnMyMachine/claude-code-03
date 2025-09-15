import React, { Suspense } from 'react';
import { Box, Typography, CircularProgress, Alert } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import CmsRouter from './CmsRouter';
import ErrorBoundary from './components/ErrorBoundary';

// Create a query client for the CMS module
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

interface CmsAppProps {
  // Platform context passed from host
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

const CmsApp: React.FC<CmsAppProps> = ({ authToken, tenantId, userId }) => {
  return (
    <ErrorBoundary
      fallback={
        <Alert severity="error" sx={{ m: 2 }}>
          <Typography variant="h6">CMS Module Error</Typography>
          <Typography>
            The CMS module encountered an error and could not be loaded. 
            Please refresh the page or contact support if the issue persists.
          </Typography>
        </Alert>
      }
    >
      <QueryClientProvider client={queryClient}>
        <Suspense
          fallback={
            <Box
              display="flex"
              justifyContent="center"
              alignItems="center"
              minHeight="400px"
              flexDirection="column"
              gap={2}
            >
              <CircularProgress />
              <Typography variant="body1" color="textSecondary">
                Loading CMS Module...
              </Typography>
            </Box>
          }
        >
          <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <CmsRouter 
              authToken={authToken}
              tenantId={tenantId}
              userId={userId}
            />
          </Box>
        </Suspense>
      </QueryClientProvider>
    </ErrorBoundary>
  );
};

export default CmsApp;