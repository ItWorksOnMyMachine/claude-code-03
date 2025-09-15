import React from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Chip,
  Alert
} from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon, Add as AddIcon } from '@mui/icons-material';
import { Link } from '@modern-js/runtime/router';
import { useEntitlements, CmsEntitlements } from '../hooks/useEntitlements';

interface PlatformContext {
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

interface ContentListProps extends PlatformContext {}

const ContentList: React.FC<ContentListProps> = ({ tenantId, userId }) => {
  const { hasEntitlement, hasAnyEntitlement, isLoading: entitlementsLoading, error: entitlementError } = useEntitlements();

  // Mock data for initial development
  const mockContent = [
    { id: 1, title: 'Homepage Content', type: 'page', status: 'published', lastModified: '2025-09-11' },
    { id: 2, title: 'About Us Page', type: 'page', status: 'draft', lastModified: '2025-09-10' },
    { id: 3, title: 'Product Landing', type: 'template', status: 'published', lastModified: '2025-09-09' },
  ];

  // Check entitlements
  const canManageContent = hasEntitlement(CmsEntitlements.CMS_MANAGE);
  const canAccessAssets = hasEntitlement(CmsEntitlements.CMS_ASSETS);
  const canManageTemplates = hasEntitlement(CmsEntitlements.CMS_TEMPLATES);

  if (entitlementsLoading) {
    return (
      <Box sx={{ p: 2 }}>
        <Typography>Loading permissions...</Typography>
      </Box>
    );
  }

  if (entitlementError) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        Failed to load permissions: {entitlementError}
      </Alert>
    );
  }

  if (!hasEntitlement(CmsEntitlements.CMS_ACCESS)) {
    return (
      <Alert severity="warning" sx={{ m: 2 }}>
        You don't have permission to access the CMS module. Contact your administrator for access.
      </Alert>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1">
          Content Management
        </Typography>
        {canManageContent && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            component={Link}
            to="/content/new"
            sx={{ ml: 'auto' }}
          >
            Create New Content
          </Button>
        )}
      </Box>

      {tenantId && (
        <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
          Tenant: {tenantId} | User: {userId}
        </Typography>
      )}

      <Paper sx={{ width: '100%' }}>
        <List>
          {mockContent.map((item) => (
            <ListItem key={item.id} divider>
              <ListItemText
                primary={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="h6">{item.title}</Typography>
                    <Chip 
                      label={item.status} 
                      color={item.status === 'published' ? 'success' : 'default'}
                      size="small"
                    />
                  </Box>
                }
                secondary={
                  <Box>
                    <Typography variant="body2" color="textSecondary">
                      Type: {item.type} • Last modified: {item.lastModified}
                    </Typography>
                  </Box>
                }
              />
              <ListItemSecondaryAction>
                {canManageContent && (
                  <IconButton
                    edge="end"
                    aria-label="edit"
                    component={Link}
                    to={`/content/edit/${item.id}`}
                    sx={{ mr: 1 }}
                  >
                    <EditIcon />
                  </IconButton>
                )}
                {canManageContent && (
                  <IconButton
                    edge="end"
                    aria-label="delete"
                    color="error"
                  >
                    <DeleteIcon />
                  </IconButton>
                )}
              </ListItemSecondaryAction>
            </ListItem>
          ))}
        </List>
      </Paper>
    </Box>
  );
};

export default ContentList;