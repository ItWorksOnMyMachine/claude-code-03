import { CmsApiService } from '../services/CmsApiService';

// Mock fetch
global.fetch = jest.fn();

describe('CmsApiService', () => {
  let apiService: CmsApiService;
  const mockContext = {
    authToken: 'test-token',
    tenantId: 'test-tenant',
    userId: 'test-user',
  };

  beforeEach(() => {
    apiService = new CmsApiService(mockContext);
    jest.clearAllMocks();
  });

  describe('Content Operations', () => {
    test('should create content with correct headers and data', async () => {
      const mockContent = {
        title: 'Test Content',
        slug: 'test-content',
        content: '<div>Test</div>',
        contentType: 'page',
        status: 'draft',
      };

      const mockResponse = { ...mockContent, id: '123' };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse),
      });

      const result = await apiService.createContent(mockContent);

      expect(fetch).toHaveBeenCalledWith('/api/cms/content', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
        body: JSON.stringify(mockContent),
      });

      expect(result).toEqual(mockResponse);
    });

    test('should get content by ID', async () => {
      const mockContent = {
        id: '123',
        title: 'Test Content',
        content: '<div>Test</div>',
        contentType: 'page',
      };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContent),
      });

      const result = await apiService.getContent('123');

      expect(fetch).toHaveBeenCalledWith('/api/cms/content/123', {
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
      });

      expect(result).toEqual(mockContent);
    });

    test('should return null when content not found', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 404,
      });

      const result = await apiService.getContent('nonexistent');

      expect(result).toBeNull();
    });

    test('should update existing content', async () => {
      const mockUpdatedContent = {
        id: '123',
        title: 'Updated Content',
        content: '<div>Updated</div>',
      };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockUpdatedContent),
      });

      const result = await apiService.updateContent('123', { title: 'Updated Content' });

      expect(fetch).toHaveBeenCalledWith('/api/cms/content/123', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
        body: JSON.stringify({ title: 'Updated Content', id: '123' }),
      });

      expect(result).toEqual(mockUpdatedContent);
    });

    test('should delete content', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
      });

      await apiService.deleteContent('123');

      expect(fetch).toHaveBeenCalledWith('/api/cms/content/123', {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
      });
    });

    test('should get all content', async () => {
      const mockContentList = [
        { id: '1', title: 'Content 1' },
        { id: '2', title: 'Content 2' },
      ];

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockContentList),
      });

      const result = await apiService.getAllContent();

      expect(fetch).toHaveBeenCalledWith('/api/cms/content', {
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
      });

      expect(result).toEqual(mockContentList);
    });
  });

  describe('Page Operations', () => {
    test('should create page with correct data', async () => {
      const mockPage = {
        title: 'Test Page',
        slug: 'test-page',
        metaTitle: 'Test Page',
        status: 'Draft' as const,
        metadata: '{}',
        sortOrder: 0,
        showInNavigation: true,
      };

      const mockResponse = { ...mockPage, id: '123' };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse),
      });

      const result = await apiService.createPage(mockPage);

      expect(fetch).toHaveBeenCalledWith('/api/cms/pages', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
        body: JSON.stringify(mockPage),
      });

      expect(result).toEqual(mockResponse);
    });

    test('should get page by ID', async () => {
      const mockPage = {
        id: '123',
        title: 'Test Page',
        slug: 'test-page',
        status: 'Published',
      };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockPage),
      });

      const result = await apiService.getPage('123');

      expect(result).toEqual(mockPage);
    });
  });

  describe('Asset Operations', () => {
    test('should upload asset file', async () => {
      const mockFile = new File(['test content'], 'test.jpg', { type: 'image/jpeg' });
      const mockResponse = { id: '123', fileName: 'test.jpg', url: '/uploads/test.jpg' };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockResponse),
      });

      const result = await apiService.uploadAsset(mockFile);

      expect(fetch).toHaveBeenCalledWith('/api/cms/assets/upload', {
        method: 'POST',
        headers: {
          'Authorization': 'Bearer test-token',
          'X-Tenant-Id': 'test-tenant',
        },
        body: expect.any(FormData),
      });

      expect(result).toEqual(mockResponse);
    });

    test('should get all assets', async () => {
      const mockAssets = [
        { id: '1', fileName: 'image1.jpg' },
        { id: '2', fileName: 'image2.png' },
      ];

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(mockAssets),
      });

      const result = await apiService.getAllAssets();

      expect(result).toEqual(mockAssets);
    });
  });

  describe('Error Handling', () => {
    test('should throw error when API request fails', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        statusText: 'Internal Server Error',
      });

      await expect(apiService.getAllContent()).rejects.toThrow('Failed to get content list: Internal Server Error');
    });

    test('should handle network errors', async () => {
      (fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      await expect(apiService.getAllContent()).rejects.toThrow('Network error');
    });
  });

  describe('Authentication Context', () => {
    test('should work without authentication token', () => {
      const serviceWithoutAuth = new CmsApiService({ tenantId: 'test-tenant' });

      expect(serviceWithoutAuth).toBeInstanceOf(CmsApiService);
    });

    test('should include correct headers based on context', async () => {
      const serviceWithMinimalContext = new CmsApiService({ tenantId: 'test-tenant' });

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve([]),
      });

      await serviceWithMinimalContext.getAllContent();

      expect(fetch).toHaveBeenCalledWith('/api/cms/content', {
        headers: {
          'Content-Type': 'application/json',
          'X-Tenant-Id': 'test-tenant',
        },
      });
    });
  });
});