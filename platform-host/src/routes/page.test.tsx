import { describe, it, expect } from '@jest/globals';
import { render, screen } from '@testing-library/react';
import Page from './page';

describe('Platform Host - Main Page', () => {
  it('should render without crashing', () => {
    const { container } = render(<Page />);
    expect(container).toBeInTheDocument();
  });

  it('should display ModernJS welcome content', () => {
    render(<Page />);
    // Check for ModernJS text content
    const getStartedText = screen.getByText(/Get started by editing/i);
    expect(getStartedText).toBeInTheDocument();
    
    // Check for code element
    const codeElement = screen.getByText('src/routes/page.tsx');
    expect(codeElement).toBeInTheDocument();
  });
});