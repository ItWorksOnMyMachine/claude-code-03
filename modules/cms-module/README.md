# CMS Module

A comprehensive Content Management System module for the Enterprise Platform Host, providing visual content editing capabilities through Module Federation.

## Overview

The CMS Module is a micro frontend application that enables users to create, edit, and manage website content through an intuitive drag-and-drop interface powered by GrapesJS. It integrates seamlessly with the platform's authentication, multi-tenancy, and entitlement systems.

## Architecture

### Frontend (cms-frontend)
- **Framework**: React 18 with TypeScript
- **Module Federation**: Remote module exposing `./CmsApp` and `./CmsRouter`
- **Visual Editor**: GrapesJS with React integration
- **UI Library**: Material-UI with shared platform theme
- **State Management**: React Query for server state
- **Development Server**: `https://cms-fe.platform.local:3003`

### Backend (cms-bff)
- **Framework**: .NET 9 with FastEndpoints
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: Platform shared services (session-based)
- **Authorization**: Entitlement-based access control
- **Multi-tenancy**: Tenant-scoped data isolation

## Features

### Content Management
- **Visual Editor**: Drag-and-drop content creation with GrapesJS
- **Content Types**: Support for pages, templates, and components
- **Auto-save**: Automatic content saving every 30 seconds
- **Version Control**: Audit trails with created/updated timestamps
- **SEO Support**: Meta title, description, and keyword management

### Asset Management
- **File Upload**: Support for images, documents, videos, and more
- **Media Library**: Organized asset browsing with tagging system
- **Metadata**: Automatic extraction of file dimensions and properties
- **Storage**: Tenant-isolated file storage with content hashing

### Template System
- **Layout Templates**: JSON-based template definitions with zones
- **Reusable Components**: Block library for consistent design
- **Template Usage**: Tracking and deletion protection for templates in use
- **Custom Blocks**: Support for text, image, video, form, and custom blocks

### Access Control
- **Entitlement-Based**: Fine-grained permissions (CMS_ACCESS, CMS_MANAGE, etc.)
- **Tenant Isolation**: Complete data separation between tenants
- **Role-Based UI**: Conditional rendering based on user permissions
- **API Security**: Endpoint-level authorization with entitlement validation

## Database Schema

### Core Entities

#### CmsContent (Legacy)
- Content storage with title, slug, and HTML content
- Template association and metadata support
- Tenant isolation and soft delete capability

#### CmsPage (Enhanced)
- Modern page system with hierarchical support
- Status tracking (Draft, Published, Archived)
- Rich metadata with JSON storage
- Parent-child relationships for site structure

#### CmsContentBlock
- Block-based content system with zone positioning
- JSON content storage for flexible block types
- Sort ordering for layout management
- Cascade delete with parent pages

#### CmsTemplate
- Layout definitions with JSON structure
- Usage tracking and deletion protection
- Template type categorization
- Preview image support

#### CmsAsset
- Media file metadata and storage paths
- JSON tag system for organization
- Content hash for duplicate detection
- Dimension and duration tracking

### Relationships
- **Templates → Pages**: One-to-many with RESTRICT delete
- **Pages → ContentBlocks**: One-to-many with CASCADE delete
- **Pages → Pages**: Self-referencing for hierarchy

## API Endpoints

### Content Management
- `GET /api/cms/content` - List all content (requires CMS_ACCESS)
- `GET /api/cms/content/{id}` - Get content by ID (requires CMS_ACCESS)
- `POST /api/cms/content` - Create new content (requires CMS_MANAGE)
- `PUT /api/cms/content/{id}` - Update content (requires CMS_MANAGE)
- `DELETE /api/cms/content/{id}` - Delete content (requires CMS_MANAGE)

### Asset Management
- `GET /api/cms/assets` - List all assets (requires CMS_ASSETS)
- `POST /api/cms/assets/upload` - Upload new asset (requires CMS_ASSETS)

### Template Management
- `GET /api/cms/templates` - List all templates (requires CMS_TEMPLATES)

### Health & Monitoring
- `GET /health` - Service health check with dependency status

## Development Setup

### Prerequisites
- Node.js 18+ (for frontend)
- .NET 9 SDK (for backend)
- PostgreSQL (for database)
- Redis (for session storage)

### Frontend Development
```bash
cd modules/cms-module/cms-frontend

# Install dependencies
npm install

# Start development server
npm run dev

# Run tests
npm test

# Type checking
npm run type-check

# Build for production
npm run build
```

