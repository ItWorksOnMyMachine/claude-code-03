import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from '@modern-js/runtime/router';
import CmsRouter from '../CmsRouter';

// Mock the lazy-loaded components
jest.mock('../components/ContentList', () => {
  return function MockContentList(props: any) {
    return (
      <div data-testid="content-list">
        Content List - Tenant: {props.tenantId || 'none'}
      </div>
    );
  };
});

jest.mock('../components/ContentEditor', () => {
  return function MockContentEditor(props: any) {
    return (
      <div data-testid="content-editor">
        Content Editor - User: {props.userId || 'none'}
      </div>
    );
  };
});

jest.mock('../components/AssetManager', () => {
  return function MockAssetManager(props: any) {
    return (
      <div data-testid="asset-manager">
        Asset Manager - Token: {props.authToken || 'none'}
      </div>
    );
  };
});

describe('CmsRouter Component', () => {
  const defaultProps = {
    authToken: 'test-token',
    tenantId: 'test-tenant',
    userId: 'test-user',
  };

  const renderWithRouter = (initialEntries = ['/'], props = defaultProps) => {
    return render(
      <MemoryRouter initialEntries={initialEntries}>
        <CmsRouter {...props} />
      </MemoryRouter>
    );
  };

  test('should redirect root path to /content', () => {
    renderWithRouter(['/']);
    expect(screen.getByTestId('content-list')).toBeInTheDocument();
  });

  test('should render ContentList for /content route', () => {
    renderWithRouter(['/content']);
    expect(screen.getByTestId('content-list')).toBeInTheDocument();
    expect(screen.getByText('Content List - Tenant: test-tenant')).toBeInTheDocument();
  });

  test('should render ContentEditor for /content/new route', () => {
    renderWithRouter(['/content/new']);
    expect(screen.getByTestId('content-editor')).toBeInTheDocument();
    expect(screen.getByText('Content Editor - User: test-user')).toBeInTheDocument();
  });

  test('should render ContentEditor for /content/edit/:id route', () => {
    renderWithRouter(['/content/edit/123']);
    expect(screen.getByTestId('content-editor')).toBeInTheDocument();
  });

  test('should render AssetManager for /assets route', () => {
    renderWithRouter(['/assets']);
    expect(screen.getByTestId('asset-manager')).toBeInTheDocument();
    expect(screen.getByText('Asset Manager - Token: test-token')).toBeInTheDocument();
  });

  test('should redirect unknown routes to /content', () => {
    renderWithRouter(['/unknown-route']);
    expect(screen.getByTestId('content-list')).toBeInTheDocument();
  });

  test('should pass platform context to all components', () => {
    const props = {
      authToken: 'custom-token',
      tenantId: 'custom-tenant',
      userId: 'custom-user',
    };

    renderWithRouter(['/content'], props);
    expect(screen.getByText('Content List - Tenant: custom-tenant')).toBeInTheDocument();
  });
});