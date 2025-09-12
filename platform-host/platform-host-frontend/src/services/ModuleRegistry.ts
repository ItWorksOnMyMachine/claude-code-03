import { RemoteModuleConfig } from './RemoteLoader';

export interface ModuleRegistryEntry {
  name: string;
  entry: string;
  exposedModule: string;
  displayName: string;
  route: string;
  enabled: boolean;
  icon?: string;
  description?: string;
  version?: string;
  dependencies?: string[];
  permissions?: string[];
  metadata?: Record<string, any>;
  healthCheckUrl?: string;
  requiredEntitlements?: string[];
  tags?: string[];
}

export class ModuleRegistry {
  private modules: Map<string, ModuleRegistryEntry> = new Map();
  private listeners: Set<(modules: ModuleRegistryEntry[]) => void> = new Set();
  private moduleHealthStatus: Map<string, boolean> = new Map();
  private initialized: boolean = false;

  /**
   * Initialize with default modules (called lazily)
   */
  private ensureInitialized(): void {
    if (!this.initialized) {
      this.initializeDefaultModules();
      this.initialized = true;
    }
  }

  /**
   * Register a module in the registry
   */
  register(module: ModuleRegistryEntry): void {
    this.modules.set(module.name, module);
    this.notifyListeners();
  }

  /**
   * Register multiple modules at once
   */
  registerBatch(modules: ModuleRegistryEntry[]): void {
    modules.forEach(module => {
      this.modules.set(module.name, module);
    });
    this.notifyListeners();
  }

  /**
   * Unregister a module from the registry
   */
  unregister(name: string): void {
    this.modules.delete(name);
    this.notifyListeners();
  }

  /**
   * Get a specific module by name
   */
  getModule(name: string): ModuleRegistryEntry | undefined {
    return this.modules.get(name);
  }

  /**
   * Check if a module exists in the registry
   */
  hasModule(name: string): boolean {
    return this.modules.has(name);
  }

  /**
   * Get all registered modules
   */
  getAllModules(): ModuleRegistryEntry[] {
    this.ensureInitialized();
    return Array.from(this.modules.values());
  }

  /**
   * Get only enabled modules
   */
  getEnabledModules(): ModuleRegistryEntry[] {
    return Array.from(this.modules.values()).filter(module => module.enabled);
  }

  /**
   * Get modules by route prefix
   */
  getModulesByRoute(routePrefix: string): ModuleRegistryEntry[] {
    return Array.from(this.modules.values()).filter(
      module => module.route.startsWith(routePrefix)
    );
  }

  /**
   * Get remote configuration for a module
   */
  getRemoteConfig(name: string): RemoteModuleConfig | undefined {
    const module = this.modules.get(name);
    
    if (!module || !module.enabled) {
      return undefined;
    }

    return {
      name: module.name,
      entry: module.entry,
      exposedModule: module.exposedModule,
    };
  }

  /**
   * Get remote configurations for all enabled modules
   */
  getAllRemoteConfigs(): RemoteModuleConfig[] {
    return this.getEnabledModules().map(module => ({
      name: module.name,
      entry: module.entry,
      exposedModule: module.exposedModule,
    }));
  }

  /**
   * Enable a module
   */
  enableModule(name: string): void {
    const module = this.modules.get(name);
    if (module) {
      module.enabled = true;
      this.modules.set(name, module);
      this.notifyListeners();
    }
  }

  /**
   * Disable a module
   */
  disableModule(name: string): void {
    const module = this.modules.get(name);
    if (module) {
      module.enabled = false;
      this.modules.set(name, module);
      this.notifyListeners();
    }
  }

  /**
   * Clear all modules from the registry
   */
  clear(): void {
    this.modules.clear();
    this.moduleHealthStatus.clear();
    this.initialized = false;
    this.notifyListeners();
  }

  /**
   * Subscribe to registry changes
   */
  subscribe(listener: (modules: ModuleRegistryEntry[]) => void): () => void {
    this.listeners.add(listener);
    
    // Return unsubscribe function
    return () => {
      this.listeners.delete(listener);
    };
  }

  /**
   * Notify all listeners of registry changes
   */
  private notifyListeners(): void {
    const modules = this.getAllModules();
    this.listeners.forEach(listener => listener(modules));
  }

