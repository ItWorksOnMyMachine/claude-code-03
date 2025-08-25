import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import TenantSelectionPage from './page';

// Mock the dependencies
jest.mock('../../contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

jest.mock('../../components/TenantSelector', () => ({
  TenantSelector: ({ onTenantSelect }: any) => (
    <div>
      <button onClick={() => onTenantSelect('tenant-1')}>
        Select Tenant
      </button>
    </div>
  ),
}));

// Mock navigate
const mockNavigate = jest.fn();
jest.mock('@modern-js/runtime/router', () => ({
  ...jest.requireActual('@modern-js/runtime/router'),
  useNavigate: () => mockNavigate,
  useSearchParams: () => [new URLSearchParams()],
}));

describe('TenantSelectionPage', () => {
  const { useAuth } = require('../../contexts/AuthContext');

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state when authentication is being checked', () => {
    useAuth.mockReturnValue({
      isAuthenticated: false,
      isLoading: true,
    });

    render(
      <MemoryRouter>
        <TenantSelectionPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('redirects to login when not authenticated', () => {
    useAuth.mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
    });

    render(
      <MemoryRouter>
        <TenantSelectionPage />
      </MemoryRouter>
    );

    expect(mockNavigate).toHaveBeenCalledWith('/login?returnUrl=/tenant-selection');
  });

  it('renders tenant selector when authenticated', () => {
    useAuth.mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    });

    render(
      <MemoryRouter>
        <TenantSelectionPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Welcome Back!')).toBeInTheDocument();
    expect(screen.getByText('Please select an organization to continue')).toBeInTheDocument();
    expect(screen.getByText('Select Tenant')).toBeInTheDocument();
  });

  it('navigates to dashboard after tenant selection', () => {
    useAuth.mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    });

    render(
      <MemoryRouter>
        <TenantSelectionPage />
      </MemoryRouter>
    );

    const selectButton = screen.getByText('Select Tenant');
    selectButton.click();

    expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
  });

  it('navigates to custom returnUrl after tenant selection', () => {
    useAuth.mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    });

    // Mock search params with returnUrl
    jest.spyOn(require('@modern-js/runtime/router'), 'useSearchParams').mockReturnValue([
      new URLSearchParams('returnUrl=/custom-page'),
    ]);

    render(
      <MemoryRouter>
        <TenantSelectionPage />
      </MemoryRouter>
    );

    const selectButton = screen.getByText('Select Tenant');
    selectButton.click();

    expect(mockNavigate).toHaveBeenCalledWith('/custom-page');
  });
});