### Backend Development
```bash
cd modules/cms-module/cms-bff

# Run development server
dotnet run

# Run tests
dotnet test

# Build project
dotnet build
```

## Testing

### Frontend Tests
- **Component Tests**: React Testing Library with Jest
- **Module Federation Tests**: Remote loading and configuration
- **API Integration Tests**: Service layer with mocked endpoints
- **Entitlement Tests**: Permission-based UI rendering

### Backend Tests
- **Unit Tests**: Service layer business logic
- **Integration Tests**: API endpoints with in-memory database
- **Entity Tests**: Database model validation and relationships
- **Entitlement Tests**: Authorization attribute functionality

### Test Coverage
- **Backend**: 110+ tests covering all services and endpoints
- **Frontend**: 50+ tests covering components and integration
- **Shared Services**: 20+ tests for entitlement and session management

## Security

### Authentication & Authorization
- **Session-based**: HTTP-only cookies through platform authentication
- **Entitlement-based**: Fine-grained permission checking
- **Tenant isolation**: Complete data separation with query filters
- **API protection**: All endpoints require appropriate entitlements

### Content Security
- **XSS Protection**: DOMPurify sanitization for user content
- **CSRF Protection**: Platform-wide CSRF token validation
- **Input Validation**: FluentValidation on all API requests
- **Audit Logging**: Complete change tracking with user attribution

### Infrastructure Security
- **HTTPS Only**: All communication encrypted in transit
- **Redis Encryption**: Session and cache data protection
- **Database Security**: Row-level security with tenant filtering
- **Content Hash**: Asset deduplication and integrity validation

## Production Deployment

### Build Process
```bash
# Frontend production build
cd modules/cms-module/cms-frontend
npm run build

# Backend production build
cd modules/cms-module/cms-bff
dotnet publish -c Release
```

### Environment Configuration
- **Frontend**: Configure remote entry URL for production
- **Backend**: Update connection strings and authentication endpoints
- **Redis**: Configure production Redis cluster
- **Database**: Set up PostgreSQL with proper indexing

### Monitoring
- **Health Checks**: `/health` endpoints for load balancer monitoring
- **Logging**: Structured logging with correlation IDs
- **Metrics**: Performance and usage tracking
- **Error Tracking**: Centralized error reporting

## Module Federation Integration

### Exposed Components
- `./CmsApp` - Main CMS application component
- `./CmsRouter` - CMS routing configuration

### Platform Integration
- **Navigation**: Automatic registration with platform navigation
- **Authentication**: Seamless SSO with platform authentication
- **Entitlements**: Dynamic feature access based on user permissions
- **Theming**: Consistent Material-UI theme with platform

### Communication
- **Inter-module**: Event-based messaging for platform integration
- **API Calls**: Backend communication through platform session context
- **Error Handling**: Graceful degradation with error boundaries

## Development Guidelines

### Code Style
- **TypeScript**: Strict type checking enabled
- **React**: Functional components with hooks
- **Material-UI**: Consistent component usage
- **API**: RESTful endpoints with OpenAPI documentation

### Testing Strategy
- **TDD Approach**: Write tests before implementation
- **Integration Focus**: Test complete workflows end-to-end
- **Mock External Dependencies**: Isolate business logic testing
- **Performance Testing**: Validate response times and loading

### Contributing
1. Follow existing code patterns and conventions
2. Ensure all tests pass before committing
3. Update documentation for new features
4. Verify entitlement integration for new endpoints
5. Test module federation compatibility

## Troubleshooting

### Common Issues
- **Module Loading**: Check remote entry URL and CORS configuration
- **Authentication**: Verify platform session and tenant context
- **Entitlements**: Check user permissions and entitlement cache
- **Database**: Ensure migrations are applied and connections work

### Development Tools
- **Browser DevTools**: React and Redux DevTools extensions
- **Network Tab**: Monitor API calls and federation loading
- **Console**: Check for JavaScript errors and warnings
- **Database Tools**: pgAdmin or similar for database inspection

## Future Enhancements

### Planned Features
- **Advanced Templates**: Custom block creation and template marketplace
- **Workflow Management**: Content approval and publishing workflows
- **Analytics Integration**: Content performance tracking
- **SEO Tools**: Advanced meta tag and sitemap management
- **Multilingual Support**: Content translation and localization

### Technical Improvements
- **Performance**: Bundle splitting and lazy loading optimization
- **Offline Support**: Service worker for offline content editing
- **Real-time Collaboration**: WebSocket integration for multi-user editing
- **Advanced Search**: Full-text search with indexing and filtering