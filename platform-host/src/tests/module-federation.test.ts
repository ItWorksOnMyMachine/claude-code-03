import { describe, it, expect, beforeAll } from '@jest/globals';
import fs from 'fs';
import path from 'path';

describe('Module Federation Configuration', () => {
  const rootDir = path.resolve(__dirname, '../..');
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

    it('should configure Emotion packages as shared dependencies', () => {
      expect(mfConfig).toContain("'@emotion/react': {");
      expect(mfConfig).toContain("'@emotion/styled': {");
    });
  });

  describe('Remote Module Configuration', () => {
    it('should support dynamic remote module loading', () => {
      const remoteLoaderPath = path.join(rootDir, 'src/services/RemoteLoader.ts');
      expect(fs.existsSync(remoteLoaderPath)).toBe(true);
      
      const remoteLoader = fs.readFileSync(remoteLoaderPath, 'utf-8');
      expect(remoteLoader).toContain('loadRemote');
      expect(remoteLoader).toContain('RemoteModuleConfig');
    });

    it('should have TypeScript definitions for federated modules', () => {
      const typeDefsPath = path.join(rootDir, 'src/types/module-federation.d.ts');
      expect(fs.existsSync(typeDefsPath)).toBe(true);
      
      const typeDefs = fs.readFileSync(typeDefsPath, 'utf-8');
      expect(typeDefs).toContain('@module-federation/enhanced');
    });

    it('should have ModuleFederationContext for state management', () => {
      const contextPath = path.join(rootDir, 'src/contexts/ModuleFederationContext.tsx');
      expect(fs.existsSync(contextPath)).toBe(true);
      
      const context = fs.readFileSync(contextPath, 'utf-8');
      expect(context).toContain('ModuleFederationProvider');
      expect(context).toContain('useModuleFederation');
    });
  });
});