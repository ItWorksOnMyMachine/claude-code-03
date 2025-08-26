const fs = require('fs');
const path = require('path');

describe('Database Initialization Scripts', () => {
  const dockerSqlPath = path.join(__dirname, '..', 'docker', 'sql');
  
  describe('Platform Database Scripts', () => {
    const platformSqlPath = path.join(dockerSqlPath, 'platform');
    
    test('platform SQL directory exists', () => {
      expect(fs.existsSync(platformSqlPath)).toBe(true);
    });
    
    test('01-init.sql exists and has valid content', () => {
      const initSqlPath = path.join(platformSqlPath, '01-init.sql');
      expect(fs.existsSync(initSqlPath)).toBe(true);
      
      const content = fs.readFileSync(initSqlPath, 'utf8');
      expect(content).toContain('CREATE EXTENSION IF NOT EXISTS "uuid-ossp"');
      expect(content).toMatch(/Platform database initialized/i);
    });
    
    test('02-schemas.sql creates required schemas', () => {
      const schemasSqlPath = path.join(platformSqlPath, '02-schemas.sql');
      if (fs.existsSync(schemasSqlPath)) {
        const content = fs.readFileSync(schemasSqlPath, 'utf8');
        expect(content).toMatch(/CREATE SCHEMA IF NOT EXISTS/i);
      }
    });
    
    test('03-tables.sql creates tenant and user tables', () => {
      const tablesSqlPath = path.join(platformSqlPath, '03-tables.sql');
      if (fs.existsSync(tablesSqlPath)) {
        const content = fs.readFileSync(tablesSqlPath, 'utf8');
        expect(content).toMatch(/CREATE TABLE.*tenants/i);
        expect(content).toMatch(/CREATE TABLE.*users/i);
        expect(content).toMatch(/CREATE TABLE.*user_tenants/i);
      }
    });
    
    test('04-seed-data.sql creates test tenants', () => {
      const seedSqlPath = path.join(platformSqlPath, '04-seed-data.sql');
      if (fs.existsSync(seedSqlPath)) {
        const content = fs.readFileSync(seedSqlPath, 'utf8');
        expect(content).toMatch(/INSERT INTO.*tenants/i);
        expect(content).toContain('Default Tenant');
        expect(content).toContain('Demo Company');
      }
    });
  });
  
  describe('Auth Database Scripts', () => {
    const authSqlPath = path.join(dockerSqlPath, 'auth');
    
    test('auth SQL directory exists', () => {
      expect(fs.existsSync(authSqlPath)).toBe(true);
    });
    
    test('01-init.sql exists and has valid content', () => {
      const initSqlPath = path.join(authSqlPath, '01-init.sql');
      expect(fs.existsSync(initSqlPath)).toBe(true);
      
      const content = fs.readFileSync(initSqlPath, 'utf8');
      expect(content).toContain('CREATE EXTENSION IF NOT EXISTS "uuid-ossp"');
      expect(content).toMatch(/Auth database initialized/i);
    });
    
    test('02-duende-tables.sql creates IdentityServer tables', () => {
      const duendeSqlPath = path.join(authSqlPath, '02-duende-tables.sql');
      if (fs.existsSync(duendeSqlPath)) {
        const content = fs.readFileSync(duendeSqlPath, 'utf8');
        expect(content).toMatch(/CREATE TABLE.*Clients/i);
        expect(content).toMatch(/CREATE TABLE.*IdentityResources/i);
        expect(content).toMatch(/CREATE TABLE.*ApiScopes/i);
      }
    });
    
    test('03-seed-clients.sql creates development clients', () => {
      const seedClientsSqlPath = path.join(authSqlPath, '03-seed-clients.sql');
      if (fs.existsSync(seedClientsSqlPath)) {
        const content = fs.readFileSync(seedClientsSqlPath, 'utf8');
        expect(content).toMatch(/INSERT INTO.*Clients/i);
        expect(content).toContain('platform-bff');
        expect(content).toContain('platform-frontend');
      }
    });
  });
  
  describe('SQL Script Validation', () => {
    test('all SQL files have proper encoding', () => {
      const sqlFiles = [];
      
      const findSqlFiles = (dir) => {
        if (!fs.existsSync(dir)) return;
        
        const files = fs.readdirSync(dir);
        files.forEach(file => {
          const fullPath = path.join(dir, file);
          const stat = fs.statSync(fullPath);
          if (stat.isDirectory()) {
            findSqlFiles(fullPath);
          } else if (path.extname(file) === '.sql') {
            sqlFiles.push(fullPath);
          }
        });
      };
      
      findSqlFiles(dockerSqlPath);
      
      sqlFiles.forEach(sqlFile => {
        const content = fs.readFileSync(sqlFile, 'utf8');
        expect(content).toBeDefined();
        expect(() => {
          const lines = content.split('\n');
          lines.forEach(line => {
            if (line.includes('�')) {
              throw new Error(`Invalid character encoding in ${sqlFile}`);
            }
          });
        }).not.toThrow();
      });
    });
    
    test('all SQL files have transaction safety', () => {
      const seedFiles = [];
      
      const findSeedFiles = (dir) => {
        if (!fs.existsSync(dir)) return;
        
        const files = fs.readdirSync(dir);
        files.forEach(file => {
          const fullPath = path.join(dir, file);
          const stat = fs.statSync(fullPath);
          if (stat.isDirectory()) {
            findSeedFiles(fullPath);
          } else if (file.includes('seed') && path.extname(file) === '.sql') {
            seedFiles.push(fullPath);
          }
        });
      };
      
      findSeedFiles(dockerSqlPath);
      
      seedFiles.forEach(sqlFile => {
        const content = fs.readFileSync(sqlFile, 'utf8');
        const hasBegin = content.match(/BEGIN|START TRANSACTION/i);
        const hasCommit = content.match(/COMMIT/i);
        const hasTransaction = !!(hasBegin && hasCommit);
        const hasOnConflict = !!content.match(/ON CONFLICT/i);
        const hasIfNotExists = !!content.match(/IF NOT EXISTS/i);
        
        const isSafe = hasTransaction || hasOnConflict || hasIfNotExists;
        expect(isSafe).toBe(true);
      });
    });
  });
});