import React, { useEffect } from 'react';
import { useNavigate } from '@modern-js/runtime/router';
import { useAuth } from '../../contexts/AuthContext';
import { 
  Box, 
  Button, 
  Card, 
  CardContent, 
  Typography, 
  CircularProgress,
  Alert
} from '@mui/material';
import { LogIn as LoginIcon } from 'lucide-react';

const LoginPage: React.FC = () => {
  const { isAuthenticated, isLoading, login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = React.useState<string | null>(null);
  const [isLoggingIn, setIsLoggingIn] = React.useState(false);

  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      // Redirect to saved location or dashboard
      const redirectUrl = sessionStorage.getItem('redirectAfterLogin') || '/';
      sessionStorage.removeItem('redirectAfterLogin');
      navigate(redirectUrl);
    }
  }, [isAuthenticated, isLoading, navigate]);

  const handleLogin = async () => {
    try {
      setError(null);
      setIsLoggingIn(true);
      const returnUrl = sessionStorage.getItem('redirectAfterLogin') || '/';
      await login(returnUrl);
    } catch (err) {
      setError('Failed to initiate login. Please try again.');
      setIsLoggingIn(false);
    }
  };

  if (isLoading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
      >
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight="100vh"
      sx={{ backgroundColor: 'background.default' }}
    >
      <Card sx={{ maxWidth: 400, width: '100%', mx: 2 }}>
        <CardContent sx={{ py: 4, px: 3 }}>
          <Box textAlign="center" mb={3}>
            <Typography variant="h4" component="h1" gutterBottom>
              Welcome Back
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Sign in to access the platform
            </Typography>
          </Box>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <Button
            fullWidth
            variant="contained"
            size="large"
            startIcon={isLoggingIn ? <CircularProgress size={20} /> : <LoginIcon />}
            onClick={handleLogin}
            disabled={isLoggingIn}
            sx={{ mt: 2 }}
          >
            {isLoggingIn ? 'Redirecting...' : 'Sign In with SSO'}
          </Button>

          <Box mt={3} textAlign="center">
            <Typography variant="caption" color="text.secondary">
              You will be redirected to the authentication service
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};

export default LoginPage;