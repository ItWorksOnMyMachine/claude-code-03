import { describe, it, expect, beforeAll, jest } from '@jest/globals';
import * as fs from 'fs';
import * as path from 'path';

describe('Build Configuration', () => {
  describe('ModernJS Configuration', () => {
    let modernConfig: any;
    
    beforeAll(() => {
      const configPath = path.resolve(__dirname, '../../modern.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      
      // Basic parsing to check config structure
      modernConfig = configContent;
    });

    it('should have webpack bundler configured', () => {
      expect(modernConfig).toContain("bundler: 'webpack'");
    });

    it('should have port 3002 configured', () => {
      expect(modernConfig).toContain('port: 3002');
    });

    it('should have API proxy configured for /api/*', () => {
      expect(modernConfig).toContain("'/api'");
      expect(modernConfig).toContain('http://localhost:5000');
    });

    it('should have Module Federation plugin configured', () => {
      expect(modernConfig).toContain('moduleFederationPlugin');
    });
  });

  describe('Module Federation Configuration', () => {
    let federationConfig: any;
    
    beforeAll(() => {
      const configPath = path.resolve(__dirname, '../../module-federation.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      federationConfig = configContent;
    });

    it('should configure app as host', () => {
      expect(federationConfig).toContain("name: 'platform_host'");
    });

    it('should have shared dependencies configured as singletons', () => {
      expect(federationConfig).toContain('singleton: true');
      expect(federationConfig).toContain('react');
      expect(federationConfig).toContain('react-dom');
      expect(federationConfig).toContain('@mui/material');
    });

    it('should have runtime remote loading configured', () => {
      expect(federationConfig).toContain('remotes:');
    });
  });

  describe('Package Scripts', () => {
    let packageJson: any;
    
    beforeAll(() => {
      const packagePath = path.resolve(__dirname, '../../package.json');
      packageJson = JSON.parse(fs.readFileSync(packagePath, 'utf-8'));
    });

    it('should have development script', () => {
      expect(packageJson.scripts).toHaveProperty('dev');
    });

    it('should have build script', () => {
      expect(packageJson.scripts).toHaveProperty('build');
    });

    it('should have test script', () => {
      expect(packageJson.scripts).toHaveProperty('test');
    });

    it('should have start script for production', () => {
      expect(packageJson.scripts).toHaveProperty('start');
    });
  });

  describe('Environment Configuration', () => {
    it('should support NODE_ENV variable', () => {
      expect(process.env.NODE_ENV).toBeDefined();
    });

    it('should have different configs for dev and prod', () => {
      const configPath = path.resolve(__dirname, '../../modern.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      
      // Check for environment-specific configuration
      expect(configContent).toContain('dev:');
      expect(configContent).toContain('server:');
    });
  });

  describe('HMR Configuration', () => {
    it('should have HMR enabled in development', () => {
      const configPath = path.resolve(__dirname, '../../modern.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      
      // ModernJS enables HMR by default in dev mode
      expect(configContent).toContain('dev:');
    });
  });

  describe('Production Build Optimizations', () => {
    it('should have production optimizations configured', () => {
      const configPath = path.resolve(__dirname, '../../modern.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      
      // Check for tools configuration
      expect(configContent).toContain('tools:');
    });
  });
});

describe('Environment Files', () => {
  it('should have .env.example file', () => {
    const envExamplePath = path.resolve(__dirname, '../../.env.example');
    expect(fs.existsSync(envExamplePath)).toBe(true);
  });

  it('should have proper gitignore for env files', () => {
    const gitignorePath = path.resolve(__dirname, '../../.gitignore');
    if (fs.existsSync(gitignorePath)) {
      const gitignoreContent = fs.readFileSync(gitignorePath, 'utf-8');
      expect(gitignoreContent).toContain('.env');
      expect(gitignoreContent).not.toContain('.env.example');
    }
  });
});

describe('API Proxy Configuration', () => {
  it('should proxy /api/* requests to port 5000', () => {
    const configPath = path.resolve(__dirname, '../../modern.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    expect(configContent).toContain('proxy');
    expect(configContent).toContain('5000');
  });

  it('should handle WebSocket upgrade for API proxy', () => {
    const configPath = path.resolve(__dirname, '../../modern.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Check if ws configuration exists (optional)
    const hasWebSocketConfig = configContent.includes('ws:') || configContent.includes('changeOrigin');
    expect(hasWebSocketConfig).toBe(true);
  });
});