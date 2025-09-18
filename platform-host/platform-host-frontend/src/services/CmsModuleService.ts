import type { ModuleInfo } from '@/contexts/ModuleFederationContext';
import React from 'react';

/**
 * Service for managing CMS Module integration with the platform host
 */
export class CmsModuleService {
  private static instance: CmsModuleService;
  private cmsModuleInfo: ModuleInfo;

  private constructor() {
    this.cmsModuleInfo = {
      name: 'cmsModule',
      displayName: 'Content Management System',
      entry: 'https://cms-fe.platform.local:3003/remoteEntry.js',
      exposedModule: './CmsApp',
      route: '/cms',
      icon: 'EditNote', // Material-UI icon name
      enabled: true,
    };
  }

  static getInstance(): CmsModuleService {
    if (!CmsModuleService.instance) {
      CmsModuleService.instance = new CmsModuleService();
    }
    return CmsModuleService.instance;
  }

  /**
   * Get the CMS module configuration
   */
  getModuleInfo(): ModuleInfo {
    return { ...this.cmsModuleInfo };
  }

  /**
   * Load the CMS module with enhanced error handling
   */
  async loadCmsModule() {
    try {
      console.log('Loading CMS module directly via dynamic import');

      // Use dynamic import for the pre-configured remote
      // @ts-ignore - TypeScript doesn't know about the remote module
      const module = await import('cmsModule/CmsApp');

      console.log('CMS Module loaded successfully:', module);

      return {
        name: this.cmsModuleInfo.name,
        module: module,
        error: undefined,
      };
    } catch (error) {
      console.error('CmsModuleService: Failed to load CMS module', error);
      throw error;
    }
  }

  /**
   * Load the CMS Router component specifically
   */
  async loadCmsRouter() {
    try {
      console.log('Loading CMS Router directly via dynamic import');

      // Use dynamic import for the pre-configured remote
      // @ts-ignore - TypeScript doesn't know about the remote module
      const module = await import('cmsModule/CmsRouter');

      console.log('CMS Router loaded successfully:', module);

      return {
        name: this.cmsModuleInfo.name,
        module: module,
        error: undefined,
      };
    } catch (error) {
      console.error('CmsModuleService: Failed to load CMS router', error);
      throw error;
    }
  }

  /**
   * Check if the CMS module is available
   */
  async checkCmsModuleHealth(): Promise<boolean> {
    try {
      const response = await fetch(`${this.getBaseUrl()}/remoteEntry.js`, {
        method: 'HEAD',
        mode: 'no-cors',
      });

      // In no-cors mode, we can't read the status, but if it doesn't throw, it's likely available
      return true;
    } catch (error) {
      console.warn('CMS module health check failed:', error);
      return false;
    }
  }

  /**
   * Get the base URL for the CMS module
   */
  getBaseUrl(): string {
    const url = new URL(this.cmsModuleInfo.entry);
    return `${url.protocol}//${url.host}`;
  }

  /**
   * Create a React component that loads the CMS module with error boundary
   */
  createCmsModuleComponent() {
    return React.lazy(async () => {
      try {
        const loaded = await this.loadCmsModule();
        if (!loaded.module) {
          throw new Error('Failed to load CMS module');
        }
        return {
          default: loaded.module.default || loaded.module,
        };
      } catch (error) {
        console.error('Error in lazy CMS component:', error);
        // Return a fallback component
        return {
          default: () =>
            React.createElement(
              'div',
              {
                style: {
                  padding: '20px',
                  border: '1px solid #f5c6cb',
                  borderRadius: '4px',
                  backgroundColor: '#f8d7da',
                  color: '#721c24',
                  margin: '10px',
                },
              },
              'CMS Module failed to load. Please refresh the page.',
            ),
        };
      }
    });
  }

  /**
   * Update module configuration (e.g., for different environments)
   */
  updateModuleConfig(overrides: Partial<ModuleInfo>) {
    this.cmsModuleInfo = {
      ...this.cmsModuleInfo,
      ...overrides,
    };
  }
}

// Export singleton instance
export const cmsModuleService = CmsModuleService.getInstance();
export default cmsModuleService;
