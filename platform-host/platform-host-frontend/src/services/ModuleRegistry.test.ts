import { describe, it, expect, beforeEach, jest } from '@jest/globals';
import { ModuleRegistry } from './ModuleRegistry';
import { RemoteModuleConfig } from './RemoteLoader';

describe('ModuleRegistry Service', () => {
  let registry: ModuleRegistry;

  beforeEach(() => {
    registry = new ModuleRegistry();
  });

  describe('Module Registration', () => {
    it('should register a new module', () => {
      const module = {
        name: 'test-module',
        entry: 'http://localhost:3003/remoteEntry.js',
        exposedModule: './App',
        displayName: 'Test Module',
        route: '/test',
        enabled: true,
      };

      registry.register(module);
      const registered = registry.getModule('test-module');
      
      expect(registered).toBeDefined();
      expect(registered?.name).toBe('test-module');
      expect(registered?.displayName).toBe('Test Module');
    });

    it('should update an existing module', () => {
      const module = {
        name: 'test-module',
        entry: 'http://localhost:3003/remoteEntry.js',
        exposedModule: './App',
        displayName: 'Test Module',
        route: '/test',
        enabled: true,
      };

      registry.register(module);
      
      const updated = {
        ...module,
        displayName: 'Updated Module',
        enabled: false,
      };
      
      registry.register(updated);
      const registered = registry.getModule('test-module');
      
      expect(registered?.displayName).toBe('Updated Module');
      expect(registered?.enabled).toBe(false);
    });

    it('should unregister a module', () => {
      const module = {
        name: 'test-module',
        entry: 'http://localhost:3003/remoteEntry.js',
        exposedModule: './App',
        displayName: 'Test Module',
        route: '/test',
        enabled: true,
      };

      registry.register(module);
      expect(registry.getModule('test-module')).toBeDefined();
      
      registry.unregister('test-module');
      expect(registry.getModule('test-module')).toBeUndefined();
    });
  });

  describe('Module Discovery', () => {
    it('should list all registered modules', () => {
      const modules = [
        {
          name: 'module1',
          entry: 'http://localhost:3003/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 1',
          route: '/module1',
          enabled: true,
        },
        {
          name: 'module2',
          entry: 'http://localhost:3004/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 2',
          route: '/module2',
          enabled: false,
        },
      ];

      modules.forEach(m => registry.register(m));
      
      const allModules = registry.getAllModules();
      expect(allModules).toHaveLength(2);
      expect(allModules.map(m => m.name)).toContain('module1');
      expect(allModules.map(m => m.name)).toContain('module2');
    });

    it('should list only enabled modules', () => {
      const modules = [
        {
          name: 'module1',
          entry: 'http://localhost:3003/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 1',
          route: '/module1',
          enabled: true,
        },
        {
          name: 'module2',
          entry: 'http://localhost:3004/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 2',
          route: '/module2',
          enabled: false,
        },
      ];

      modules.forEach(m => registry.register(m));
      
      const enabledModules = registry.getEnabledModules();
      expect(enabledModules).toHaveLength(1);
      expect(enabledModules[0].name).toBe('module1');
    });

    it('should check if a module exists', () => {
      const module = {
        name: 'test-module',
        entry: 'http://localhost:3003/remoteEntry.js',
        exposedModule: './App',
        displayName: 'Test Module',
        route: '/test',
        enabled: true,
      };

      registry.register(module);
      
      expect(registry.hasModule('test-module')).toBe(true);
      expect(registry.hasModule('non-existent')).toBe(false);
    });
  });

  describe('Module Loading Configuration', () => {
    it('should get remote config for a module', () => {
      const module = {
        name: 'test-module',
        entry: 'http://localhost:3003/remoteEntry.js',
        exposedModule: './App',
        displayName: 'Test Module',
        route: '/test',
        enabled: true,
      };

      registry.register(module);
      
      const config = registry.getRemoteConfig('test-module');
      expect(config).toBeDefined();
      expect(config?.name).toBe('test-module');
      expect(config?.entry).toBe('http://localhost:3003/remoteEntry.js');
      expect(config?.exposedModule).toBe('./App');
    });

    it('should return undefined for non-existent module config', () => {
      const config = registry.getRemoteConfig('non-existent');
      expect(config).toBeUndefined();
    });

    it('should return undefined for disabled module config', () => {
      const module = {
        name: 'test-module',
        entry: 'http://localhost:3003/remoteEntry.js',
        exposedModule: './App',
        displayName: 'Test Module',
        route: '/test',
        enabled: false,
      };

      registry.register(module);
      
      const config = registry.getRemoteConfig('test-module');
      expect(config).toBeUndefined();
    });
  });

  describe('Batch Operations', () => {
    it('should register multiple modules at once', () => {
      const modules = [
        {
          name: 'module1',
          entry: 'http://localhost:3003/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 1',
          route: '/module1',
          enabled: true,
        },
        {
          name: 'module2',
          entry: 'http://localhost:3004/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 2',
          route: '/module2',
          enabled: true,
        },
      ];

      registry.registerBatch(modules);
      
      expect(registry.hasModule('module1')).toBe(true);
      expect(registry.hasModule('module2')).toBe(true);
    });

    it('should clear all modules', () => {
      const modules = [
        {
          name: 'module1',
          entry: 'http://localhost:3003/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 1',
          route: '/module1',
          enabled: true,
        },
        {
          name: 'module2',
          entry: 'http://localhost:3004/remoteEntry.js',
          exposedModule: './App',
          displayName: 'Module 2',
          route: '/module2',
          enabled: true,
        },
      ];

      registry.registerBatch(modules);
      expect(registry.getAllModules()).toHaveLength(2);
      
      registry.clear();
      expect(registry.getAllModules()).toHaveLength(0);
    });
  });
});