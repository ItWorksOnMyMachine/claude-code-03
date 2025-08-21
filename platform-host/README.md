# Platform Host - Module Federation Application

The Platform Host is a micro frontend host application built with ModernJS and React, implementing Module Federation for dynamic loading of remote modules.

## 🚀 Quick Start

### Prerequisites

- Node.js >= 16.18.1
- npm or pnpm
- Platform BFF running on port 5000 (optional for API integration)

### Installation

```bash
# Install dependencies
npm install

# Copy environment variables
cp .env.example .env.local
```

### Development

```bash
# Start development server on port 3002
npm run dev

# The application will be available at:
# http://localhost:3002
```

## 📜 Available Scripts

| Script | Description |
|--------|-------------|
| `npm run dev` | Start development server with HMR |
| `npm run build` | Build for production |
| `npm run build:prod` | Build with NODE_ENV=production |
| `npm run build:analyze` | Build and analyze bundle size |
| `npm start` | Start production server |
| `npm test` | Run all tests |
| `npm run test:watch` | Run tests in watch mode |
| `npm run test:coverage` | Run tests with coverage report |
| `npm run type-check` | Type check TypeScript files |
| `npm run lint` | Run linter |
| `npm run clean` | Clean build output |
| `npm run clean:all` | Clean all generated files and node_modules |

## 🏗️ Architecture

### Module Federation

The application is configured as a Module Federation host that can dynamically load remote modules at runtime.

**Key Features:**
- Dynamic remote loading
- Shared dependencies (React, MUI, etc.)
- Runtime module discovery
- Error boundaries for fault isolation

### Project Structure

```
platform-host/
├── src/
│   ├── components/       # Reusable components
│   │   ├── layout/       # Layout components (AppShell, Header, Sidebar)
│   │   ├── ErrorBoundary.tsx
│   │   ├── LoadingFallback.tsx
│   │   └── RemoteModule.tsx
│   ├── contexts/         # React contexts
│   │   └── ModuleFederationContext.tsx
│   ├── services/         # Business logic and services
│   │   ├── RemoteLoader.ts
│   │   └── ModuleRegistry.ts
│   ├── utils/           # Utility functions
│   │   └── moduleDiscovery.ts
│   ├── config/          # Configuration
│   │   └── environment.ts
│   ├── routes/          # Application routes
│   └── tests/           # Test files
├── modern.config.ts     # ModernJS configuration
├── module-federation.config.ts # Module Federation config
└── .env.example        # Environment variables template
```

## ⚙️ Configuration

### Environment Variables

Create a `.env.local` file based on `.env.example`:

```bash
# Application
NODE_ENV=development
PORT=3002

# API Configuration
API_URL=http://localhost:5000
API_TIMEOUT=30000

# Module Federation
REMOTE_MODULES_DISCOVERY_URL=/api/federation/modules

# Feature Flags
ENABLE_MODULE_DISCOVERY=true
ENABLE_HEALTH_CHECKS=true
```

### API Proxy

The development server proxies `/api/*` requests to the Platform BFF running on port 5000:

```typescript
// modern.config.ts
proxy: {
  '/api': {
    target: 'http://localhost:5000',
    changeOrigin: true,
    ws: true, // WebSocket support
  }
}
```

## 🧪 Testing

### Running Tests

```bash
# Run all tests
npm test

# Run specific test file
npm test -- src/tests/module-federation.test.ts

# Run tests in watch mode
npm run test:watch

# Generate coverage report
npm run test:coverage
```

### Test Structure

- Unit tests: Located alongside components in `__tests__` folders
- Integration tests: Located in `src/tests/`
- Test utilities: Located in `src/tests/setup.ts`

## 🚢 Deployment

### Building for Production

```bash
# Build the application
npm run build:prod

# The output will be in the ./dist directory
```

### Production Configuration

Set the following environment variables for production:

- `NODE_ENV=production`
- `API_URL` - Production API endpoint
- `ASSET_PREFIX` - CDN or asset path prefix

### Running in Production

```bash
# After building
npm start

# The server will start on the configured PORT (default: 3002)
```

## 🔧 Module Federation

### Loading Remote Modules

The application can dynamically load remote modules:

```typescript
import { RemoteModule } from '@/components/RemoteModule';

// Load a remote module
<RemoteModule 
  moduleName="cms"
  fallback={<LoadingFallback />}
  onError={(error) => console.error(error)}
/>
```

### Registering Remote Modules

```typescript
import moduleRegistry from '@/services/ModuleRegistry';

// Register a new module
moduleRegistry.register({
  name: 'cms',
  entry: 'http://localhost:3003/remoteEntry.js',
  exposedModule: './App',
  displayName: 'Content Management',
  route: '/cms',
  enabled: true,
});
```

### Module Discovery

The application supports automatic module discovery from an API endpoint:

```typescript
import { moduleDiscovery } from '@/utils/moduleDiscovery';

// Start automatic discovery
await moduleDiscovery.startDiscovery();
```

## 🐛 Troubleshooting

### Common Issues

1. **Port already in use**
   ```bash
   # Change the port in .env.local
   PORT=3003
   ```

2. **API proxy not working**
   - Ensure Platform BFF is running on port 5000
   - Check proxy configuration in `modern.config.ts`

3. **Module loading errors**
   - Verify remote module is running and accessible
   - Check browser console for CORS errors
   - Ensure shared dependencies match versions

4. **Build errors**
   ```bash
   # Clean and rebuild
   npm run clean:all
   npm install
   npm run build
   ```

## 📚 Resources

- [ModernJS Documentation](https://modernjs.dev/)
- [Module Federation](https://module-federation.github.io/)
- [React Documentation](https://react.dev/)
- [Material-UI](https://mui.com/)

## 📄 License

This project is part of the Enterprise Platform Host system.