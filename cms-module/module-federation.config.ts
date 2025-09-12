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
    '@emotion/react': false,
    '@emotion/styled': false,
    // GrapesJS dependencies are not shared to avoid version conflicts
    'grapesjs': false,
    'grapesjs-react': false,
    'grapesjs-preset-webpage': false,
    'grapesjs-plugin-forms': false,
    'react-dropzone': false,
    'dompurify': false,
  },
});