const baseConfig = require('./jest.config.js');

module.exports = {
  ...baseConfig,
  testEnvironment: 'node',
  setupFilesAfterEnv: [], // Remove setup file that expects DOM globals
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    // Remove DOM-specific mocks for Node environment
  },
  testMatch: [
    '<rootDir>/src/tests/node-only/**/*.{js,jsx,ts,tsx}'
  ],
  testPathIgnorePatterns: [], // Override the ignore patterns from base config
};