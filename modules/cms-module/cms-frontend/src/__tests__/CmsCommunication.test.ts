import { CmsCommunication } from '../services/CmsCommunication';
import { ModuleCommunication, MessageTypes } from '../services/ModuleCommunication';

// Mock the ModuleCommunication
jest.mock('../services/ModuleCommunication', () => {
  const mockInstance = {
    subscribe: jest.fn(() => jest.fn()), // Return unsubscribe function
    broadcast: jest.fn(),
    send: jest.fn(),
    request: jest.fn(),
    cleanup: jest.fn(),
  };

  return {
    ModuleCommunication: {
      getInstance: jest.fn(() => mockInstance),
    },
    MessageTypes: {
      USER_CONTEXT_UPDATED: 'USER_CONTEXT_UPDATED',
      TENANT_CHANGED: 'TENANT_CHANGED',
      THEME_CHANGED: 'THEME_CHANGED',
      LOGOUT: 'LOGOUT',
      MODULE_READY: 'MODULE_READY',
      CONTENT_SAVED: 'CONTENT_SAVED',
      ASSET_UPLOADED: 'ASSET_UPLOADED',
      NAVIGATE_TO: 'NAVIGATE_TO',
      NOTIFICATION: 'NOTIFICATION',
    },
  };
});

describe('CmsCommunication', () => {
  let mockModuleCommunication: any;

  beforeEach(() => {
    jest.clearAllMocks();
    
    // Get the mock instance that will be returned
    mockModuleCommunication = {
      subscribe: jest.fn(() => jest.fn()),
      broadcast: jest.fn(),
      send: jest.fn(),
      request: jest.fn(),
      cleanup: jest.fn(),
    };
    
    (ModuleCommunication.getInstance as jest.Mock).mockReturnValue(mockModuleCommunication);
    
    // Clear any existing instances
    (CmsCommunication as any).instance = undefined;
  });

  test('should create singleton instance', () => {
    const instance1 = CmsCommunication.getInstance();
    const instance2 = CmsCommunication.getInstance();
    expect(instance1).toBe(instance2);
  });

  test('should initialize with cmsModule identifier', () => {
    CmsCommunication.getInstance();
    expect(ModuleCommunication.getInstance).toHaveBeenCalledWith('cmsModule');
  });

  test('should set up event handlers during initialization', () => {
    CmsCommunication.getInstance();
    
    // Verify that subscribe was called for each message type
    expect(mockModuleCommunication.subscribe).toHaveBeenCalledWith(
      MessageTypes.USER_CONTEXT_UPDATED,
      expect.any(Function)
    );
    expect(mockModuleCommunication.subscribe).toHaveBeenCalledWith(
      MessageTypes.TENANT_CHANGED,
      expect.any(Function)
    );
    expect(mockModuleCommunication.subscribe).toHaveBeenCalledWith(
      MessageTypes.THEME_CHANGED,
      expect.any(Function)
    );
    expect(mockModuleCommunication.subscribe).toHaveBeenCalledWith(
      MessageTypes.LOGOUT,
      expect.any(Function)
    );
  });

  test('should broadcast module ready event', () => {
    const cms = CmsCommunication.getInstance();
    
    cms.notifyReady();
    
    expect(mockModuleCommunication.broadcast).toHaveBeenCalledWith(
      MessageTypes.MODULE_READY,
      {
        module: 'cmsModule',
        version: '1.0.0',
        capabilities: ['content-editing', 'asset-management', 'template-creation'],
      }
    );
  });

  test('should send content saved notification', () => {
    const cms = CmsCommunication.getInstance();
    const contentData = { id: '123', title: 'Test Content', type: 'article' };
    
    cms.notifyContentSaved(contentData);
    
    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.CONTENT_SAVED,
      'platform_host',
      {
        contentId: '123',
        contentType: 'article',
        title: 'Test Content',
        timestamp: expect.any(Number),
      }
    );
  });

  test('should send asset uploaded notification', () => {
    const cms = CmsCommunication.getInstance();
    const assetData = { 
      id: '456', 
      name: 'test.jpg', 
      type: 'image/jpeg', 
      size: 1024,
      url: '/assets/test.jpg'
    };
    
    cms.notifyAssetUploaded(assetData);
    
    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.ASSET_UPLOADED,
      'platform_host',
      {
        assetId: '456',
        fileName: 'test.jpg',
        fileType: 'image/jpeg',
        fileSize: 1024,
        url: '/assets/test.jpg',
        timestamp: expect.any(Number),
      }
    );
  });

  test('should request navigation', () => {
    const cms = CmsCommunication.getInstance();
    
    cms.requestNavigation('/dashboard', { tab: 'analytics' });
    
    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.NAVIGATE_TO,
      'platform_host',
      {
        route: '/dashboard',
        params: { tab: 'analytics' },
        source: 'cmsModule',
      }
    );
  });

  test('should show notification', () => {
    const cms = CmsCommunication.getInstance();
    
    cms.showNotification('success', 'Content saved successfully');
    
    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.NOTIFICATION,
      'platform_host',
      {
        type: 'success',
        message: 'Content saved successfully',
        source: 'CMS Module',
        timestamp: expect.any(Number),
      }
    );
  });

  test('should request user context from host', async () => {
    const cms = CmsCommunication.getInstance();
    const mockUserContext = { userId: '123', permissions: ['read', 'write'] };
    
    mockModuleCommunication.request.mockResolvedValue(mockUserContext);
    
    const result = await cms.requestUserContext();
    
    expect(mockModuleCommunication.request).toHaveBeenCalledWith(
      'GET_USER_CONTEXT',
      'platform_host',
      {},
      3000
    );
    expect(result).toBe(mockUserContext);
  });

  test('should handle cleanup properly', () => {
    const cms = CmsCommunication.getInstance();
    
    cms.cleanup();
    
    expect(mockModuleCommunication.cleanup).toHaveBeenCalled();
  });
});
