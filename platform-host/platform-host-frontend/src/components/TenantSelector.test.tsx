import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { TenantSelector } from './TenantSelector';

// Mock fetch globally
global.fetch = jest.fn();

describe('TenantSelector', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (global.fetch as jest.Mock).mockImplementationOnce(
      () => new Promise(() => {}) // Never resolves to keep loading state
    );

    render(<TenantSelector onTenantSelect={jest.fn()} />);
    
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('fetches and displays available tenants', async () => {
    const mockTenants = [
      {
        id: 'tenant-1',
        name: 'Company A',
        description: 'First company',
      },
      {
        id: 'tenant-2',
        name: 'Company B',
        description: 'Second company',
      },
    ];

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ tenants: mockTenants }),
    });

    render(<TenantSelector onTenantSelect={jest.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Company A')).toBeInTheDocument();
      expect(screen.getByText('Company B')).toBeInTheDocument();
    });
  });

  it('displays platform tenant with special badge', async () => {
    const mockTenants = [
      {
        id: 'platform-tenant',
        name: 'Platform Administration',
        description: 'Platform admin access',
        isPlatformTenant: true,
      },
    ];

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ tenants: mockTenants }),
    });

    render(<TenantSelector onTenantSelect={jest.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Platform Administration')).toBeInTheDocument();
      expect(screen.getByText('Platform Admin')).toBeInTheDocument();
    });
  });

  it('handles tenant selection', async () => {
    const mockOnTenantSelect = jest.fn();
    const mockTenants = [
      {
        id: 'tenant-1',
        name: 'Company A',
        description: 'First company',
      },
    ];

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ tenants: mockTenants }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ success: true }),
      });

    render(<TenantSelector onTenantSelect={mockOnTenantSelect} />);

    await waitFor(() => {
      expect(screen.getByText('Company A')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Company A'));

    await waitFor(() => {
      expect(mockOnTenantSelect).toHaveBeenCalledWith('tenant-1');
    });

    expect(global.fetch).toHaveBeenCalledWith(
      'http://localhost:5000/api/tenant/select',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ tenantId: 'tenant-1' }),
      })
    );
  });

  it('displays error when fetching tenants fails', async () => {
    (global.fetch as jest.Mock).mockRejectedValueOnce(
      new Error('Network error')
    );

    render(<TenantSelector onTenantSelect={jest.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument();
    }, { timeout: 3000 });
  });

  it('displays error when tenant selection fails', async () => {
    const mockTenants = [
      {
        id: 'tenant-1',
        name: 'Company A',
      },
    ];

    (global.fetch as jest.Mock)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ tenants: mockTenants }),
      })
      .mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Selection failed' }),
      });

    render(<TenantSelector onTenantSelect={jest.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Company A')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Company A'));

    await waitFor(() => {
      expect(screen.getByText('Selection failed')).toBeInTheDocument();
    });
  });

  it('displays message when no tenants are available', async () => {
    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ tenants: [] }),
    });

    render(<TenantSelector onTenantSelect={jest.fn()} />);

    await waitFor(() => {
      expect(
        screen.getByText('No tenants available. Please contact your administrator.')
      ).toBeInTheDocument();
    });
  });

  it('highlights currently selected tenant', async () => {
    const mockTenants = [
      {
        id: 'tenant-1',
        name: 'Company A',
      },
      {
        id: 'tenant-2',
        name: 'Company B',
      },
    ];

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ tenants: mockTenants }),
    });

    render(
      <TenantSelector
        onTenantSelect={jest.fn()}
        currentTenantId="tenant-1"
      />
    );

    await waitFor(() => {
      const companyAButton = screen.getByText('Company A').closest('div[role="button"]');
      const companyBButton = screen.getByText('Company B').closest('div[role="button"]');
      
      expect(companyAButton).toHaveClass('Mui-selected');
      expect(companyBButton).not.toHaveClass('Mui-selected');
    });
  });

  it('hides header when showHeader is false', async () => {
    const mockTenants = [
      {
        id: 'tenant-1',
        name: 'Company A',
      },
    ];

    (global.fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ tenants: mockTenants }),
    });

    render(
      <TenantSelector
        onTenantSelect={jest.fn()}
        showHeader={false}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Company A')).toBeInTheDocument();
    });

    expect(screen.queryByText('Select Organization')).not.toBeInTheDocument();
  });
});