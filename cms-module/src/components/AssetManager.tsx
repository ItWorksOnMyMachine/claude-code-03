import React, { useCallback, useState } from 'react';
import { 
  Box, 
  Typography, 
  Paper, 
  Grid,
  Card,
  CardMedia,
  CardContent,
  CardActions,
  Button,
  IconButton,
  Alert
} from '@mui/material';
import { 
  CloudUpload as UploadIcon, 
  Delete as DeleteIcon,
  GetApp as DownloadIcon 
} from '@mui/icons-material';
import { useDropzone } from 'react-dropzone';

interface PlatformContext {
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

interface AssetManagerProps extends PlatformContext {}

interface Asset {
  id: string;
  name: string;
  type: string;
  size: number;
  url: string;
  thumbnailUrl?: string;
  uploadDate: string;
}

const AssetManager: React.FC<AssetManagerProps> = ({ tenantId, authToken }) => {
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  
  // Mock assets data
  const [assets] = useState<Asset[]>([
    {
      id: '1',
      name: 'hero-image.jpg',
      type: 'image/jpeg',
      size: 1024000,
      url: '/assets/hero-image.jpg',
      thumbnailUrl: '/assets/thumbnails/hero-image.jpg',
      uploadDate: '2025-09-11',
    },
    {
      id: '2',
      name: 'logo.png',
      type: 'image/png',
      size: 52000,
      url: '/assets/logo.png',
      thumbnailUrl: '/assets/thumbnails/logo.png',
      uploadDate: '2025-09-10',
    },
    {
      id: '3',
      name: 'document.pdf',
      type: 'application/pdf',
      size: 256000,
      url: '/assets/document.pdf',
      uploadDate: '2025-09-09',
    },
  ]);

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    setUploading(true);
    setUploadError(null);

    try {
      const formData = new FormData();
      acceptedFiles.forEach((file) => {
        formData.append('files', file);
      });

      // Add tenant context
      if (tenantId) {
        formData.append('tenantId', tenantId);
      }

      console.log('Uploading files:', acceptedFiles.map(f => f.name));
      
      // This would typically upload to an API
      // const response = await fetch('/api/cms/assets/upload', {
      //   method: 'POST',
      //   headers: {
      //     'Authorization': authToken ? `Bearer ${authToken}` : '',
      //   },
      //   body: formData,
      // });

      // Mock successful upload
      await new Promise(resolve => setTimeout(resolve, 1000));
      
    } catch (error) {
      setUploadError('Failed to upload files. Please try again.');
      console.error('Upload error:', error);
    } finally {
      setUploading(false);
    }
  }, [tenantId, authToken]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'image/*': ['.png', '.jpg', '.jpeg', '.gif', '.svg'],
      'application/pdf': ['.pdf'],
      'text/*': ['.txt', '.md'],
    },
    maxSize: 10485760, // 10MB
  });

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const handleDelete = (assetId: string) => {
    console.log('Deleting asset:', assetId);
    // This would typically call an API to delete the asset
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Asset Manager
      </Typography>
      
      {tenantId && (
        <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
          Managing assets for tenant: {tenantId}
        </Typography>
      )}

      {/* Upload Area */}
      <Paper
        {...getRootProps()}
        sx={{
          p: 4,
          mb: 3,
          border: '2px dashed',
          borderColor: isDragActive ? 'primary.main' : 'grey.300',
          backgroundColor: isDragActive ? 'primary.50' : 'background.paper',
          cursor: 'pointer',
          transition: 'all 0.2s ease',
          '&:hover': {
            borderColor: 'primary.main',
            backgroundColor: 'primary.50',
          },
        }}
      >
        <input {...getInputProps()} />
        <Box sx={{ textAlign: 'center' }}>
          <UploadIcon sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />
          <Typography variant="h6" gutterBottom>
            {isDragActive ? 'Drop files here' : 'Drag & drop files here'}
          </Typography>
          <Typography variant="body2" color="textSecondary">
            or click to select files (Images, PDFs, Text files - Max 10MB)
          </Typography>
          {uploading && (
            <Typography variant="body2" color="primary" sx={{ mt: 1 }}>
              Uploading...
            </Typography>
          )}
        </Box>
      </Paper>

      {uploadError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setUploadError(null)}>
          {uploadError}
        </Alert>
      )}

      {/* Assets Grid */}
      <Grid container spacing={2}>
        {assets.map((asset) => (
          <Grid item xs={12} sm={6} md={4} lg={3} key={asset.id}>
            <Card>
              {asset.type.startsWith('image/') ? (
                <CardMedia
                  component="img"
                  height="140"
                  image={asset.thumbnailUrl || asset.url}
                  alt={asset.name}
                  sx={{ objectFit: 'cover' }}
                />
              ) : (
                <Box
                  sx={{
                    height: 140,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    backgroundColor: 'grey.100',
                  }}
                >
                  <Typography variant="h6" color="textSecondary">
                    {asset.type.split('/')[1].toUpperCase()}
                  </Typography>
                </Box>
              )}
              <CardContent>
                <Typography variant="subtitle2" noWrap title={asset.name}>
                  {asset.name}
                </Typography>
                <Typography variant="caption" color="textSecondary">
                  {formatFileSize(asset.size)} • {asset.uploadDate}
                </Typography>
              </CardContent>
              <CardActions>
                <IconButton 
                  size="small" 
                  onClick={() => window.open(asset.url, '_blank')}
                  title="Download"
                >
                  <DownloadIcon />
                </IconButton>
                <IconButton 
                  size="small" 
                  color="error"
                  onClick={() => handleDelete(asset.id)}
                  title="Delete"
                >
                  <DeleteIcon />
                </IconButton>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default AssetManager;