import React from 'react';
import { Box, Typography, Button, Paper } from '@mui/material';
import { AlertCircle, RefreshCw, Home } from 'lucide-react';
import { useNavigate } from '@modern-js/runtime/router';

export interface ErrorFallbackProps {
  error?: Error | string;
  resetError?: () => void;
  showHomeButton?: boolean;
  compact?: boolean;
}

export const ErrorFallback: React.FC<ErrorFallbackProps> = ({
  error,
  resetError,
  showHomeButton = true,
  compact = false,
}) => {
  const navigate = useNavigate();
  
  const errorMessage = typeof error === 'string' 
    ? error 
    : error?.message || 'An unexpected error occurred';

  const handleGoHome = () => {
    navigate('/');
  };

  if (compact) {
    return (
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 2,
          p: 2,
          backgroundColor: 'error.lighter',
          borderRadius: 1,
          border: 1,
          borderColor: 'error.main',
        }}
      >
        <AlertCircle size={20} color="error" />
        <Typography variant="body2" color="error" sx={{ flexGrow: 1 }}>
          {errorMessage}
        </Typography>
        {resetError && (
          <Button size="small" onClick={resetError}>
            Retry
          </Button>
        )}
      </Box>
    );
  }

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
          backgroundColor: 'background.paper',
          border: 1,
          borderColor: 'divider',
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
          <AlertCircle size={48} />
        </Box>

        <Typography variant="h5" gutterBottom>
          Oops! Something went wrong
        </Typography>

        <Typography variant="body1" color="text.secondary" paragraph>
          {errorMessage}
        </Typography>

        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', mt: 3 }}>
          {resetError && (
            <Button
              variant="contained"
              startIcon={<RefreshCw size={20} />}
              onClick={resetError}
            >
              Try Again
            </Button>
          )}
          
          {showHomeButton && (
            <Button
              variant="outlined"
              startIcon={<Home size={20} />}
              onClick={handleGoHome}
            >
              Go Home
            </Button>
          )}
        </Box>
      </Paper>
    </Box>
  );
};

export default ErrorFallback;