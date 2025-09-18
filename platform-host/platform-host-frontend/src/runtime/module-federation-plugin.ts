import { init, registerRemotes } from '@module-federation/enhanced/runtime';

// Initialize Module Federation runtime
init({
  name: 'platform_host',
  remotes: [
    {
      name: 'cmsModule',
      entry: 'https://cms-fe.platform.local:3003/remoteEntry.js',
    }
  ],
});

// This plugin will be loaded by Module Federation at runtime
export default function () {
  console.log('Module Federation runtime plugin initialized');

  // Register the CMS module remote at runtime
  registerRemotes([
    {
      name: 'cmsModule',
      entry: 'https://cms-fe.platform.local:3003/remoteEntry.js',
    }
  ]);
}