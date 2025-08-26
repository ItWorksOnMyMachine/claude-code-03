import React from 'react';
import { Box, Typography } from '@mui/material';

export default function TestPage() {
  return (
    <Box p={3}>
      <Typography variant="h4">Test Page</Typography>
      <Typography>If you can see this, MUI is working!</Typography>
    </Box>
  );
}