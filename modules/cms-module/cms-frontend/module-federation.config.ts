import { createModuleFederationConfig } from '@module-federation/modern-js';
import pkg from './package.json' assert { type: 'json' };

export default createModuleFederationConfig({
  name: 'cmsModule',
  filename: 'remoteEntry.js',
  exposes: {
    './CmsApp': './src/CmsApp',
    './CmsRouter': './src/CmsRouter',
  },
  shared: {
    react: {
      singleton: true,
      eager: true,
      requiredVersion: pkg.dependencies.react,
    },
    'react-dom': {
      singleton: true,
      eager: true,
      requiredVersion: pkg.dependencies['react-dom'],
    },
    '@mui/material': {
      singleton: true,
      eager: true,
      version: pkg.dependencies['@mui/material'],
      requiredVersion: pkg.dependencies['@mui/material'],
    },
    '@mui/system': {
      singleton: true,
      eager: true,
      version: pkg.dependencies['@mui/system'],
      requiredVersion: pkg.dependencies['@mui/system'],
    },
    '@mui/icons-material': {
      singleton: true,
      eager: true,
      version: pkg.dependencies['@mui/icons-material'],
      requiredVersion: pkg.dependencies['@mui/icons-material'],
    },
    '@tanstack/react-query': {
      singleton: true,
      requiredVersion: pkg.dependencies['@tanstack/react-query'],
    },
    '@modern-js/runtime': {
      singleton: true,
      requiredVersion: pkg.dependencies['@modern-js/runtime'],
    },
    // Emotion dependencies are NOT shared to prevent styling conflicts
    // GrapesJS dependencies are not shared to avoid version conflicts
    // These are commented out instead of set to false as Module Federation doesn't support false values
  },
});