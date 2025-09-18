/// <reference types="@module-federation/enhanced" />

declare module '@module-federation/enhanced/runtime' {
  export interface LoadRemoteOptions {
    name: string;
    entry: string;
    exposedModule: string;
  }

  export interface RemoteInfo {
    name: string;
    entry: string;
  }

  export function loadRemote(id: string): Promise<any>;
  export function loadRemote(options: LoadRemoteOptions): Promise<any>;
  export function init(options?: any): void;
  export function registerRemotes(remotes: RemoteInfo[]): void;
}

declare module '@module-federation/modern-js' {
  export interface ModuleFederationConfig {
    name: string;
    filename?: string;
    exposes?: Record<string, string>;
    remotes?: Record<string, string>;
    shared?: Record<string, any>;
    runtimePlugins?: string[];
  }

  export function createModuleFederationConfig(
    config: ModuleFederationConfig
  ): ModuleFederationConfig;

  export function moduleFederationPlugin(
    options?: ModuleFederationConfig
  ): any;
}

// Remote module type definitions
declare module 'cmsModule/CmsApp' {
  import React from 'react';

  interface CmsAppProps {
    authToken?: string;
    tenantId?: string;
    userId?: string;
    currentPath?: string;
    onNavigate?: (path: string) => void;
  }

  const CmsApp: React.FC<CmsAppProps>;
  export default CmsApp;
}

declare module 'cmsModule/CmsRouter' {
  import React from 'react';

  interface CmsRouterProps {
    authToken?: string;
    tenantId?: string;
    userId?: string;
  }

  const CmsRouter: React.FC<CmsRouterProps>;
  export default CmsRouter;
}

declare module 'cms/App' {
  const CMSApp: React.ComponentType;
  export default CMSApp;
}

declare module 'forms/App' {
  const FormsApp: React.ComponentType;
  export default FormsApp;
}

// Generic remote module type
declare module '*/App' {
  const RemoteApp: React.ComponentType;
  export default RemoteApp;
}

// Window augmentation for Module Federation runtime
declare global {
  interface Window {
    __FEDERATION__: {
      __INSTANCES__: Map<string, any>;
      __SHARE__: Map<string, any>;
      __MANIFEST__: any;
    };
    __webpack_init_sharing__: (scope: string) => Promise<void>;
    __webpack_share_scopes__: Record<string, any>;
  }

  // Augment WindowEventMap to include custom module-message event
  interface WindowEventMap {
    'module-message': CustomEvent<import('../services/ModuleCommunication').ModuleMessage>;
  }
}

export {};