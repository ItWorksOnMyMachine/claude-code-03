import React from 'react';
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Box,
  Divider,
} from '@mui/material';
import { Link, useLocation } from '@modern-js/runtime/router';
import {
  Home,
  FileText,
  Settings,
  Package,
  LayoutGrid,
} from 'lucide-react';

export interface NavigationItem {
  name: string;
  path: string;
  icon?: string;
  enabled?: boolean;
}

export interface SidebarProps {
  open: boolean;
  width?: number;
  navigationItems?: NavigationItem[];
}

const iconMap: Record<string, React.ReactNode> = {
  Home: <Home size={20} />,
  Description: <FileText size={20} />,
  Settings: <Settings size={20} />,
  Package: <Package size={20} />,
  LayoutGrid: <LayoutGrid size={20} />,
};

export const Sidebar: React.FC<SidebarProps> = ({
  open,
  width = 240,
  navigationItems = [],
}) => {
  const location = useLocation();

  const defaultItems: NavigationItem[] = [
    { name: 'Dashboard', path: '/', icon: 'Home', enabled: true },
    { name: 'Modules', path: '/modules', icon: 'LayoutGrid', enabled: true },
    { name: 'Settings', path: '/settings', icon: 'Settings', enabled: true },
  ];

  const items = navigationItems.length > 0 ? navigationItems : defaultItems;

  return (
    <Drawer
      variant="persistent"
      anchor="left"
      open={open}
      sx={{
        width: open ? width : 0,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width,
          boxSizing: 'border-box',
          borderRight: 1,
          borderColor: 'divider',
        },
      }}
    >
      <Toolbar />
      <Box sx={{ overflow: 'auto' }}>
        <List>
          {items.map((item) => {
            if (item.enabled === false) return null;
            
            const isActive = location.pathname === item.path;
            
            return (
              <ListItem key={item.path} disablePadding>
                <ListItemButton
                  component={Link}
                  to={item.path}
                  selected={isActive}
                  sx={{
                    '&.Mui-selected': {
                      backgroundColor: 'action.selected',
                      '&:hover': {
                        backgroundColor: 'action.selected',
                      },
                    },
                  }}
                >
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    {item.icon && iconMap[item.icon] ? iconMap[item.icon] : <Package size={20} />}
                  </ListItemIcon>
                  <ListItemText primary={item.name} />
                </ListItemButton>
              </ListItem>
            );
          })}
        </List>
        <Divider />
      </Box>
    </Drawer>
  );
};