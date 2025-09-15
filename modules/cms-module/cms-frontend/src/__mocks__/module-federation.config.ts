// Mock module federation config for tests
export default {
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
    },
    'react-dom': {
      singleton: true,
      eager: true,
    },
    '@mui/material': {
      singleton: true,
      eager: true,
    },
    '@mui/system': {
      singleton: true,
      eager: true,
    },
    '@mui/icons-material': {
      singleton: true,
      eager: true,
    },
    '@tanstack/react-query': {
      singleton: true,
      eager: true,
    },
    grapesjs: false,
    'grapesjs-react': false,
    'grapesjs-preset-webpage': false,
    'grapesjs-plugin-forms': false,
    '@emotion/react': false,
    '@emotion/styled': false,
  },
};
