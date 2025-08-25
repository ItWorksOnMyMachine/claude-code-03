import moduleRegistry, { ModuleRegistryEntry } from '@/services/ModuleRegistry';
import remoteLoader from '@/services/RemoteLoader';

export interface ModuleManifest {
  name: string;
  version: string;
  displayName: string;
  description?: string;
  entry: string;
  exposedModule: string;
  route: string;
  icon?: string;
  dependencies?: string[];
  permissions?: string[];
  metadata?: Record<string, any>;
}

export class ModuleDiscovery {
  private discoveryEndpoint: string;
  private pollingInterval: number;
  private pollingTimer?: NodeJS.Timeout;

  constructor(
    discoveryEndpoint: string = '/api/federation/modules',
    pollingInterval: number = 60000 // 1 minute
  ) {
    this.discoveryEndpoint = discoveryEndpoint;
    this.pollingInterval = pollingInterval;
  }

  /**
   * Start automatic module discovery
   */
  async startDiscovery(): Promise<void> {
    // Initial discovery
    await this.discoverModules();

    // Set up polling
    this.pollingTimer = setInterval(async () => {
      try {
        await this.discoverModules();
      } catch (error) {
        console.error('Module discovery failed:', error);
      }
    }, this.pollingInterval);
  }

  /**
   * Stop automatic module discovery
   */
  stopDiscovery(): void {
    if (this.pollingTimer) {
      clearInterval(this.pollingTimer);
      this.pollingTimer = undefined;
    }
  }

  /**
   * Discover modules from the configured endpoint
   */
  async discoverModules(): Promise<ModuleRegistryEntry[]> {
    try {
      const response = await fetch(this.discoveryEndpoint);
      
      if (!response.ok) {
        throw new Error(`Discovery failed: ${response.statusText}`);
      }

      const data = await response.json();
      const manifests: ModuleManifest[] = data.modules || [];
      
      const modules = manifests.map(manifest => this.manifestToRegistryEntry(manifest));
      
      // Register all discovered modules
      moduleRegistry.registerBatch(modules);
      
      return modules;
    } catch (error) {
      console.error('Failed to discover modules:', error);
      throw error;
    }
  }

  /**
   * Convert module manifest to registry entry
   */
  private manifestToRegistryEntry(manifest: ModuleManifest): ModuleRegistryEntry {
    return {
      name: manifest.name,
      entry: manifest.entry,
      exposedModule: manifest.exposedModule || './App',
      displayName: manifest.displayName,
      route: manifest.route,
      enabled: true,
      icon: manifest.icon,
      description: manifest.description,
      version: manifest.version,
      dependencies: manifest.dependencies,
      permissions: manifest.permissions,
      metadata: manifest.metadata,
    };
  }

  /**
   * Verify module health
   */
  async verifyModuleHealth(moduleName: string): Promise<boolean> {
    try {
      const module = moduleRegistry.getModule(moduleName);
      
      if (!module) {
        return false;
      }

      // Try to fetch the remote entry
      const response = await fetch(module.entry, { method: 'HEAD' });
      return response.ok;
    } catch (error) {
      console.error(`Module ${moduleName} health check failed:`, error);
      return false;
    }
  }

  /**
   * Verify all modules health
   */
  async verifyAllModulesHealth(): Promise<Map<string, boolean>> {
    const modules = moduleRegistry.getAllModules();
    const healthStatus = new Map<string, boolean>();

    await Promise.all(
      modules.map(async (module) => {
        const isHealthy = await this.verifyModuleHealth(module.name);
        healthStatus.set(module.name, isHealthy);
        
        // Disable unhealthy modules
        if (!isHealthy) {
          moduleRegistry.disableModule(module.name);
        }
      })
    );

    return healthStatus;
  }

  /**
   * Preload a module
   */
  async preloadModule(moduleName: string): Promise<void> {
    const config = moduleRegistry.getRemoteConfig(moduleName);
    
    if (!config) {
      throw new Error(`Module ${moduleName} not found in registry`);
    }

    // Preload the module but don't mount it
    await remoteLoader.loadModule(config);
  }

  /**
   * Preload multiple modules
   */
  async preloadModules(moduleNames: string[]): Promise<void> {
    await Promise.all(
      moduleNames.map(name => this.preloadModule(name))
    );
  }
}

// Export singleton instance
export const moduleDiscovery = new ModuleDiscovery();

/**
 * Initialize module system
 */
export async function initializeModuleSystem(
  options: {
    discoveryEndpoint?: string;
    pollingInterval?: number;
    autoStart?: boolean;
    preloadModules?: string[];
  } = {}
): Promise<void> {
  const {
    discoveryEndpoint = '/api/federation/modules',
    pollingInterval = 60000,
    autoStart = true,
    preloadModules = [],
  } = options;

  // Create discovery instance
  const discovery = new ModuleDiscovery(discoveryEndpoint, pollingInterval);

  try {
    // Initial discovery
    await discovery.discoverModules();

    // Verify module health
    await discovery.verifyAllModulesHealth();

    // Preload specified modules
    if (preloadModules.length > 0) {
      await discovery.preloadModules(preloadModules);
    }

    // Start automatic discovery if requested
    if (autoStart) {
      await discovery.startDiscovery();
    }

    console.log('Module system initialized successfully');
  } catch (error) {
    console.error('Failed to initialize module system:', error);
    throw error;
  }
}

export default moduleDiscovery;