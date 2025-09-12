import { beforeAll, describe, expect, it } from '@jest/globals';
import fs from 'fs';
import path from 'path';
import mfConfig from '../../../module-federation.config';

describe('Module Federation shared config guard', () => {
  const shared = (mfConfig as any).shared || {};

  it('does not share Emotion or MUI styled-engine', () => {
    expect(shared['@emotion/react']).toBeUndefined();
    expect(shared['@emotion/styled']).toBeUndefined();
    expect(shared['@mui/styled-engine']).toBeUndefined();
  });

  it('pins versions for MUI packages', () => {
    for (const pkg of ['@mui/material', '@mui/system']) {
      expect(shared[pkg]).toBeDefined();
      const v = shared[pkg].version ?? shared[pkg].requiredVersion;
      expect(typeof v).toBe('string');
      expect(v.length).toBeGreaterThan(0);
    }
  });
});

describe('Module Federation Configuration', () => {
  const rootDir = path.resolve(__dirname, '../../..');
  let modernConfig: string;

  beforeAll(() => {
    const configPath = path.join(rootDir, 'modern.config.ts');
    modernConfig = fs.readFileSync(configPath, 'utf-8');
  });

  describe('Module Federation Plugin', () => {
    it('should have Module Federation plugin configured', () => {
      expect(modernConfig).toContain('@module-federation/modern-js');
      expect(modernConfig).toContain('moduleFederationPlugin');
    });

    it('should be configured as host application', () => {
      const mfConfigPath = path.join(rootDir, 'module-federation.config.ts');
      expect(fs.existsSync(mfConfigPath)).toBe(true);

      const mfConfig = fs.readFileSync(mfConfigPath, 'utf-8');
      expect(mfConfig).toContain("name: 'platform_host'");
    });
  });

  describe('Shared Dependencies', () => {
    let mfConfig: string;

    beforeAll(() => {
      const mfConfigPath = path.join(rootDir, 'module-federation.config.ts');
      mfConfig = fs.readFileSync(mfConfigPath, 'utf-8');
    });

    it('should configure React as singleton shared dependency', () => {
      expect(mfConfig).toContain('react: {');
      expect(mfConfig).toContain('singleton: true');
    });

    it('should configure React-DOM as singleton shared dependency', () => {
      expect(mfConfig).toContain("'react-dom': {");
      expect(mfConfig).toContain('singleton: true');
    });

    it('should configure MUI packages as shared dependencies', () => {
      expect(mfConfig).toContain("'@mui/material': {");
      expect(mfConfig).toContain('singleton: true');
    });

  });

  describe('Remote Module Configuration', () => {
    it('should support dynamic remote module loading', () => {
      const remoteLoaderPath = path.join(
        rootDir,
        'src/services/RemoteLoader.ts',
      );
      expect(fs.existsSync(remoteLoaderPath)).toBe(true);

      const remoteLoader = fs.readFileSync(remoteLoaderPath, 'utf-8');
      expect(remoteLoader).toContain('loadRemote');
      expect(remoteLoader).toContain('RemoteModuleConfig');
    });

    it('should have TypeScript definitions for federated modules', () => {
      const typeDefsPath = path.join(
        rootDir,
        'src/types/module-federation.d.ts',
      );
      expect(fs.existsSync(typeDefsPath)).toBe(true);

      const typeDefs = fs.readFileSync(typeDefsPath, 'utf-8');
      expect(typeDefs).toContain('@module-federation/enhanced');
    });

    it('should have ModuleFederationContext for state management', () => {
      const contextPath = path.join(
        rootDir,
        'src/contexts/ModuleFederationContext.tsx',
      );
      expect(fs.existsSync(contextPath)).toBe(true);

      const context = fs.readFileSync(contextPath, 'utf-8');
      expect(context).toContain('ModuleFederationProvider');
      expect(context).toContain('useModuleFederation');
    });
  });

  describe('CMS Module Federation Support', () => {
    it('should support CMS module configuration', () => {
      // Test that platform host can handle CMS module remote loading
      const remoteLoaderPath = path.join(
        rootDir,
        'src/services/RemoteLoader.ts',
      );
      const remoteLoader = fs.readFileSync(remoteLoaderPath, 'utf-8');
      
      // Verify RemoteModuleConfig interface supports CMS module structure
      expect(remoteLoader).toContain('interface RemoteModuleConfig');
      expect(remoteLoader).toContain('name: string');
      expect(remoteLoader).toContain('entry: string');
      expect(remoteLoader).toContain('exposedModule: string');
    });

    it('should handle CMS module loading errors gracefully', () => {
      const remoteLoaderPath = path.join(
        rootDir,
        'src/services/RemoteLoader.ts',
      );
      const remoteLoader = fs.readFileSync(remoteLoaderPath, 'utf-8');
      
      // Verify error handling is implemented
      expect(remoteLoader).toContain('catch (error)');
      expect(remoteLoader).toContain('error: error as Error');
    });

    it('should support parallel loading of multiple modules including CMS', () => {
      const remoteLoaderPath = path.join(
        rootDir,
        'src/services/RemoteLoader.ts',
      );
      const remoteLoader = fs.readFileSync(remoteLoaderPath, 'utf-8');
      
      // Verify loadModules function exists for parallel loading
      expect(remoteLoader).toContain('loadModules');
      expect(remoteLoader).toContain('Promise.all(promises)');
    });

    it('should support caching for CMS module to prevent duplicate loads', () => {
      const remoteLoaderPath = path.join(
        rootDir,
        'src/services/RemoteLoader.ts',
      );
      const remoteLoader = fs.readFileSync(remoteLoaderPath, 'utf-8');
      
      // Verify module caching implementation
      expect(remoteLoader).toContain('loadedModules');
      expect(remoteLoader).toContain('has(cacheKey)');
      expect(remoteLoader).toContain('get(cacheKey)');
    });
  });

  describe('CMS Module Routing Integration', () => {
    it('should support dynamic routing for CMS module', () => {
      // Check if routing configuration supports dynamic module routes
      const routesDir = path.join(rootDir, 'src/routes');
      expect(fs.existsSync(routesDir)).toBe(true);
      
      // Verify that routing system can handle federated modules
      const routeFiles = fs.readdirSync(routesDir);
      const hasRouting = routeFiles.some(file => 
        file.includes('route') || file.includes('Router') || file.includes('index')
      );
      expect(hasRouting).toBe(true);
    });
  });
});
