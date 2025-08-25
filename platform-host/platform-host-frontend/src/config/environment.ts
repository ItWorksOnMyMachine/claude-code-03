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
    const nodeEnv = (process.env.NODE_ENV || 'development') as EnvironmentConfig['nodeEnv'];
    
    this.config = {
      // Application
      nodeEnv,
      port: parseInt(process.env.PORT || '3002', 10),
      isDevelopment: nodeEnv === 'development',
      isProduction: nodeEnv === 'production',
      isTest: nodeEnv === 'test',

      // API Configuration
      apiUrl: process.env.API_URL || 'http://localhost:5000',
      apiTimeout: parseInt(process.env.API_TIMEOUT || '30000', 10),

      // Module Federation
      remoteModulesDiscoveryUrl: process.env.REMOTE_MODULES_DISCOVERY_URL || '/api/federation/modules',

      // Asset Configuration
      assetPrefix: process.env.ASSET_PREFIX || '/',

      // Feature Flags
      enableModuleDiscovery: process.env.ENABLE_MODULE_DISCOVERY === 'true',
      enableHealthChecks: process.env.ENABLE_HEALTH_CHECKS === 'true',

      // Logging
      logLevel: (process.env.LOG_LEVEL || 'info') as EnvironmentConfig['logLevel'],
    };
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