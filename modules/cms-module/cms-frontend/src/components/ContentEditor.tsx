import React, { useRef, useEffect, useState, useCallback } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  TextField,
  Grid,
  Toolbar,
  Alert,
  Snackbar
} from '@mui/material';
import { Save as SaveIcon, ArrowBack as BackIcon } from '@mui/icons-material';
import { Link, useParams } from '@modern-js/runtime/router';
import grapesjs from 'grapesjs';
import 'grapesjs/dist/css/grapes.min.css';
import { CmsApiService } from '../services/CmsApiService';

interface PlatformContext {
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

interface ContentEditorProps extends PlatformContext {
  contentId?: string;
}

const ContentEditor: React.FC<ContentEditorProps> = ({ tenantId, authToken, userId, contentId }) => {
  const { id } = useParams();
  // Use the passed contentId if available, otherwise use the id from params
  const editId = contentId || id;
  const editorRef = useRef<HTMLDivElement>(null);
  const editorInstance = useRef<any>(null);
  const isEditMode = Boolean(editId);

  // State management
  const [title, setTitle] = useState('');
  const [contentType, setContentType] = useState('page');
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  // API service
  const apiService = useRef(new CmsApiService({ authToken, tenantId, userId }));

  useEffect(() => {
    if (editorRef.current && !editorInstance.current) {
      // Initialize GrapesJS editor
      editorInstance.current = (grapesjs as any).init({
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
      if (isEditMode && editId) {
        loadContent(editId);
      }
    }

    return () => {
      // Cleanup editor instance on unmount
      if (editorInstance.current) {
        editorInstance.current.destroy();
        editorInstance.current = null;
      }
    };
  }, [editId, isEditMode, tenantId, authToken]);

  // Load content from API
  const loadContent = useCallback(async (contentId: string) => {
    try {
      setIsLoading(true);
      const content = await apiService.current.getContent(contentId);

      if (content && editorInstance.current) {
        setTitle(content.title);
        setContentType(content.contentType);

        // Load HTML/CSS into GrapesJS
        editorInstance.current.setComponents(content.content);

        setMessage({ text: 'Content loaded successfully', type: 'success' });
      }
    } catch (error) {
      console.error('Failed to load content:', error);
      setMessage({ text: 'Failed to load content', type: 'error' });
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Save content to API
  const handleSave = useCallback(async () => {
    if (!editorInstance.current || !title.trim()) {
      setMessage({ text: 'Please enter a title', type: 'error' });
      return;
    }

    try {
      setIsSaving(true);
      const html = editorInstance.current.getHtml();
      const css = editorInstance.current.getCss();
      const combinedContent = html + (css ? `<style>${css}</style>` : '');

      const contentData = {
        title,
        slug: title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''),
        content: combinedContent,
        contentType,
        status: 'draft',
      };

      if (isEditMode && editId) {
        await apiService.current.updateContent(editId, contentData);
        setMessage({ text: 'Content updated successfully', type: 'success' });
      } else {
        await apiService.current.createContent(contentData);
        setMessage({ text: 'Content created successfully', type: 'success' });
      }

      setLastSaved(new Date());
    } catch (error) {
      console.error('Failed to save content:', error);
      setMessage({ text: 'Failed to save content', type: 'error' });
    } finally {
      setIsSaving(false);
    }
  }, [title, contentType, isEditMode, editId]);

  // Auto-save functionality with debouncing (subtask 6.7)
  useEffect(() => {
    if (!editorInstance.current || !title.trim()) return;

    const autoSaveInterval = setInterval(async () => {
      if (!isSaving && editorInstance.current) {
        try {
          setIsSaving(true);
          const html = editorInstance.current.getHtml();
          const css = editorInstance.current.getCss();
          const combinedContent = html + (css ? `<style>${css}</style>` : '');

          const contentData = {
            title,
            slug: title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''),
            content: combinedContent,
            contentType,
            status: 'draft',
          };

          if (isEditMode && editId) {
            await apiService.current.updateContent(editId, contentData);
            setLastSaved(new Date());
          }
        } catch (error) {
          console.error('Auto-save failed:', error);
        } finally {
          setIsSaving(false);
        }
      }
    }, 30000); // Auto-save every 30 seconds

    return () => clearInterval(autoSaveInterval);
  }, [title, contentType, isEditMode, editId, isSaving]);

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
          <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
            <Typography variant="h5" component="h1">
              {isEditMode ? 'Edit Content' : 'Create New Content'}
            </Typography>
            {lastSaved && (
              <Typography variant="caption" color="textSecondary">
                Last saved: {lastSaved.toLocaleTimeString()}
              </Typography>
            )}
          </Box>
          <Button
            variant="contained"
            startIcon={<SaveIcon />}
            onClick={handleSave}
            disabled={isSaving || isLoading}
          >
            {isSaving ? 'Saving...' : 'Save'}
          </Button>
        </Toolbar>
        
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              fullWidth
              label="Content Title"
              variant="outlined"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={isLoading}
            />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              fullWidth
              label="Content Type"
              variant="outlined"
              select
              SelectProps={{
                native: true,
              }}
              value={contentType}
              onChange={(e) => setContentType(e.target.value)}
              disabled={isLoading}
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

      {/* Message snackbar */}
      <Snackbar
        open={Boolean(message)}
        autoHideDuration={6000}
        onClose={() => setMessage(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert
          onClose={() => setMessage(null)}
          severity={message?.type || 'info'}
          sx={{ width: '100%' }}
        >
          {message?.text}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default ContentEditor;