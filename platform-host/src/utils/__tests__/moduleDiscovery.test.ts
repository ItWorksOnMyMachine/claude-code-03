import { describe, it, expect, beforeEach, jest, afterEach } from '@jest/globals';
import { ModuleDiscovery } from '../moduleDiscovery';
import moduleRegistry from '@/services/ModuleRegistry';

// Mock fetch
global.fetch = jest.fn() as jest.MockedFunction<typeof fetch>;

// Mock module registry
jest.mock('@/services/ModuleRegistry');

// Mock remote loader
jest.mock('@/services/RemoteLoader', () => ({
  default: {
    loadModule: jest.fn(),
  },
}));

describe('ModuleDiscovery', () => {
  let discovery: ModuleDiscovery;
  let consoleErrorSpy: jest.SpyInstance;

  beforeEach(() => {
    jest.clearAllMocks();
    
    // Setup moduleRegistry mocks
    (moduleRegistry.registerBatch as jest.Mock) = jest.fn();
    (moduleRegistry.getModule as jest.Mock) = jest.fn();
    (moduleRegistry.getAllModules as jest.Mock) = jest.fn();
    (moduleRegistry.disableModule as jest.Mock) = jest.fn();
    
    discovery = new ModuleDiscovery('/api/federation/modules', 60000);
    // Suppress expected error logs
    consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    discovery.stopDiscovery();
    consoleErrorSpy.mockRestore();
  });

  describe('discoverModules', () => {
    it('should fetch and register modules from API', async () => {
      const mockModules = [
        {
          name: 'module1',
          version: '1.0.0',
          displayName: 'Module 1',
          entry: 'http://localhost:3003/remoteEntry.js',
          exposedModule: './App',
          route: '/module1',
        },
        {
          name: 'module2',
          version: '1.0.0',
          displayName: 'Module 2',
          entry: 'http://localhost:3004/remoteEntry.js',
          exposedModule: './App',
          route: '/module2',
        },
      ];

      (global.fetch as jest.MockedFunction<typeof fetch>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ modules: mockModules }),
      } as Response);

      const modules = await discovery.discoverModules();

      expect(fetch).toHaveBeenCalledWith('/api/federation/modules');
      expect(moduleRegistry.registerBatch).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            name: 'module1',
            displayName: 'Module 1',
            enabled: true,
          }),
          expect.objectContaining({
            name: 'module2',
            displayName: 'Module 2',
            enabled: true,
          }),
        ])
      );
      expect(modules).toHaveLength(2);
    });

    it('should handle API errors gracefully', async () => {
      (global.fetch as jest.MockedFunction<typeof fetch>).mockResolvedValueOnce({
        ok: false,
        statusText: 'Not Found',
      } as Response);

      await expect(discovery.discoverModules()).rejects.toThrow('Discovery failed: Not Found');
    });

    it('should handle network errors', async () => {
      (global.fetch as jest.MockedFunction<typeof fetch>).mockRejectedValueOnce(
        new Error('Network error')
      );

      await expect(discovery.discoverModules()).rejects.toThrow('Network error');
    });
  });

  describe('verifyModuleHealth', () => {
    it('should return true for healthy module', async () => {
      (moduleRegistry.getModule as jest.Mock).mockReturnValue({
        name: 'module1',
        entry: 'http://localhost:3003/remoteEntry.js',
        enabled: true,
      });

      (global.fetch as jest.MockedFunction<typeof fetch>).mockResolvedValueOnce({
        ok: true,
      } as Response);

      const isHealthy = await discovery.verifyModuleHealth('module1');

      expect(isHealthy).toBe(true);
      expect(fetch).toHaveBeenCalledWith(
        'http://localhost:3003/remoteEntry.js',
        { method: 'HEAD' }
      );
    });

    it('should return false for non-existent module', async () => {
      (moduleRegistry.getModule as jest.Mock).mockReturnValue(undefined);

      const isHealthy = await discovery.verifyModuleHealth('non-existent');

      expect(isHealthy).toBe(false);
      expect(fetch).not.toHaveBeenCalled();
    });

    it('should return false for unhealthy module', async () => {
      (moduleRegistry.getModule as jest.Mock).mockReturnValue({
        name: 'module1',
        entry: 'http://localhost:3003/remoteEntry.js',
        enabled: true,
      });

      (global.fetch as jest.MockedFunction<typeof fetch>).mockResolvedValueOnce({
        ok: false,
      } as Response);

      const isHealthy = await discovery.verifyModuleHealth('module1');

      expect(isHealthy).toBe(false);
    });
  });

  describe('verifyAllModulesHealth', () => {
    it('should verify health of all modules and disable unhealthy ones', async () => {
      const mockModules = [
        { name: 'module1', entry: 'http://localhost:3003/remoteEntry.js', enabled: true },
        { name: 'module2', entry: 'http://localhost:3004/remoteEntry.js', enabled: true },
      ];

      (moduleRegistry.getAllModules as jest.Mock).mockReturnValue(mockModules);
      (moduleRegistry.getModule as jest.Mock)
        .mockReturnValueOnce(mockModules[0])
        .mockReturnValueOnce(mockModules[1]);

      // First module is healthy
      (global.fetch as jest.MockedFunction<typeof fetch>)
        .mockResolvedValueOnce({ ok: true } as Response)
        .mockResolvedValueOnce({ ok: false } as Response);

      const healthStatus = await discovery.verifyAllModulesHealth();

      expect(healthStatus.get('module1')).toBe(true);
      expect(healthStatus.get('module2')).toBe(false);
      expect(moduleRegistry.disableModule).toHaveBeenCalledWith('module2');
      expect(moduleRegistry.disableModule).not.toHaveBeenCalledWith('module1');
    });
  });

  describe('startDiscovery', () => {
    it('should start automatic discovery with polling', async () => {
      jest.useFakeTimers();
      
      (global.fetch as jest.MockedFunction<typeof fetch>).mockResolvedValue({
        ok: true,
        json: async () => ({ modules: [] }),
      } as Response);

      await discovery.startDiscovery();

      expect(fetch).toHaveBeenCalledTimes(1);

      // Fast-forward time by polling interval
      jest.advanceTimersByTime(60000);

      // Wait for the async operation
      await Promise.resolve();

      expect(fetch).toHaveBeenCalledTimes(2);
      
      jest.useRealTimers();
    });
  });

  describe('stopDiscovery', () => {
    it('should stop automatic discovery', async () => {
      jest.useFakeTimers();

      (global.fetch as jest.MockedFunction<typeof fetch>).mockResolvedValue({
        ok: true,
        json: async () => ({ modules: [] }),
      } as Response);

      await discovery.startDiscovery();
      discovery.stopDiscovery();

      // Fast-forward time
      jest.advanceTimersByTime(120000);

      // Should only have the initial call, not polling calls
      expect(fetch).toHaveBeenCalledTimes(1);

      jest.useRealTimers();
    });
  });
});