  /**
   * Load modules from a remote configuration endpoint
   */
  async loadFromRemote(endpoint: string): Promise<void> {
    try {
      const response = await fetch(endpoint);
      if (!response.ok) {
        throw new Error(`Failed to load modules: ${response.statusText}`);
      }

      const data = await response.json();
      const modules: ModuleRegistryEntry[] = data.modules || [];
      
      this.registerBatch(modules);
    } catch (error) {
      console.error('Failed to load modules from remote:', error);
      throw error;
    }
  }

  /**
   * Export registry state for persistence
   */
  export(): ModuleRegistryEntry[] {
    return this.getAllModules();
  }

  /**
   * Import registry state from persistence
   */
  import(modules: ModuleRegistryEntry[]): void {
    this.clear();
    this.registerBatch(modules);
  }

  /**
   * Initialize with default modules like CMS
   */
  private initializeDefaultModules(): void {
    // Register CMS module directly without triggering listeners (to avoid circular dependency)
    this.modules.set('cmsModule', {
      name: 'cmsModule',
      entry: 'https://cms.platform.local:3003/remoteEntry.js',
      exposedModule: './CmsApp',
      displayName: 'Content Management System',
      route: '/cms',
      enabled: true,
      icon: 'EditNote',
      description: 'Create and manage content using a visual editor',
      version: '1.0.0',
      healthCheckUrl: 'https://cms.platform.local:3003/health',
      requiredEntitlements: ['CMS_ACCESS'],
      tags: ['content', 'editor', 'cms'],
      permissions: ['CMS_MANAGE', 'CMS_ASSETS'],
    });
  }

  /**
   * Check health status of a specific module
   */
  async checkModuleHealth(moduleName: string): Promise<boolean> {
    const module = this.modules.get(moduleName);
    if (!module) {
      return false;
    }

    try {
      const url = module.healthCheckUrl || module.entry;
      const response = await fetch(url, {
        method: 'HEAD',
        mode: 'no-cors',
      });
      
      this.moduleHealthStatus.set(moduleName, true);
      return true;
    } catch (error) {
      console.warn(`Health check failed for module ${moduleName}:`, error);
      this.moduleHealthStatus.set(moduleName, false);
      return false;
    }
  }

  /**
   * Check health status of all modules
   */
  async checkAllModuleHealth(): Promise<Map<string, boolean>> {
    const healthPromises = Array.from(this.modules.keys()).map(async (moduleName) => {
      const isHealthy = await this.checkModuleHealth(moduleName);
      return { moduleName, isHealthy };
    });

    const results = await Promise.allSettled(healthPromises);
    
    results.forEach((result) => {
      if (result.status === 'fulfilled') {
        this.moduleHealthStatus.set(result.value.moduleName, result.value.isHealthy);
      }
    });

    return new Map(this.moduleHealthStatus);
  }

  /**
   * Get module health status
   */
  getModuleHealthStatus(moduleName: string): boolean | undefined {
    return this.moduleHealthStatus.get(moduleName);
  }

  /**
   * Get modules filtered by user entitlements
   */
  getModulesForUser(userEntitlements: string[] = []): ModuleRegistryEntry[] {
    return this.getEnabledModules().filter(module => {
      if (!module.requiredEntitlements || module.requiredEntitlements.length === 0) {
        return true;
      }
      
      return module.requiredEntitlements.some(entitlement =>
        userEntitlements.includes(entitlement)
      );
    });
  }

  /**
   * Search modules by name, description, or tags
   */
  searchModules(query: string): ModuleRegistryEntry[] {
    const lowercaseQuery = query.toLowerCase();
    
    return this.getAllModules().filter(module => {
      const nameMatch = module.name.toLowerCase().includes(lowercaseQuery);
      const displayNameMatch = module.displayName.toLowerCase().includes(lowercaseQuery);
      const descriptionMatch = module.description?.toLowerCase().includes(lowercaseQuery);
      const tagMatch = module.tags?.some(tag => 
        tag.toLowerCase().includes(lowercaseQuery)
      );
      
      return nameMatch || displayNameMatch || descriptionMatch || tagMatch;
    });
  }
}

// Export singleton instance
export const moduleRegistry = new ModuleRegistry();
export default moduleRegistry;