import { loadRemote } from '@module-federation/enhanced/runtime';

export interface RemoteModuleConfig {
  name: string;
  entry: string;
  exposedModule: string;
}

export interface LoadedModule {
  name: string;
  module: any;
  error?: Error;
}

class RemoteLoader {
  private loadedModules: Map<string, LoadedModule> = new Map();
  private loadingPromises: Map<string, Promise<any>> = new Map();

  /**
   * Dynamically loads a remote module
   */
  async loadModule(config: RemoteModuleConfig): Promise<LoadedModule> {
    const cacheKey = `${config.name}/${config.exposedModule}`;

    // Return cached module if already loaded
    if (this.loadedModules.has(cacheKey)) {
      return this.loadedModules.get(cacheKey)!;
    }

    // Return existing loading promise if already loading
    if (this.loadingPromises.has(cacheKey)) {
      await this.loadingPromises.get(cacheKey);
      return this.loadedModules.get(cacheKey)!;
    }

    // Start loading the module
    const loadingPromise = this.performLoad(config, cacheKey);
    this.loadingPromises.set(cacheKey, loadingPromise);

    try {
      const result = await loadingPromise;
      return result;
    } finally {
      this.loadingPromises.delete(cacheKey);
    }
  }

  private async performLoad(
    config: RemoteModuleConfig,
    cacheKey: string
  ): Promise<LoadedModule> {
    try {
      // Load the remote module using Module Federation runtime
      const module = await loadRemote({
        name: config.name,
        entry: config.entry,
        exposedModule: config.exposedModule,
      });

      const loadedModule: LoadedModule = {
        name: config.name,
        module,
      };

      this.loadedModules.set(cacheKey, loadedModule);
      return loadedModule;
    } catch (error) {
      const errorModule: LoadedModule = {
        name: config.name,
        module: null,
        error: error as Error,
      };

      this.loadedModules.set(cacheKey, errorModule);
      return errorModule;
    }
  }

  /**
   * Loads multiple remote modules in parallel
   */
  async loadModules(configs: RemoteModuleConfig[]): Promise<LoadedModule[]> {
    const promises = configs.map(config => this.loadModule(config));
    return Promise.all(promises);
  }

  /**
   * Unloads a module from cache
   */
  unloadModule(name: string, exposedModule: string): void {
    const cacheKey = `${name}/${exposedModule}`;
    this.loadedModules.delete(cacheKey);
  }

  /**
   * Gets a previously loaded module
   */
  getLoadedModule(name: string, exposedModule: string): LoadedModule | undefined {
    const cacheKey = `${name}/${exposedModule}`;
    return this.loadedModules.get(cacheKey);
  }

  /**
   * Checks if a module is loaded
   */
  isModuleLoaded(name: string, exposedModule: string): boolean {
    const cacheKey = `${name}/${exposedModule}`;
    return this.loadedModules.has(cacheKey);
  }

  /**
   * Clears all loaded modules
   */
  clearCache(): void {
    this.loadedModules.clear();
    this.loadingPromises.clear();
  }
}

// Export singleton instance
export const remoteLoader = new RemoteLoader();
export default remoteLoader;