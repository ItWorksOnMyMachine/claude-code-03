import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  CircularProgress,
  Alert,
  Chip,
  Divider,
} from '@mui/material';
import { Business, AdminPanelSettings } from '@mui/icons-material';

interface Tenant {
  id: string;
  name: string;
  description?: string;
  isPlatformTenant?: boolean;
}

interface TenantSelectorProps {
  onTenantSelect: (tenantId: string) => void;
  currentTenantId?: string;
  showHeader?: boolean;
}

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

export const TenantSelector: React.FC<TenantSelectorProps> = ({
  onTenantSelect,
  currentTenantId,
  showHeader = true,
}) => {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selecting, setSelecting] = useState<string | null>(null);

  // Fetch available tenants
  useEffect(() => {
    const fetchTenants = async () => {
      try {
        setLoading(true);
        setError(null);
        
        const response = await fetch(`${API_BASE_URL}/api/tenant/available`, {
          method: 'GET',
          credentials: 'include',
          headers: {
            'Content-Type': 'application/json',
          },
        });

        if (!response.ok) {
          throw new Error('Failed to fetch available tenants');
        }

        const data = await response.json();
        setTenants(data.tenants || []);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load tenants');
      } finally {
        setLoading(false);
      }
    };

    fetchTenants();
  }, []);

  const handleTenantSelect = async (tenantId: string) => {
    try {
      setSelecting(tenantId);
      setError(null);

      const response = await fetch(`${API_BASE_URL}/api/tenant/select`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ tenantId }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to select tenant');
      }

      // Call the parent callback
      onTenantSelect(tenantId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to select tenant');
      setSelecting(null);
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box p={2}>
        <Alert severity="error">{error}</Alert>
      </Box>
    );
  }

  if (tenants.length === 0) {
    return (
      <Box p={2}>
        <Alert severity="info">
          No tenants available. Please contact your administrator.
        </Alert>
      </Box>
    );
  }

  return (
    <Card>
      {showHeader && (
        <CardContent>
          <Typography variant="h5" component="h2" gutterBottom>
            Select Organization
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Choose the organization you want to access
          </Typography>
        </CardContent>
      )}
      <Divider />
      <List>
        {tenants.map((tenant) => (
          <ListItem key={tenant.id} disablePadding>
            <ListItemButton
              onClick={() => handleTenantSelect(tenant.id)}
              selected={currentTenantId === tenant.id}
              disabled={selecting !== null}
            >
              <ListItemText
                primary={
                  <Box display="flex" alignItems="center" gap={1}>
                    {tenant.isPlatformTenant ? (
                      <AdminPanelSettings fontSize="small" />
                    ) : (
                      <Business fontSize="small" />
                    )}
                    <Typography variant="body1">{tenant.name}</Typography>
                    {tenant.isPlatformTenant && (
                      <Chip label="Platform Admin" size="small" color="primary" />
                    )}
                  </Box>
                }
                secondary={tenant.description}
              />
              {selecting === tenant.id && (
                <CircularProgress size={24} sx={{ ml: 2 }} />
              )}
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </Card>
  );
};

export default TenantSelector;