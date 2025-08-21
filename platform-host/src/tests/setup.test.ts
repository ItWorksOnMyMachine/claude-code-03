import { describe, it, expect } from '@jest/globals';
import fs from 'fs';
import path from 'path';

describe('Platform Host - Project Setup Validation', () => {
  const rootDir = path.resolve(__dirname, '../..');

  describe('Project Structure', () => {
    it('should have required configuration files', () => {
      expect(fs.existsSync(path.join(rootDir, 'package.json'))).toBe(true);
      expect(fs.existsSync(path.join(rootDir, 'tsconfig.json'))).toBe(true);
      expect(fs.existsSync(path.join(rootDir, 'modern.config.ts'))).toBe(true);
      expect(fs.existsSync(path.join(rootDir, 'biome.json'))).toBe(true);
    });

    it('should have source directory structure', () => {
      expect(fs.existsSync(path.join(rootDir, 'src'))).toBe(true);
      expect(fs.existsSync(path.join(rootDir, 'src/routes'))).toBe(true);
      expect(fs.existsSync(path.join(rootDir, 'src/routes/layout.tsx'))).toBe(true);
      expect(fs.existsSync(path.join(rootDir, 'src/routes/page.tsx'))).toBe(true);
    });
  });

  describe('Dependencies', () => {
    let packageJson: any;

    beforeAll(() => {
      const packagePath = path.join(rootDir, 'package.json');
      packageJson = JSON.parse(fs.readFileSync(packagePath, 'utf-8'));
    });

    it('should have core ModernJS dependencies', () => {
      expect(packageJson.dependencies['@modern-js/runtime']).toBeDefined();
      expect(packageJson.dependencies['react']).toBeDefined();
      expect(packageJson.dependencies['react-dom']).toBeDefined();
    });

    it('should have Module Federation dependencies', () => {
      expect(packageJson.dependencies['@module-federation/enhanced']).toBeDefined();
    });

    it('should have UI framework dependencies', () => {
      expect(packageJson.dependencies['@mui/material']).toBeDefined();
      expect(packageJson.dependencies['@emotion/react']).toBeDefined();
      expect(packageJson.dependencies['@emotion/styled']).toBeDefined();
      expect(packageJson.dependencies['lucide-react']).toBeDefined();
    });

    it('should have state management dependencies', () => {
      expect(packageJson.dependencies['@tanstack/react-query']).toBeDefined();
    });

    it('should have testing dependencies', () => {
      expect(packageJson.devDependencies['@testing-library/react']).toBeDefined();
      expect(packageJson.devDependencies['@testing-library/jest-dom']).toBeDefined();
      expect(packageJson.devDependencies['@jest/globals']).toBeDefined();
    });
  });

  describe('Configuration', () => {
    it('should be configured to run on port 3002', () => {
      const configPath = path.join(rootDir, 'modern.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      
      expect(configContent).toContain('port: 3002');
      expect(configContent).toContain('server:');
      expect(configContent).toContain('dev:');
    });

    it('should have API proxy configuration', () => {
      const configPath = path.join(rootDir, 'modern.config.ts');
      const configContent = fs.readFileSync(configPath, 'utf-8');
      
      expect(configContent).toContain("'/api':");
      expect(configContent).toContain('target: \'http://localhost:5000\'');
      expect(configContent).toContain('changeOrigin: true');
    });

    it('should have TypeScript path aliases configured', () => {
      const tsconfigPath = path.join(rootDir, 'tsconfig.json');
      const tsconfig = JSON.parse(fs.readFileSync(tsconfigPath, 'utf-8'));
      
      expect(tsconfig.compilerOptions.paths).toBeDefined();
      expect(tsconfig.compilerOptions.paths['@/*']).toEqual(['./src/*']);
    });
  });
});