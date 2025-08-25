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
}

export class ModuleRegistry {
  private modules: Map<string, ModuleRegistryEntry> = new Map();
  private listeners: Set<(modules: ModuleRegistryEntry[]) => void> = new Set();

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
}

// Export singleton instance
export const moduleRegistry = new ModuleRegistry();
export default moduleRegistry;