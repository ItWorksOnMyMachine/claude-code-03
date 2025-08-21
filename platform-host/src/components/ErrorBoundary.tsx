import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Box, Typography, Button, Paper, Alert } from '@mui/material';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  isolate?: boolean;
  moduleName?: string;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): State {
    return {
      hasError: true,
      error,
      errorInfo: null,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    
    this.setState({
      error,
      errorInfo,
    });

    // Log to monitoring service in production
    if (process.env.NODE_ENV === 'production') {
      // TODO: Send error to monitoring service
      console.error(`Module Error [${this.props.moduleName || 'Unknown'}]:`, {
        error: error.toString(),
        componentStack: errorInfo.componentStack,
      });
    }
  }

  handleReset = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  render() {
    if (this.state.hasError) {
      // If a custom fallback is provided, use it
      if (this.props.fallback) {
        return <>{this.props.fallback}</>;
      }

      // For isolated module errors, show a compact error message
      if (this.props.isolate) {
        return (
          <Alert 
            severity="error" 
            action={
              <Button color="inherit" size="small" onClick={this.handleReset}>
                Retry
              </Button>
            }
          >
            {this.props.moduleName 
              ? `Failed to load module: ${this.props.moduleName}`
              : 'Module failed to load'
            }
          </Alert>
        );
      }

      // Default error UI for non-isolated errors
      return (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: '400px',
            p: 3,
          }}
        >
          <Paper
            elevation={0}
            sx={{
              p: 4,
              maxWidth: 500,
              textAlign: 'center',
              backgroundColor: 'background.default',
              border: 1,
              borderColor: 'error.main',
              borderRadius: 2,
            }}
          >
            <Box
              sx={{
                display: 'flex',
                justifyContent: 'center',
                mb: 2,
                color: 'error.main',
              }}
            >
              <AlertTriangle size={48} />
            </Box>

            <Typography variant="h5" gutterBottom color="error">
              Something went wrong
            </Typography>

            <Typography variant="body2" color="text.secondary" paragraph>
              {this.state.error?.message || 'An unexpected error occurred'}
            </Typography>

            {process.env.NODE_ENV === 'development' && this.state.errorInfo && (
              <Box
                component="details"
                sx={{
                  mt: 2,
                  p: 2,
                  backgroundColor: 'grey.100',
                  borderRadius: 1,
                  textAlign: 'left',
                }}
              >
                <summary style={{ cursor: 'pointer' }}>
                  <Typography variant="caption">Error Details</Typography>
                </summary>
                <Typography
                  variant="caption"
                  component="pre"
                  sx={{
                    mt: 1,
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word',
                    fontFamily: 'monospace',
                  }}
                >
                  {this.state.error?.stack}
                </Typography>
              </Box>
            )}

            <Button
              variant="contained"
              startIcon={<RefreshCw size={20} />}
              onClick={this.handleReset}
              sx={{ mt: 2 }}
            >
              Try Again
            </Button>
          </Paper>
        </Box>
      );
    }

    return this.props.children;
  }
}

// Higher-order component for wrapping remote modules
export function withErrorBoundary<P extends object>(
  Component: React.ComponentType<P>,
  moduleName?: string
): React.ComponentType<P> {
  return (props: P) => (
    <ErrorBoundary isolate moduleName={moduleName}>
      <Component {...props} />
    </ErrorBoundary>
  );
}

export default ErrorBoundary;