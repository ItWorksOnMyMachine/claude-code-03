import React, { createContext, useContext, useState, useCallback, ReactNode } from 'react';
import remoteLoader, { RemoteModuleConfig, LoadedModule } from '@/services/RemoteLoader';

export interface ModuleInfo {
  name: string;
  displayName: string;
  entry: string;
  exposedModule: string;
  route: string;
  icon?: string;
  enabled: boolean;
}

interface ModuleFederationContextType {
  modules: ModuleInfo[];
  loadedModules: Map<string, LoadedModule>;
  isLoading: boolean;
  error: Error | null;
  registerModule: (module: ModuleInfo) => void;
  unregisterModule: (name: string) => void;
  loadModule: (name: string) => Promise<LoadedModule | undefined>;
  getModule: (name: string) => LoadedModule | undefined;
  refreshModules: () => Promise<void>;
}

const ModuleFederationContext = createContext<ModuleFederationContextType | undefined>(
  undefined
);

export interface ModuleFederationProviderProps {
  children: ReactNode;
  initialModules?: ModuleInfo[];
}

export const ModuleFederationProvider: React.FC<ModuleFederationProviderProps> = ({
  children,
  initialModules = [],
}) => {
  const [modules, setModules] = useState<ModuleInfo[]>(initialModules);
  const [loadedModules, setLoadedModules] = useState<Map<string, LoadedModule>>(new Map());
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const registerModule = useCallback((module: ModuleInfo) => {
    setModules(prev => {
      const exists = prev.find(m => m.name === module.name);
      if (exists) {
        return prev.map(m => (m.name === module.name ? module : m));
      }
      return [...prev, module];
    });
  }, []);

  const unregisterModule = useCallback((name: string) => {
    setModules(prev => prev.filter(m => m.name !== name));
    setLoadedModules(prev => {
      const newMap = new Map(prev);
      newMap.delete(name);
      return newMap;
    });
    remoteLoader.unloadModule(name, './App');
  }, []);

  const loadModule = useCallback(async (name: string): Promise<LoadedModule | undefined> => {
    const moduleInfo = modules.find(m => m.name === name);
    if (!moduleInfo || !moduleInfo.enabled) {
      return undefined;
    }

    // Check if already loaded
    const existing = loadedModules.get(name);
    if (existing && !existing.error) {
      return existing;
    }

    setIsLoading(true);
    setError(null);

    try {
      const config: RemoteModuleConfig = {
        name: moduleInfo.name,
        entry: moduleInfo.entry,
        exposedModule: moduleInfo.exposedModule || './App',
      };

      const loaded = await remoteLoader.loadModule(config);
      
      setLoadedModules(prev => {
        const newMap = new Map(prev);
        newMap.set(name, loaded);
        return newMap;
      });

      if (loaded.error) {
        setError(loaded.error);
      }

      return loaded;
    } catch (err) {
      const error = err as Error;
      setError(error);
      
      const errorModule: LoadedModule = {
        name,
        module: null,
        error,
      };
      
      setLoadedModules(prev => {
        const newMap = new Map(prev);
        newMap.set(name, errorModule);
        return newMap;
      });
      
      return errorModule;
    } finally {
      setIsLoading(false);
    }
  }, [modules, loadedModules]);

  const getModule = useCallback((name: string): LoadedModule | undefined => {
    return loadedModules.get(name);
  }, [loadedModules]);

  const refreshModules = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      // In a real application, this would fetch module configuration from the API
      const response = await fetch('/api/federation/modules');
      if (response.ok) {
        const data = await response.json();
        setModules(data.modules || []);
      }
    } catch (err) {
      setError(err as Error);
      console.error('Failed to refresh modules:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const value: ModuleFederationContextType = {
    modules,
    loadedModules,
    isLoading,
    error,
    registerModule,
    unregisterModule,
    loadModule,
    getModule,
    refreshModules,
  };

  return (
    <ModuleFederationContext.Provider value={value}>
      {children}
    </ModuleFederationContext.Provider>
  );
};

export const useModuleFederation = (): ModuleFederationContextType => {
  const context = useContext(ModuleFederationContext);
  if (!context) {
    throw new Error('useModuleFederation must be used within ModuleFederationProvider');
  }
  return context;
};

export default ModuleFederationContext;