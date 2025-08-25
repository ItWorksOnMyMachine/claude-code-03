import React from 'react';
import { 
  Box, 
  Typography, 
  Grid,
  Card, 
  CardContent,
  Button,
  Paper,
} from '@mui/material';
import { 
  Package, 
  Shield, 
  Layers, 
  Settings,
  ArrowRight,
} from 'lucide-react';
import { useNavigate } from '@modern-js/runtime/router';

const Index = () => {
  const navigate = useNavigate();

  const features = [
    {
      icon: <Package size={32} />,
      title: 'Module Federation',
      description: 'Dynamically load micro frontend modules at runtime with shared dependencies.',
      action: () => navigate('/modules'),
    },
    {
      icon: <Shield size={32} />,
      title: 'Secure Authentication',
      description: 'Enterprise-grade authentication with multi-tenant support.',
      action: () => console.log('Auth feature'),
    },
    {
      icon: <Layers size={32} />,
      title: 'Multi-Tenancy',
      description: 'Isolated environments for different organizations and users.',
      action: () => console.log('Tenant feature'),
    },
    {
      icon: <Settings size={32} />,
      title: 'Configuration',
      description: 'Flexible configuration for modules and platform settings.',
      action: () => navigate('/settings'),
    },
  ];

  return (
    <Box>
      <Paper 
        elevation={0} 
        sx={{ 
          p: 4, 
          mb: 4,
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          color: 'white',
        }}
      >
        <Typography variant="h3" gutterBottom>
          Welcome to Platform Host
        </Typography>
        <Typography variant="h6">
          Enterprise Micro Frontend Platform with Module Federation
        </Typography>
      </Paper>

      <Grid container spacing={3}>
        {features.map((feature, index) => (
          <Grid key={index} size={{ xs: 12, sm: 6, md: 3 }}>
            <Card 
              sx={{ 
                height: '100%',
                cursor: 'pointer',
                transition: 'all 0.3s',
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: 4,
                },
              }}
              onClick={feature.action}
            >
              <CardContent>
                <Box sx={{ color: 'primary.main', mb: 2 }}>
                  {feature.icon}
                </Box>
                <Typography variant="h6" gutterBottom>
                  {feature.title}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {feature.description}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Box sx={{ mt: 4 }}>
        <Typography variant="h5" gutterBottom>
          Getting Started
        </Typography>
        <Typography variant="body1" paragraph>
          This platform serves as the host for micro frontend applications using Module Federation.
          Navigate to the Modules page to load and manage available micro frontends.
        </Typography>
        <Button 
          variant="contained" 
          endIcon={<ArrowRight />}
          onClick={() => navigate('/modules')}
        >
          View Available Modules
        </Button>
      </Box>
    </Box>
  );
};

export default Index;
