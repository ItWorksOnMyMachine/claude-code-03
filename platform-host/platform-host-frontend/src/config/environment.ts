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
    // Default to development mode for now - will be properly configured later
    const nodeEnv = 'development' as EnvironmentConfig['nodeEnv'];
    
    this.config = {
      // Application
      nodeEnv,
      port: 3004,
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