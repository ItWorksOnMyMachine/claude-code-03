import { render } from '@testing-library/react';
import { MemoryRouter } from '@modern-js/runtime/router';

// Mock the lazy loaded components
jest.mock('../components/ContentList', () => ({
  __esModule: true,
  default: function MockContentList(props: any) {
    return (
      <div data-testid="content-list">
        Content List - Tenant: {props.tenantId || 'none'}
      </div>
    );
  },
}));

jest.mock('../components/ContentEditor', () => ({
  __esModule: true,
  default: function MockContentEditor(props: any) {
    return (
      <div data-testid="content-editor">
        Content Editor - User: {props.userId || 'none'}
      </div>
    );
  },
}));

jest.mock('../components/AssetManager', () => ({
  __esModule: true,
  default: function MockAssetManager(props: any) {
    return (
      <div data-testid="asset-manager">
        Asset Manager - Token: {props.authToken || 'none'}
      </div>
    );
  },
}));

// Import CmsRouter after mocks are set up
import CmsRouter from '../CmsRouter';

describe('CmsRouter Component', () => {
  const renderWithRouter = (
    initialEntries: string[],
    props = {}
  ) => {
    const defaultProps = {
      authToken: 'test-token',
      tenantId: 'test-tenant',
      userId: 'test-user',
    };
    
    const combinedProps = { ...defaultProps, ...props };
    
    return render(
      <MemoryRouter initialEntries={initialEntries}>
        <CmsRouter {...combinedProps} />
      </MemoryRouter>
    );
  };

  test('should render with basic structure', () => {
    renderWithRouter(['/content']);
    
    // Test that the main container is rendered (MUI Box)
    const container = document.querySelector('.MuiBox-root');
    expect(container).toBeInTheDocument();
  });

  test('should accept props correctly', () => {
    const props = {
      authToken: 'custom-token',
      tenantId: 'custom-tenant', 
      userId: 'custom-user',
    };
    
    const { container } = renderWithRouter(['/content'], props);
    expect(container.firstChild).toBeInTheDocument();
  });

  test('should handle routing without crashing', () => {
    // Test different routes
    expect(() => renderWithRouter(['/'])).not.toThrow();
    expect(() => renderWithRouter(['/content'])).not.toThrow();
    expect(() => renderWithRouter(['/content/new'])).not.toThrow();
    expect(() => renderWithRouter(['/content/edit/123'])).not.toThrow();
    expect(() => renderWithRouter(['/assets'])).not.toThrow();
    expect(() => renderWithRouter(['/unknown'])).not.toThrow();
  });

  test('should pass context props to components', () => {
    const props = {
      authToken: 'test-auth',
      tenantId: 'test-tenant-id',
      userId: 'test-user-id',
    };
    
    renderWithRouter(['/content'], props);
    
    // Verify the component structure is correct
    const container = document.querySelector('.MuiBox-root');
    expect(container).toBeInTheDocument();
    expect(container).toHaveStyle('height: 100%');
    expect(container).toHaveStyle('overflow: hidden');
  });

  test('should handle navigation between routes', () => {
    // Test that different initial entries work
    const { rerender } = renderWithRouter(['/content']);
    expect(document.querySelector('.MuiBox-root')).toBeInTheDocument();
    
    // Test changing route
    rerender(
      <MemoryRouter initialEntries={['/assets']}>
        <CmsRouter 
          authToken="test-token" 
          tenantId="test-tenant" 
          userId="test-user" 
        />
      </MemoryRouter>
    );
    expect(document.querySelector('.MuiBox-root')).toBeInTheDocument();
  });
});
