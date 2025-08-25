import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { RemoteModule } from '../RemoteModule';
import { ModuleFederationProvider } from '@/contexts/ModuleFederationContext';

// Mock the contexts and services
jest.mock('@/contexts/ModuleFederationContext', () => ({
  ...jest.requireActual('@/contexts/ModuleFederationContext'),
  useModuleFederation: jest.fn(),
  ModuleFederationProvider: ({ children }: { children: React.ReactNode }) => children,
}));

jest.mock('@/services/RemoteLoader', () => ({
  default: {
    loadModule: jest.fn(),
  },
}));

describe('RemoteModule', () => {
  const mockLoadModule = jest.fn();
  let consoleErrorSpy: jest.SpyInstance;
  
  beforeEach(() => {
    jest.clearAllMocks();
    // Suppress expected error logs
    consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    
    const { useModuleFederation } = require('@/contexts/ModuleFederationContext');
    useModuleFederation.mockReturnValue({
      modules: new Map(),
      loadModule: mockLoadModule,
      isLoading: false,
      error: null,
    });
  });

  afterEach(() => {
    consoleErrorSpy.mockRestore();
  });

  it('should display loading fallback while loading', () => {
    const { useModuleFederation } = require('@/contexts/ModuleFederationContext');
    useModuleFederation.mockReturnValue({
      modules: new Map(),
      loadModule: mockLoadModule,
      isLoading: true,
      error: null,
    });

    render(
      <RemoteModule 
        moduleName="testModule"
      />
    );

    expect(screen.getByText(/Loading testModule.../i)).toBeInTheDocument();
  });

  it('should display error fallback when module fails to load', async () => {
    const error = new Error('Failed to load module');
    mockLoadModule.mockRejectedValue(error);

    render(
      <RemoteModule 
        moduleName="testModule"
      />
    );

    await waitFor(() => {
      expect(screen.getByText(/Something went wrong/i)).toBeInTheDocument();
    });
  });

  it('should call onError callback when module fails to load', async () => {
    const error = new Error('Failed to load module');
    const onError = jest.fn();
    mockLoadModule.mockRejectedValue(error);

    render(
      <RemoteModule 
        moduleName="testModule"
        onError={onError}
      />
    );

    await waitFor(() => {
      expect(onError).toHaveBeenCalledWith(error);
    });
  });

  it('should render the loaded module component', async () => {
    const TestComponent = () => <div>Test Module Content</div>;
    mockLoadModule.mockResolvedValue({
      module: { default: TestComponent },
      error: null,
    });

    render(
      <RemoteModule 
        moduleName="testModule"
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Test Module Content')).toBeInTheDocument();
    });
  });

  it('should pass props to the loaded module', async () => {
    const TestComponent = ({ message }: { message: string }) => <div>{message}</div>;
    mockLoadModule.mockResolvedValue({
      module: { default: TestComponent },
      error: null,
    });

    render(
      <RemoteModule 
        moduleName="testModule"
        props={{ message: 'Custom Message' }}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Custom Message')).toBeInTheDocument();
    });
  });

  it('should call onLoad callback when module loads successfully', async () => {
    const TestComponent = () => <div>Test Module</div>;
    const onLoad = jest.fn();
    mockLoadModule.mockResolvedValue({
      module: { default: TestComponent },
      error: null,
    });

    render(
      <RemoteModule 
        moduleName="testModule"
        onLoad={onLoad}
      />
    );

    await waitFor(() => {
      expect(onLoad).toHaveBeenCalled();
    });
  });

  it('should show error when module cannot be loaded', async () => {
    mockLoadModule.mockResolvedValue(null);

    render(
      <RemoteModule 
        moduleName="testModule"
      />
    );

    await waitFor(() => {
      // Since returning null will throw an error "Module testModule not found or not enabled"
      expect(screen.getByText(/Something went wrong/i)).toBeInTheDocument();
    });
  });
});