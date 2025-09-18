import { Component, ErrorInfo, ReactNode } from 'react';

interface Props {
  children?: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
  };

  public static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI
    return { 
      hasError: true,
      error,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('CMS Module Error Boundary caught an error:', error, errorInfo);
    
    // Log error to monitoring service if available
    if (window.console && window.console.error) {
      window.console.error('CMS Module Error:', {
        error: error.message,
        stack: error.stack,
        componentStack: errorInfo.componentStack,
        timestamp: new Date().toISOString(),
      });
    }
  }

  public render() {
    if (this.state.hasError) {
      // Return custom fallback UI
      return this.props.fallback || (
        <div style={{ 
          padding: '20px', 
          border: '1px solid #f5c6cb',
          borderRadius: '4px',
          backgroundColor: '#f8d7da',
          color: '#721c24',
          margin: '10px'
        }}>
          <h2>Something went wrong in CMS Module</h2>
          <details>
            <summary>Error details</summary>
            <pre style={{ marginTop: '10px', fontSize: '12px' }}>
              {this.state.error?.message}
              {this.state.error?.stack}
            </pre>
          </details>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;