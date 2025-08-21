import React, { useState } from 'react';
import {
  Box,
  Button,
  Typography,
  Paper,
  Alert,
  Stack,
} from '@mui/material';
import { RemoteModule } from '@/components/RemoteModule';
import moduleRegistry from '@/services/ModuleRegistry';

const TestModulePage: React.FC = () => {
  const [moduleRegistered, setModuleRegistered] = useState(false);
  const [showModule, setShowModule] = useState(false);

  const registerTestModule = () => {
    // Register the test module
    moduleRegistry.register({
      name: 'testRemote',
      entry: 'http://localhost:3003/remoteEntry.js',
      exposedModule: './TestApp',
      displayName: 'Test Remote Module',
      route: '/test-module',
      enabled: true,
      description: 'A test module for verifying Module Federation',
      version: '1.0.0',
    });
    
    setModuleRegistered(true);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Module Federation Test
      </Typography>
      
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Test Module Loading
        </Typography>
        
        <Stack spacing={2}>
          <Alert severity="info">
            This page tests the Module Federation remote loading capabilities.
            Make sure the test remote module is running on port 3003.
          </Alert>
          
          {!moduleRegistered && (
            <Button
              variant="contained"
              onClick={registerTestModule}
            >
              Register Test Module
            </Button>
          )}
          
          {moduleRegistered && !showModule && (
            <Button
              variant="contained"
              color="success"
              onClick={() => setShowModule(true)}
            >
              Load Remote Module
            </Button>
          )}
          
          {moduleRegistered && (
            <Alert severity="success">
              Module registered successfully! You can now load it.
            </Alert>
          )}
        </Stack>
      </Paper>
      
      {showModule && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Remote Module Content
          </Typography>
          
          <RemoteModule
            moduleName="testRemote"
            props={{ message: 'Custom message from host!' }}
            onLoad={() => console.log('Test module loaded successfully')}
            onError={(error) => console.error('Failed to load test module:', error)}
          />
        </Paper>
      )}
    </Box>
  );
};

export default TestModulePage;