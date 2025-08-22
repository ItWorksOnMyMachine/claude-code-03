import React from 'react';
import { Box, Typography, Card, CardContent, Chip } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { ProtectedRoute } from '../../components/auth/ProtectedRoute';
import { useAuth } from '../../contexts/AuthContext';
import { Shield, User, Clock, Key } from 'lucide-react';

const DashboardPage: React.FC = () => {
  const { user } = useAuth();

  return (
    <ProtectedRoute>
      <Box sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Dashboard
        </Typography>
        
        <Typography variant="body1" color="text.secondary" paragraph>
          Welcome back, {user?.name || user?.email || 'User'}!
        </Typography>

        <Grid container spacing={3}>
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" mb={2}>
                  <User size={24} style={{ marginRight: 8 }} />
                  <Typography variant="h6">User Information</Typography>
                </Box>
                
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  <strong>ID:</strong> {user?.id}
                </Typography>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  <strong>Email:</strong> {user?.email || 'Not provided'}
                </Typography>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  <strong>Name:</strong> {user?.name || 'Not provided'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" mb={2}>
                  <Shield size={24} style={{ marginRight: 8 }} />
                  <Typography variant="h6">Security Status</Typography>
                </Box>
                
                <Box display="flex" gap={1} flexWrap="wrap">
                  <Chip 
                    label="Authenticated" 
                    color="success" 
                    size="small" 
                    icon={<Key size={16} />}
                  />
                  <Chip 
                    label="Session Active" 
                    color="primary" 
                    size="small"
                    icon={<Clock size={16} />}
                  />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {user?.claims && Object.keys(user.claims).length > 0 && (
            <Grid size={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    User Claims
                  </Typography>
                  
                  <Box display="flex" gap={1} flexWrap="wrap">
                    {Object.entries(user.claims).map(([key, value]) => (
                      <Chip
                        key={key}
                        label={`${key}: ${value}`}
                        variant="outlined"
                        size="small"
                      />
                    ))}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          )}
        </Grid>
      </Box>
    </ProtectedRoute>
  );
};

export default DashboardPage;