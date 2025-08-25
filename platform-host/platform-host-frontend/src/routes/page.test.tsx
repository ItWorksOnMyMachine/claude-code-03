import { describe, it, expect, jest } from '@jest/globals';
import { render, screen } from '@testing-library/react';
import Page from './page';

// Mock the router
jest.mock('@modern-js/runtime/router', () => ({
  useNavigate: () => jest.fn(),
}));

describe('Platform Host - Main Page', () => {
  it('should render without crashing', () => {
    const { container } = render(<Page />);
    expect(container).toBeInTheDocument();
  });

  it('should display welcome content', () => {
    render(<Page />);
    // Check for new welcome text
    const welcomeText = screen.getByText(/Welcome to Platform Host/i);
    expect(welcomeText).toBeInTheDocument();
    
    // Check for subtitle
    const subtitle = screen.getByText(/Enterprise Micro Frontend Platform/i);
    expect(subtitle).toBeInTheDocument();
  });

  it('should display feature cards', () => {
    render(<Page />);
    
    // Check for feature cards
    expect(screen.getByText('Module Federation')).toBeInTheDocument();
    expect(screen.getByText('Secure Authentication')).toBeInTheDocument();
    expect(screen.getByText('Multi-Tenancy')).toBeInTheDocument();
    expect(screen.getByText('Configuration')).toBeInTheDocument();
  });
});