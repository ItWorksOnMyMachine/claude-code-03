export default {
  preset: 'ts-jest/presets/default-esm',
  extensionsToTreatAsEsm: ['.ts', '.tsx'],
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/src/__tests__/setup.ts'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@modern-js/runtime/router$': '<rootDir>/src/__mocks__/@modern-js/runtime/router.ts',
    '^grapesjs$': '<rootDir>/src/__mocks__/grapesjs.ts',
    '^grapesjs-react$': '<rootDir>/src/__mocks__/grapesjs-react.ts',
    '^../../module-federation.config$': '<rootDir>/src/__mocks__/module-federation.config.ts',
    '\.(css|scss|sass)$': 'identity-obj-proxy',
  },
  testMatch: [
    '<rootDir>/src/**/__tests__/**/*.{test,spec}.{ts,tsx}',
    '<rootDir>/src/**/*.{test,spec}.{ts,tsx}',
  ],
  testPathIgnorePatterns: [
    '<rootDir>/src/__tests__/setup.ts',
  ],
  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    '!src/**/*.d.ts',
    '!src/__tests__/**',
    '!src/modern-app-env.d.ts',
    '!src/modern.runtime.ts',
    '!src/__mocks__/**',
  ],
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json'],
  transform: {
    '^.+\.(ts|tsx)$': ['ts-jest', {
      useESM: true,
      tsconfig: {
        module: 'esnext',
        target: 'es2022',
        moduleResolution: 'node',
        allowSyntheticDefaultImports: true,
        esModuleInterop: true,
      }
    }],
  },
  transformIgnorePatterns: [
    'node_modules/(?!(@modern-js|@module-federation|@emotion|@mui|grapesjs)/)',
  ],
  moduleDirectories: ['node_modules', '<rootDir>/src'],
  testTimeout: 10000,
};
