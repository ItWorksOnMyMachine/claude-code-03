import React, { Suspense, lazy, useEffect, useState } from 'react';
import { Box, Alert } from '@mui/material';
import { useModuleFederation } from '@/contexts/ModuleFederationContext';
import LoadingFallback from '@/components/LoadingFallback';
import ErrorFallback from '@/components/ErrorFallback';
import ErrorBoundary from '@/components/ErrorBoundary';
import remoteLoader from '@/services/RemoteLoader';

export interface RemoteModuleProps {
  moduleName: string;
  fallback?: React.ReactNode;
  onError?: (error: Error) => void;
  onLoad?: () => void;
  props?: Record<string, any>;
}

export const RemoteModule: React.FC<RemoteModuleProps> = ({
  moduleName,
  fallback = <LoadingFallback message={`Loading ${moduleName}...`} />,
  onError,
  onLoad,
  props = {},
}) => {
  const { modules, loadModule } = useModuleFederation();
  const [Component, setComponent] = useState<React.ComponentType | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let mounted = true;

    const loadRemoteModule = async () => {
      try {
        setIsLoading(true);
        setError(null);

        // Load the module using ModuleFederation context
        const loadedModule = await loadModule(moduleName);

        if (!mounted) return;

        if (loadedModule?.error) {
          throw loadedModule.error;
        }

        if (loadedModule?.module) {
          // Handle both default export and named exports
          const ModuleComponent = loadedModule.module.default || loadedModule.module;
          
          if (typeof ModuleComponent === 'function' || 
              (typeof ModuleComponent === 'object' && ModuleComponent !== null)) {
            setComponent(() => ModuleComponent);
            onLoad?.();
          } else {
            throw new Error(`Invalid module export from ${moduleName}`);
          }
        } else {
          throw new Error(`Module ${moduleName} not found or not enabled`);
        }
      } catch (err) {
        const error = err as Error;
        console.error(`Failed to load module ${moduleName}:`, error);
        
        if (mounted) {
          setError(error);
          onError?.(error);
        }
      } finally {
        if (mounted) {
          setIsLoading(false);
        }
      }
    };

    loadRemoteModule();

    return () => {
      mounted = false;
    };
  }, [moduleName, loadModule, onError, onLoad]);

  if (error) {
    return (
      <ErrorFallback
        error={error}
        resetError={() => {
          setError(null);
          setIsLoading(true);
          // Trigger reload
          window.location.reload();
        }}
      />
    );
  }

  if (isLoading) {
    return <>{fallback}</>;
  }

  if (!Component) {
    return (
      <Alert severity="warning">
        Module {moduleName} could not be loaded. Please check if it's properly configured.
      </Alert>
    );
  }

  return (
    <ErrorBoundary isolate moduleName={moduleName}>
      <Suspense fallback={fallback}>
        <Component {...props} />
      </Suspense>
    </ErrorBoundary>
  );
};

/**
 * Hook to dynamically load a remote module
 */
export const useRemoteModule = (moduleName: string) => {
  const [Component, setComponent] = useState<React.ComponentType | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const { loadModule } = useModuleFederation();

  const load = React.useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const loadedModule = await loadModule(moduleName);

      if (loadedModule?.error) {
        throw loadedModule.error;
      }

      if (loadedModule?.module) {
        const ModuleComponent = loadedModule.module.default || loadedModule.module;
        setComponent(() => ModuleComponent);
      } else {
        throw new Error(`Module ${moduleName} not found`);
      }
    } catch (err) {
      const error = err as Error;
      setError(error);
      console.error(`Failed to load module ${moduleName}:`, error);
    } finally {
      setIsLoading(false);
    }
  }, [moduleName, loadModule]);

  return {
    Component,
    error,
    isLoading,
    load,
  };
};

export default RemoteModule;