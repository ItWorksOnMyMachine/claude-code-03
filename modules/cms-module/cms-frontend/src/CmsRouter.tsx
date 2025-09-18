import React, { lazy } from 'react';
import { Box } from '@mui/material';

// Lazy load CMS components
const ContentList = lazy(() => import('./components/ContentList'));
const ContentEditor = lazy(() => import('./components/ContentEditor'));
const AssetManager = lazy(() => import('./components/AssetManager'));

interface CmsRouterProps {
  authToken?: string;
  tenantId?: string;
  userId?: string;
  currentPath?: string; // Path will be passed from the host
  onNavigate?: (path: string) => void; // Navigation handler from the host
}

const CmsRouter: React.FC<CmsRouterProps> = ({
  authToken,
  tenantId,
  userId,
  currentPath = '/cms/content',
  onNavigate
}) => {
  // Pass platform context to all CMS components
  const platformContext = {
    authToken,
    tenantId,
    userId,
    onNavigate, // Pass navigation handler to components
  };

  // Render the appropriate component based on the path
  const renderContent = () => {
    const pathname = currentPath;

    if (pathname === '/cms' || pathname === '/cms/') {
      // Default to content list
      return <ContentList {...platformContext} />;
    }

    if (pathname === '/cms/content' || pathname === '/cms/content/') {
      return <ContentList {...platformContext} />;
    }

    if (pathname === '/cms/content/new') {
      return <ContentEditor {...platformContext} />;
    }

    if (pathname.startsWith('/cms/content/edit/')) {
      // Extract the ID from the path
      const id = pathname.replace('/cms/content/edit/', '');
      return <ContentEditor {...platformContext} contentId={id} />;
    }

    if (pathname === '/cms/assets' || pathname === '/cms/assets/') {
      return <AssetManager {...platformContext} />;
    }

    // Default to content list for any unmatched routes
    return <ContentList {...platformContext} />;
  };

  return (
    <Box sx={{ height: '100%', overflow: 'hidden' }}>
      {renderContent()}
    </Box>
  );
};

export default CmsRouter;