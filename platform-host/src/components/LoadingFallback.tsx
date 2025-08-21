import React from 'react';
import { Box, CircularProgress, Typography, Skeleton } from '@mui/material';

export interface LoadingFallbackProps {
  message?: string;
  variant?: 'circular' | 'skeleton' | 'linear';
  height?: number | string;
}

export const LoadingFallback: React.FC<LoadingFallbackProps> = ({
  message = 'Loading...',
  variant = 'circular',
  height = 400,
}) => {
  if (variant === 'skeleton') {
    return (
      <Box sx={{ p: 3 }}>
        <Skeleton variant="text" width="40%" height={40} />
        <Skeleton variant="rectangular" width="100%" height={height} sx={{ mt: 2 }} />
        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
          <Skeleton variant="rectangular" width="30%" height={100} />
          <Skeleton variant="rectangular" width="30%" height={100} />
          <Skeleton variant="rectangular" width="30%" height={100} />
        </Box>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: height,
        p: 3,
      }}
    >
      <CircularProgress size={40} />
      <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
        {message}
      </Typography>
    </Box>
  );
};

export default LoadingFallback;