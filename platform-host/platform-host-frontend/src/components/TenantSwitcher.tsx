import React, { useState } from 'react';
import {
  Button,
  Menu,
  MenuItem,
  Box,
  Typography,
  Divider,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Business,
  AdminPanelSettings,
  ExpandMore,
  SwapHoriz,
} from '@mui/icons-material';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from '@modern-js/runtime/router';

export const TenantSwitcher: React.FC = () => {
  const { selectedTenant, clearTenant } = useAuth();
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleSwitchTenant = async () => {
    handleClose();
    try {
      await clearTenant();
      navigate('/tenant-selection');
    } catch (error) {
      console.error('Failed to switch tenant:', error);
    }
  };

  if (!selectedTenant) {
    return null;
  }

  return (
    <Box>
      <Button
        color="inherit"
        onClick={handleClick}
        startIcon={
          selectedTenant.isPlatformAdmin ? (
            <AdminPanelSettings />
          ) : (
            <Business />
          )
        }
        endIcon={<ExpandMore />}
        sx={{ textTransform: 'none' }}
      >
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 500 }}>
            {selectedTenant.name}
          </Typography>
          {selectedTenant.isPlatformAdmin && (
            <Typography variant="caption" color="primary.light">
              Platform Admin
            </Typography>
          )}
        </Box>
      </Button>
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
      >
        <MenuItem disabled>
          <ListItemText
            primary="Current Organization"
            secondary={selectedTenant.name}
          />
        </MenuItem>
        <Divider />
        <MenuItem onClick={handleSwitchTenant}>
          <ListItemIcon>
            <SwapHoriz fontSize="small" />
          </ListItemIcon>
          <ListItemText primary="Switch Organization" />
        </MenuItem>
      </Menu>
    </Box>
  );
};

export default TenantSwitcher;