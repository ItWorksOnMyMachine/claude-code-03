# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-09-11-cms-module/spec.md

> Created: 2025-09-11
> Version: 1.0.0

## Technical Requirements

### Module Federation Configuration

- **Remote Module Name**: `cmsModule`
- **Exposed Components**: 
  - `./CmsApp` - Main CMS application entry point
  - `./CmsRouter` - CMS routing configuration for platform integration
- **Shared Dependencies**: 
  - React 18+ (singleton)
  - React-DOM (singleton) 
  - Material-UI core components (singleton)
  - React Query/TanStack Query (singleton)
  - Emotion styling (singleton)
- **Build Configuration**: Webpack Module Federation plugin with development and production builds
- **Port Assignment**: Development server on `https://cms-fe.platform.local:3003`

### GrapesJS Integration Details

- **Core Integration**: Embed GrapesJS editor within React component using `grapesjs` npm package
- **Editor Configuration**:
  - Container: React ref-managed DOM element
  - Storage: Custom storage manager integration with platform APIs
  - Asset Manager: Integration with platform's shared asset management system
  - Canvas: Responsive design support with mobile/tablet/desktop previews
- **Plugin Integration**: 
  - Basic blocks plugin for common components (text, image, link, button)
  - Preset webpage plugin for standard web content structure
  - Forms plugin for interactive content creation
- **Theme Integration**: Custom CSS styling to match platform's Material-UI theme
- **Content Persistence**: Auto-save functionality with platform API integration

### FastEndpoints Implementation

- **CMS Content Endpoints**:
  - `GET /api/cms/content` - Retrieve tenant-scoped content list
  - `GET /api/cms/content/{id}` - Get specific content item
  - `POST /api/cms/content` - Create new content
  - `PUT /api/cms/content/{id}` - Update existing content
  - `DELETE /api/cms/content/{id}` - Delete content (soft delete)
- **Asset Management Endpoints**:
  - `POST /api/cms/assets/upload` - Upload content assets (images, files)
  - `GET /api/cms/assets` - List tenant assets
  - `DELETE /api/cms/assets/{id}` - Remove asset
- **Template Endpoints**:
  - `GET /api/cms/templates` - Get available content templates
  - `POST /api/cms/templates` - Create custom template
- **Request/Response Models**:
  - `ContentCreateRequest/ContentResponse`
  - `AssetUploadRequest/AssetResponse`  
  - `TemplateCreateRequest/TemplateResponse`
- **Validation**: FluentValidation rules for all content operations
- **Error Handling**: Structured error responses with appropriate HTTP status codes

### Shared Session Service Architecture

- **Session Context Integration**: Utilize platform's `ISessionService` for user context
- **Tenant Resolution**: Extract tenant ID from authenticated user session
- **User Authorization**: Verify CMS entitlements through platform's entitlement service
- **Session Management**: 
  - Leverage HTTP-only authentication cookies set by platform
  - Session refresh handling through platform middleware
  - Automatic session validation for all CMS operations

### Data Protection Centralization

- **Tenant Data Isolation**: 
  - All CMS queries filtered by tenant ID from session context
  - Database-level tenant scoping through `BaseRepository` patterns
  - Content access restricted to authenticated tenant users only
- **Permission Boundaries**:
  - Content CRUD operations require "CMS_MANAGE" entitlement
  - Asset upload requires "CMS_ASSETS" entitlement
  - Template creation requires "CMS_TEMPLATES" entitlement
- **Data Sanitization**: HTML content sanitization for XSS protection
- **Audit Logging**: Track all content modifications with user and timestamp information

### Entitlement Checking System

- **Platform Integration**: Use existing `IEntitlementService` from platform host
- **Required Entitlements**:
  - `CMS_ACCESS` - Basic CMS module access
  - `CMS_MANAGE` - Content creation and editing
  - `CMS_ASSETS` - Asset upload and management
  - `CMS_TEMPLATES` - Custom template creation
- **Check Implementation**: 
  - Frontend: React hook for entitlement validation before rendering CMS components
  - Backend: FastEndpoints authorization attributes for API protection
  - Real-time: WebSocket integration for entitlement changes
- **Graceful Degradation**: Progressive feature availability based on user entitlements

### Frontend Architecture

- **Component Structure**:
  - `CmsApp.tsx` - Main application shell with routing
  - `ContentEditor.tsx` - GrapesJS editor wrapper component
  - `ContentList.tsx` - Content management dashboard
  - `AssetManager.tsx` - Asset upload and management interface
- **State Management**: React Query for server state, React Context for UI state
- **Navigation Integration**: Utilize platform's `NavigationContext` for menu integration
- **Error Boundaries**: Isolated error handling to prevent platform-wide failures
- **Loading States**: Skeleton loading components matching platform design patterns

### Backend Architecture

- **Controller Organization**: `CmsController` with feature-grouped endpoints
- **Service Layer**: 
  - `CmsContentService` - Business logic for content operations
  - `CmsAssetService` - File upload and asset management
  - `CmsTemplateService` - Template management operations
- **Repository Pattern**: Extend platform's `BaseRepository<T>` for tenant-scoped data access
- **Database Integration**: Entity Framework Core with existing platform context

## External Dependencies

### GrapesJS Core Library
- **Package**: `grapesjs@^0.21.7`
- **Purpose**: Visual drag-and-drop content editor
- **Justification**: Industry-standard WYSIWYG editor with extensive plugin ecosystem and React integration support

### GrapesJS React Integration
- **Package**: `grapesjs-react@^3.0.2` 
- **Purpose**: React wrapper for seamless GrapesJS integration
- **Justification**: Provides proper React lifecycle management and component integration for GrapesJS

### GrapesJS Preset Webpage
- **Package**: `grapesjs-preset-webpage@^1.0.2`
- **Purpose**: Pre-built components and blocks for web content creation
- **Justification**: Accelerates development by providing common web components out-of-the-box

### GrapesJS Plugin Forms
- **Package**: `grapesjs-plugin-forms@^2.0.6`
- **Purpose**: Form building capabilities within content editor
- **Justification**: Essential for creating interactive content with form elements

### File Upload Handling
- **Package**: `react-dropzone@^14.2.3`
- **Purpose**: Drag-and-drop file upload interface for assets
- **Justification**: Enhanced UX for asset management with progress indicators and file validation

### Content Sanitization
- **Package**: `dompurify@^3.0.6` (Frontend), `HtmlSanitizer` (Backend)
- **Purpose**: XSS protection for user-generated HTML content
- **Justification**: Critical security requirement for safely handling user-created content

### Shared Platform Dependencies
- **React Query**: Already shared singleton for API state management
- **Material-UI**: Shared UI component library for consistent design
- **Emotion**: Shared styling system for theme consistency
- **Platform Auth Context**: Shared authentication and session management
- **Platform Navigation Context**: Shared navigation integration
- **Platform Entitlement Service**: Shared authorization system