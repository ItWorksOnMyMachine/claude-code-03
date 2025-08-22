import React, { useEffect } from 'react';
import { useNavigate } from '@modern-js/runtime/router';
import { Box, CircularProgress, Typography } from '@mui/material';
import { useAuth } from '../../../contexts/AuthContext';

const AuthCallbackPage: React.FC = () => {
  const navigate = useNavigate();
  const { checkSession } = useAuth();

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Check session to get the updated authentication state
        await checkSession();
        
        // Get the return URL from session storage or default to home
        const returnUrl = sessionStorage.getItem('redirectAfterLogin') || '/';
        sessionStorage.removeItem('redirectAfterLogin');
        
        // Navigate to the return URL
        navigate(returnUrl);
      } catch (error) {
        console.error('Authentication callback error:', error);
        navigate('/login?error=callback_failed');
      }
    };

    handleCallback();
  }, [checkSession, navigate]);

  return (
    <Box
      display="flex"
      flexDirection="column"
      justifyContent="center"
      alignItems="center"
      minHeight="100vh"
    >
      <CircularProgress size={48} />
      <Typography variant="h6" sx={{ mt: 3 }}>
        Completing sign in...
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        Please wait while we redirect you.
      </Typography>
    </Box>
  );
};

export default AuthCallbackPage;