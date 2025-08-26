const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const yaml = require('js-yaml');

describe('Docker Compose Configuration', () => {
  const dockerComposePath = path.join(__dirname, '..', 'docker-compose.yml');
  const envExamplePath = path.join(__dirname, '..', '.env.example');
  
  test('docker-compose.yml should exist', () => {
    expect(fs.existsSync(dockerComposePath)).toBe(true);
  });

  test('.env.example should exist', () => {
    expect(fs.existsSync(envExamplePath)).toBe(true);
  });

  describe('Docker Compose Structure', () => {
    let dockerConfig;

    beforeAll(() => {
      if (fs.existsSync(dockerComposePath)) {
        const fileContents = fs.readFileSync(dockerComposePath, 'utf8');
        dockerConfig = yaml.load(fileContents);
      }
    });

    test('should have required services', () => {
      expect(dockerConfig).toBeDefined();
      expect(dockerConfig.services).toBeDefined();
      expect(dockerConfig.services['postgres-platform']).toBeDefined();
      expect(dockerConfig.services['postgres-auth']).toBeDefined();
      expect(dockerConfig.services['redis']).toBeDefined();
    });

    test('postgres-platform should have correct configuration', () => {
      const service = dockerConfig?.services?.['postgres-platform'];
      expect(service).toBeDefined();
      expect(service.image).toMatch(/postgres:17/);
      expect(service.ports).toContain('5432:5432');
      expect(service.environment).toBeDefined();
      expect(service.volumes).toBeDefined();
      expect(service.healthcheck).toBeDefined();
    });

    test('postgres-auth should have correct configuration', () => {
      const service = dockerConfig?.services?.['postgres-auth'];
      expect(service).toBeDefined();
      expect(service.image).toMatch(/postgres:17/);
      expect(service.ports).toContain('5433:5432');
      expect(service.environment).toBeDefined();
      expect(service.volumes).toBeDefined();
      expect(service.healthcheck).toBeDefined();
    });

    test('redis should have correct configuration', () => {
      const service = dockerConfig?.services?.['redis'];
      expect(service).toBeDefined();
      expect(service.image).toMatch(/redis:7/);
      expect(service.ports).toContain('6379:6379');
      expect(service.healthcheck).toBeDefined();
    });

    test('should have proper network configuration', () => {
      expect(dockerConfig?.networks).toBeDefined();
      expect(dockerConfig.networks['platform-network']).toBeDefined();
    });

    test('should have volume definitions', () => {
      expect(dockerConfig?.volumes).toBeDefined();
      expect(dockerConfig.volumes['postgres-platform-data']).toBeDefined();
      expect(dockerConfig.volumes['postgres-auth-data']).toBeDefined();
      expect(dockerConfig.volumes['redis-data']).toBeDefined();
    });
  });

  describe('Environment Configuration', () => {
    let envContent;

    beforeAll(() => {
      if (fs.existsSync(envExamplePath)) {
        envContent = fs.readFileSync(envExamplePath, 'utf8');
      }
    });

    test('should have database configuration variables', () => {
      expect(envContent).toContain('POSTGRES_PLATFORM_DB');
      expect(envContent).toContain('POSTGRES_PLATFORM_USER');
      expect(envContent).toContain('POSTGRES_PLATFORM_PASSWORD');
      expect(envContent).toContain('POSTGRES_AUTH_DB');
      expect(envContent).toContain('POSTGRES_AUTH_USER');
      expect(envContent).toContain('POSTGRES_AUTH_PASSWORD');
    });

    test('should have Redis configuration variables', () => {
      expect(envContent).toContain('REDIS_HOST');
      expect(envContent).toContain('REDIS_PORT');
    });

    test('should have service URL variables', () => {
      expect(envContent).toContain('AUTH_SERVICE_URL');
      expect(envContent).toContain('PLATFORM_BFF_URL');
      expect(envContent).toContain('FRONTEND_URL');
    });
  });

  describe('Docker Compose Validation', () => {
    test('docker-compose config should validate successfully', () => {
      if (!fs.existsSync(dockerComposePath)) {
        console.log('Skipping validation - docker-compose.yml not yet created');
        return;
      }

      try {
        execSync('docker-compose config', { 
          cwd: path.dirname(dockerComposePath),
          stdio: 'pipe' 
        });
      } catch (error) {
        if (error.message.includes('docker-compose: command not found')) {
          console.log('Docker Compose not installed - skipping validation');
          return;
        }
        throw error;
      }
    });
  });
});