import React, { lazy } from 'react';
import { Routes, Route, Navigate } from '@modern-js/runtime/router';
import { Box } from '@mui/material';

// Lazy load CMS components
const ContentList = lazy(() => import('./components/ContentList'));
const ContentEditor = lazy(() => import('./components/ContentEditor'));
const AssetManager = lazy(() => import('./components/AssetManager'));

interface CmsRouterProps {
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

const CmsRouter: React.FC<CmsRouterProps> = ({ authToken, tenantId, userId }) => {
  // Pass platform context to all CMS components
  const platformContext = {
    authToken,
    tenantId,
    userId,
  };

  return (
    <Box sx={{ height: '100%', overflow: 'hidden' }}>
      <Routes>
        {/* Default route - redirect to content list */}
        <Route path="/" element={<Navigate to="/content" replace />} />
        
        {/* Content management routes */}
        <Route
          path="/content"
          element={<ContentList {...platformContext} />}
        />
        <Route
          path="/content/new"
          element={<ContentEditor {...platformContext} />}
        />
        <Route
          path="/content/edit/:id"
          element={<ContentEditor {...platformContext} />}
        />
        
        {/* Asset management route */}
        <Route
          path="/assets"
          element={<AssetManager {...platformContext} />}
        />

        {/* Catch-all route */}
        <Route path="*" element={<Navigate to="/content" replace />} />
      </Routes>
    </Box>
  );
};

export default CmsRouter;