import { useModuleFederation } from '@/contexts/ModuleFederationContext';
import { cmsModuleService } from '@/services/CmsModuleService';
import { Refresh as RefreshIcon } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Typography,
} from '@mui/material';
import React, { Suspense, useEffect, useState } from 'react';

interface CmsModuleLoaderProps {
  // Platform context to pass to CMS module
  authToken?: string;
  tenantId?: string;
  userId?: string;
  currentPath?: string;
  onNavigate?: (path: string) => void;
}

interface CmsModuleState {
  isHealthy: boolean;
  isChecking: boolean;
  error: Error | null;
  LazyComponent: React.LazyExoticComponent<React.ComponentType<any>> | null;
}

/**
 * Component that loads and renders the CMS module with error boundaries and health checks
 */
const CmsModuleLoader: React.FC<CmsModuleLoaderProps> = ({
  authToken,
  tenantId,
  userId,
  currentPath,
  onNavigate,
}) => {
  const { registerModule, loadModule, getModule } = useModuleFederation();
  const [state, setState] = useState<CmsModuleState>({
    isHealthy: false,
    isChecking: true,
    error: null,
    LazyComponent: null,
  });

  const checkHealthAndLoad = async () => {
    setState(prev => ({ ...prev, isChecking: true, error: null }));

    try {
      // First check if CMS module is available
      const isHealthy = await cmsModuleService.checkCmsModuleHealth();

      if (!isHealthy) {
        throw new Error(
          'CMS module is not available. Please ensure the CMS service is running on https://cms-fe.platform.local:3003',
        );
      }

      // Register the module with the federation context
      const moduleInfo = cmsModuleService.getModuleInfo();
      registerModule(moduleInfo);

      // Create the lazy component
      const LazyComponent = cmsModuleService.createCmsModuleComponent();

      setState(prev => ({
        ...prev,
        isHealthy: true,
        isChecking: false,
        LazyComponent,
      }));

      console.log('CMS module registered and ready');
    } catch (error) {
      console.error('CMS module health check or loading failed:', error);
      setState(prev => ({
        ...prev,
        isHealthy: false,
        isChecking: false,
        error: error as Error,
        LazyComponent: null,
      }));
    }
  };

  useEffect(() => {
    checkHealthAndLoad();
  }, []);

  const handleRetry = () => {
    checkHealthAndLoad();
  };

  // Loading state
  if (state.isChecking) {
    return (
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
          Checking CMS module availability...
        </Typography>
      </Box>
    );
  }

  // Error state
  if (state.error || !state.isHealthy || !state.LazyComponent) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert
          severity="error"
          sx={{ mb: 2 }}
          action={
            <Button
              color="inherit"
              size="small"
              startIcon={<RefreshIcon />}
              onClick={handleRetry}
            >
              Retry
            </Button>
          }
        >
          <Typography variant="h6" gutterBottom>
            CMS Module Unavailable
          </Typography>
          <Typography variant="body2">
            {state.error?.message || 'The CMS module could not be loaded.'}
          </Typography>
          <Typography variant="body2" sx={{ mt: 1 }}>
            Please ensure the CMS service is running and try again.
          </Typography>
        </Alert>

        <Box sx={{ mt: 2, p: 2, backgroundColor: 'grey.50', borderRadius: 1 }}>
          <Typography variant="body2" color="textSecondary">
            <strong>Troubleshooting:</strong>
          </Typography>
          <Typography variant="body2" component="ul" sx={{ pl: 2, mt: 1 }}>
            <li>
              Check if CMS service is running on
              https://cms-fe.platform.local:3003
            </li>
            <li>Verify network connectivity</li>
            <li>Check browser console for additional error details</li>
          </Typography>
        </Box>
      </Box>
    );
  }

  // Success state - render the lazy-loaded CMS module
  const { LazyComponent } = state;

  return (
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
      <Box sx={{ height: '100%' }}>
        <LazyComponent
          authToken={authToken}
          tenantId={tenantId}
          userId={userId}
          currentPath={currentPath}
          onNavigate={onNavigate}
        />
      </Box>
    </Suspense>
  );
};

export default CmsModuleLoader;
