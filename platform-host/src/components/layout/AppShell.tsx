import React, { useState } from 'react';
import { Box, Toolbar } from '@mui/material';
import { Header } from './Header';
import { Sidebar, NavigationItem } from './Sidebar';

export interface AppShellProps {
  children: React.ReactNode;
  navigationItems?: NavigationItem[];
}

export const AppShell: React.FC<AppShellProps> = ({ 
  children,
  navigationItems = [],
}) => {
  const [sidebarOpen, setSidebarOpen] = useState(true);

  const handleDrawerToggle = () => {
    setSidebarOpen(!sidebarOpen);
  };

  const drawerWidth = 240;

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Header onMenuClick={handleDrawerToggle} />
      
      <Sidebar 
        open={sidebarOpen}
        width={drawerWidth}
        navigationItems={navigationItems}
      />

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          marginLeft: sidebarOpen ? `${drawerWidth}px` : 0,
          transition: (theme) => theme.transitions.create(['margin'], {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.leavingScreen,
          }),
          backgroundColor: 'background.default',
        }}
      >
        <Toolbar />
        {children}
      </Box>
    </Box>
  );
};