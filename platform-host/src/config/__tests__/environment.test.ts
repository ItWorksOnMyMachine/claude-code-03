import { describe, it, expect, beforeEach, afterEach } from '@jest/globals';

describe('Environment Configuration', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    // Reset modules to get fresh instance
    jest.resetModules();
    // Create a shallow copy of process.env
    process.env = { ...originalEnv };
  });

  afterEach(() => {
    // Restore original env
    process.env = originalEnv;
  });

  it('should provide default development configuration', () => {
    process.env.NODE_ENV = 'development';
    
    const { environment } = require('../environment');
    const config = environment.get();
    
    expect(config.nodeEnv).toBe('development');
    expect(config.isDevelopment).toBe(true);
    expect(config.isProduction).toBe(false);
    expect(config.port).toBe(3002);
    expect(config.apiUrl).toBe('http://localhost:5000');
  });

  it('should read production configuration', () => {
    process.env.NODE_ENV = 'production';
    process.env.PORT = '8080';
    process.env.API_URL = 'https://api.example.com';
    
    const { environment } = require('../environment');
    const config = environment.get();
    
    expect(config.nodeEnv).toBe('production');
    expect(config.isProduction).toBe(true);
    expect(config.isDevelopment).toBe(false);
    expect(config.port).toBe(8080);
    expect(config.apiUrl).toBe('https://api.example.com');
  });

  it('should handle feature flags correctly', () => {
    process.env.ENABLE_MODULE_DISCOVERY = 'true';
    process.env.ENABLE_HEALTH_CHECKS = 'false';
    
    const { environment } = require('../environment');
    const config = environment.get();
    
    expect(config.enableModuleDiscovery).toBe(true);
    expect(config.enableHealthChecks).toBe(false);
  });

  it('should provide getValue method for specific keys', () => {
    process.env.LOG_LEVEL = 'debug';
    
    const { environment } = require('../environment');
    
    expect(environment.getValue('logLevel')).toBe('debug');
    expect(environment.getValue('nodeEnv')).toBe('test'); // In test environment
  });

  it('should check feature flags with isFeatureEnabled', () => {
    process.env.ENABLE_MODULE_DISCOVERY = 'true';
    process.env.ENABLE_HEALTH_CHECKS = 'false';
    
    const { environment } = require('../environment');
    
    expect(environment.isFeatureEnabled('moduleDiscovery')).toBe(true);
    expect(environment.isFeatureEnabled('healthChecks')).toBe(false);
    expect(environment.isFeatureEnabled('nonExistent')).toBe(false);
  });

  it('should handle test environment', () => {
    process.env.NODE_ENV = 'test';
    
    const { environment } = require('../environment');
    const config = environment.get();
    
    expect(config.nodeEnv).toBe('test');
    expect(config.isTest).toBe(true);
    expect(config.isDevelopment).toBe(false);
    expect(config.isProduction).toBe(false);
  });
});