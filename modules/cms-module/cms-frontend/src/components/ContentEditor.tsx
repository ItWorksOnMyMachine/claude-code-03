import React, { useRef, useEffect } from 'react';
import { 
  Box, 
  Typography, 
  Paper, 
  Button,
  TextField,
  Grid,
  Toolbar,
  Alert
} from '@mui/material';
import { Save as SaveIcon, ArrowBack as BackIcon } from '@mui/icons-material';
import { Link, useParams } from '@modern-js/runtime/router';
import grapesjs from 'grapesjs';
import 'grapesjs/dist/css/grapes.min.css';

interface PlatformContext {
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

interface ContentEditorProps extends PlatformContext {}

const ContentEditor: React.FC<ContentEditorProps> = ({ tenantId, authToken }) => {
  const { id } = useParams();
  const editorRef = useRef<HTMLDivElement>(null);
  const editorInstance = useRef<any>(null);
  const isEditMode = Boolean(id);

  useEffect(() => {
    if (editorRef.current && !editorInstance.current) {
      // Initialize GrapesJS editor
      editorInstance.current = grapesjs.init({
        container: editorRef.current,
        height: '500px',
        width: '100%',
        storageManager: {
          type: 'remote',
          urlStore: '/api/cms/content/store',
          urlLoad: '/api/cms/content/load',
          headers: {
            'Authorization': authToken ? `Bearer ${authToken}` : '',
            'X-Tenant-Id': tenantId || '',
          },
        },
        assetManager: {
          upload: '/api/cms/assets/upload',
          uploadName: 'files',
          headers: {
            'Authorization': authToken ? `Bearer ${authToken}` : '',
            'X-Tenant-Id': tenantId || '',
          },
        },
        plugins: [
          'gjs-blocks-basic',
          'gjs-preset-webpage',
          'gjs-plugin-forms',
        ],
        pluginsOpts: {
          'gjs-blocks-basic': { flexGrid: true },
          'gjs-preset-webpage': {
            modalImportTitle: 'Import Template',
            modalImportLabel: '<div style="margin-bottom: 10px; font-size: 13px;">Paste here your HTML/CSS and click Import</div>',
            modalImportContent: function(editor: any) {
              return editor.getHtml() + '<style>' + editor.getCss() + '</style>';
            },
          },
          'gjs-plugin-forms': {},
        },
        canvas: {
          styles: [
            'https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/css/bootstrap.min.css'
          ],
          scripts: [
            'https://code.jquery.com/jquery-3.3.1.slim.min.js',
            'https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/js/bootstrap.min.js'
          ],
        },
      });

      // Load content if in edit mode
      if (isEditMode && id) {
        // This would typically load from an API
        console.log(`Loading content for ID: ${id}`);
      }
    }

    return () => {
      // Cleanup editor instance on unmount
      if (editorInstance.current) {
        editorInstance.current.destroy();
        editorInstance.current = null;
      }
    };
  }, [id, isEditMode, tenantId, authToken]);

  const handleSave = () => {
    if (editorInstance.current) {
      const html = editorInstance.current.getHtml();
      const css = editorInstance.current.getCss();
      const data = {
        html,
        css,
        tenantId,
        ...(isEditMode && { id }),
      };
      
      console.log('Saving content:', data);
      // This would typically save to an API
    }
  };

  return (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Paper sx={{ p: 2, mb: 2 }}>
        <Toolbar sx={{ px: 0 }}>
          <Button
            startIcon={<BackIcon />}
            component={Link}
            to="/content"
            sx={{ mr: 2 }}
          >
            Back to Content
          </Button>
          <Typography variant="h5" component="h1" sx={{ flexGrow: 1 }}>
            {isEditMode ? 'Edit Content' : 'Create New Content'}
          </Typography>
          <Button
            variant="contained"
            startIcon={<SaveIcon />}
            onClick={handleSave}
          >
            Save
          </Button>
        </Toolbar>
        
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Content Title"
              variant="outlined"
              defaultValue={isEditMode ? `Content ${id}` : ''}
            />
          </Grid>
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Content Type"
              variant="outlined"
              select
              SelectProps={{
                native: true,
              }}
              defaultValue="page"
            >
              <option value="page">Page</option>
              <option value="template">Template</option>
              <option value="component">Component</option>
            </TextField>
          </Grid>
        </Grid>
      </Paper>

      {tenantId && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Editing content for tenant: {tenantId}
        </Alert>
      )}

      <Paper sx={{ flex: 1, overflow: 'hidden' }}>
        <div 
          ref={editorRef} 
          style={{ 
            height: '100%', 
            width: '100%',
            minHeight: '500px'
          }} 
        />
      </Paper>
    </Box>
  );
};

export default ContentEditor;