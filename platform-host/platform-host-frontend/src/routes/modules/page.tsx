import React, { Suspense, lazy } from 'react';
import { 
  Grid, 
  Card, 
  CardContent, 
  CardActions,
  Typography, 
  Button,
  Box,
  Chip,
} from '@mui/material';
import { Package, ExternalLink } from 'lucide-react';
import { useModuleFederation } from '@/contexts/ModuleFederationContext';
import LoadingFallback from '@/components/LoadingFallback';
import ErrorBoundary from '@/components/ErrorBoundary';
import { useNavigate } from '@modern-js/runtime/router';

const ModulesPage: React.FC = () => {
  const { modules, loadModule, isLoading, error } = useModuleFederation();
  const navigate = useNavigate();

  const handleLoadModule = async (moduleName: string, route: string) => {
    try {
      await loadModule(moduleName);
      navigate(route);
    } catch (err) {
      console.error(`Failed to load module ${moduleName}:`, err);
    }
  };

  // Example modules - in production these would come from the API
  const availableModules = [
    {
      name: 'cms',
      displayName: 'Content Management',
      description: 'Create and manage website content',
      route: '/cms',
      status: 'available',
      icon: '📝',
    },
    {
      name: 'forms',
      displayName: 'Forms Builder',
      description: 'Build and manage dynamic forms',
      route: '/forms',
      status: 'coming-soon',
      icon: '📋',
    },
    {
      name: 'analytics',
      displayName: 'Analytics Dashboard',
      description: 'View platform analytics and metrics',
      route: '/analytics',
      status: 'coming-soon',
      icon: '📊',
    },
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Available Modules
      </Typography>
      <Typography variant="body1" color="text.secondary" paragraph>
        Load and manage micro frontend modules
      </Typography>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        {availableModules.map((module) => (
          <Grid item xs={12} sm={6} md={4} key={module.name}>
            <Card 
              sx={{ 
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                position: 'relative',
              }}
            >
              <CardContent sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'flex-start', mb: 2 }}>
                  <Typography variant="h4" sx={{ mr: 2 }}>
                    {module.icon}
                  </Typography>
                  <Box sx={{ flexGrow: 1 }}>
                    <Typography variant="h6" component="h2">
                      {module.displayName}
                    </Typography>
                    {module.status === 'coming-soon' && (
                      <Chip 
                        label="Coming Soon" 
                        size="small" 
                        color="warning"
                        sx={{ mt: 0.5 }}
                      />
                    )}
                  </Box>
                </Box>
                
                <Typography variant="body2" color="text.secondary">
                  {module.description}
                </Typography>
              </CardContent>
              
              <CardActions>
                <Button
                  size="small"
                  startIcon={<ExternalLink size={16} />}
                  onClick={() => handleLoadModule(module.name, module.route)}
                  disabled={module.status === 'coming-soon' || isLoading}
                >
                  {module.status === 'coming-soon' ? 'Coming Soon' : 'Open Module'}
                </Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      {error && (
        <Box sx={{ mt: 2 }}>
          <Typography color="error">
            Error loading modules: {error.message}
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default ModulesPage;