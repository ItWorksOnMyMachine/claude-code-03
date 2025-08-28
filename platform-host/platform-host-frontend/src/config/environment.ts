/**
 * Environment configuration service
 * Provides type-safe access to environment variables
 */

export interface EnvironmentConfig {
  // Application
  nodeEnv: 'development' | 'production' | 'test';
  port: number;
  isDevelopment: boolean;
  isProduction: boolean;
  isTest: boolean;

  // API Configuration
  apiUrl: string;
  apiTimeout: number;

  // Module Federation
  remoteModulesDiscoveryUrl: string;

  // Asset Configuration
  assetPrefix: string;

  // Feature Flags
  enableModuleDiscovery: boolean;
  enableHealthChecks: boolean;

  // Logging
  logLevel: 'debug' | 'info' | 'warn' | 'error';
}

class Environment {
  private config: EnvironmentConfig;

  constructor() {
    // Check if we're in test environment by looking for global process object
    // This is safe because Jest provides process globally
    const isJest = typeof process !== 'undefined' && process.env && process.env.NODE_ENV;
    const nodeEnv = (isJest ? process.env.NODE_ENV : 'development') as EnvironmentConfig['nodeEnv'];
    
    if (isJest && typeof process !== 'undefined' && process.env) {
      // Test environment - read from process.env
      const env = process.env;
      this.config = {
        // Application
        nodeEnv: (env.NODE_ENV || 'test') as EnvironmentConfig['nodeEnv'],
        port: parseInt(env.PORT || '3002', 10),
        isDevelopment: env.NODE_ENV === 'development',
        isProduction: env.NODE_ENV === 'production',
        isTest: env.NODE_ENV === 'test',

        // API Configuration
        apiUrl: env.API_URL || (env.NODE_ENV === 'production' ? '/api' : 'http://localhost:5000'),
        apiTimeout: parseInt(env.API_TIMEOUT || '30000', 10),

        // Module Federation
        remoteModulesDiscoveryUrl: env.REMOTE_MODULES_DISCOVERY_URL || '/api/federation/modules',

        // Asset Configuration
        assetPrefix: env.ASSET_PREFIX || '/',

        // Feature Flags
        enableModuleDiscovery: env.ENABLE_MODULE_DISCOVERY === 'true',
        enableHealthChecks: env.ENABLE_HEALTH_CHECKS === 'true',

        // Logging
        logLevel: (env.LOG_LEVEL || 'info') as EnvironmentConfig['logLevel'],
      };
    } else {
      // Runtime environment - use hardcoded values
      this.config = {
        // Application
        nodeEnv: 'development',
        port: 3002,
        isDevelopment: true,
        isProduction: false,
        isTest: false,

        // API Configuration
        apiUrl: '/api',
        apiTimeout: 30000,

        // Module Federation
        remoteModulesDiscoveryUrl: '/api/federation/modules',

        // Asset Configuration
        assetPrefix: '/',

        // Feature Flags
        enableModuleDiscovery: false,
        enableHealthChecks: false,

        // Logging
        logLevel: 'info',
      };
    }
  }

  public get(): EnvironmentConfig {
    return this.config;
  }

  public getValue<K extends keyof EnvironmentConfig>(key: K): EnvironmentConfig[K] {
    return this.config[key];
  }

  public isFeatureEnabled(feature: string): boolean {
    const featureKey = `enable${feature.charAt(0).toUpperCase()}${feature.slice(1)}` as keyof EnvironmentConfig;
    const value = this.config[featureKey];
    return typeof value === 'boolean' ? value : false;
  }
}

// Export singleton instance
export const environment = new Environment();
export default environment;