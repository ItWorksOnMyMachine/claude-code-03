/// <reference types="@module-federation/enhanced" />

declare module '@module-federation/enhanced/runtime' {
  export interface LoadRemoteOptions {
    name: string;
    entry: string;
    exposedModule: string;
  }

  export function loadRemote(options: LoadRemoteOptions): Promise<any>;
  export function init(options?: any): void;
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
}

export {};