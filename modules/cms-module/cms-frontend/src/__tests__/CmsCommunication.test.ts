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
  let cmsCommunication: CmsCommunication;
  let mockModuleCommunication: any;

  beforeEach(() => {
    // Reset the singleton instance for each test
    (CmsCommunication as any).instance = undefined;
    cmsCommunication = CmsCommunication.getInstance();
    mockModuleCommunication = ModuleCommunication.getInstance();

    // Clear all mock calls
    jest.clearAllMocks();
  });

  test('should create singleton instance', () => {
    const instance1 = CmsCommunication.getInstance();
    const instance2 = CmsCommunication.getInstance();
    expect(instance1).toBe(instance2);
  });

  test('should initialize with cmsModule identifier', () => {
    expect(ModuleCommunication.getInstance).toHaveBeenCalledWith('cmsModule');
  });

  test('should set up event handlers during initialization', () => {
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

  test('should notify platform when module is ready', () => {
    cmsCommunication.notifyReady();

    expect(mockModuleCommunication.broadcast).toHaveBeenCalledWith(
      MessageTypes.MODULE_READY,
      {
        module: 'cmsModule',
        version: '1.0.0',
        capabilities: ['content-editing', 'asset-management', 'template-creation'],
      }
    );
  });

  test('should notify platform when content is saved', () => {
    const contentData = {
      id: 'content-123',
      type: 'page',
      title: 'Test Content',
    };

    cmsCommunication.notifyContentSaved(contentData);

    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.CONTENT_SAVED,
      'platform_host',
      {
        contentId: contentData.id,
        contentType: contentData.type,
        title: contentData.title,
        timestamp: expect.any(Number),
      }
    );
  });

  test('should notify platform when asset is uploaded', () => {
    const assetData = {
      id: 'asset-123',
      name: 'test-image.jpg',
      type: 'image/jpeg',
      size: 1024,
      url: '/uploads/test-image.jpg',
    };

    cmsCommunication.notifyAssetUploaded(assetData);

    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.ASSET_UPLOADED,
      'platform_host',
      {
        assetId: assetData.id,
        fileName: assetData.name,
        fileType: assetData.type,
        fileSize: assetData.size,
        url: assetData.url,
        timestamp: expect.any(Number),
      }
    );
  });

  test('should request navigation from platform', () => {
    const route = '/dashboard';
    const params = { section: 'analytics' };

    cmsCommunication.requestNavigation(route, params);

    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.NAVIGATE_TO,
      'platform_host',
      {
        route,
        params,
        source: 'cmsModule',
      }
    );
  });

  test('should show notification in platform', () => {
    cmsCommunication.showNotification('success', 'Content saved successfully!');

    expect(mockModuleCommunication.send).toHaveBeenCalledWith(
      MessageTypes.NOTIFICATION,
      'platform_host',
      {
        type: 'success',
        message: 'Content saved successfully!',
        source: 'CMS Module',
        timestamp: expect.any(Number),
      }
    );
  });

  test('should request user context from platform', async () => {
    const mockUserContext = { userId: 'user-123', name: 'John Doe' };
    mockModuleCommunication.request.mockResolvedValue(mockUserContext);

    const result = await cmsCommunication.requestUserContext();

    expect(mockModuleCommunication.request).toHaveBeenCalledWith(
      'GET_USER_CONTEXT',
      'platform_host',
      {},
      3000
    );
    expect(result).toEqual(mockUserContext);
  });

  test('should request tenant info from platform', async () => {
    const mockTenantInfo = { tenantId: 'tenant-123', name: 'Test Company' };
    mockModuleCommunication.request.mockResolvedValue(mockTenantInfo);

    const result = await cmsCommunication.requestTenantInfo();

    expect(mockModuleCommunication.request).toHaveBeenCalledWith(
      'GET_TENANT_INFO',
      'platform_host',
      {},
      3000
    );
    expect(result).toEqual(mockTenantInfo);
  });

  test('should handle request errors gracefully', async () => {
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();
    mockModuleCommunication.request.mockRejectedValue(new Error('Network error'));

    const result = await cmsCommunication.requestUserContext();

    expect(result).toBeNull();
    expect(consoleErrorSpy).toHaveBeenCalledWith('Failed to get user context:', expect.any(Error));

    consoleErrorSpy.mockRestore();
  });

  test('should provide pass-through methods', () => {
    const handler = jest.fn();
    const eventType = 'CUSTOM_EVENT';
    const target = 'other_module';
    const payload = { data: 'test' };

    // Test subscribe
    cmsCommunication.subscribe(eventType, handler);
    expect(mockModuleCommunication.subscribe).toHaveBeenCalledWith(eventType, handler);

    // Test send
    cmsCommunication.send(eventType, target, payload);
    expect(mockModuleCommunication.send).toHaveBeenCalledWith(eventType, target, payload);

    // Test broadcast
    cmsCommunication.broadcast(eventType, payload);
    expect(mockModuleCommunication.broadcast).toHaveBeenCalledWith(eventType, payload);

    // Test cleanup
    cmsCommunication.cleanup();
    expect(mockModuleCommunication.cleanup).toHaveBeenCalled();
  });

  test('should emit custom events for component updates', () => {
    // Mock window.dispatchEvent
    const dispatchEventSpy = jest.spyOn(window, 'dispatchEvent').mockImplementation();

    // Simulate user context update message
    const mockMessage = { payload: { userId: 'new-user' } };

    // Get the handler that was registered for USER_CONTEXT_UPDATED
    const userContextHandler = mockModuleCommunication.subscribe.mock.calls.find(
      call => call[0] === MessageTypes.USER_CONTEXT_UPDATED
    )?.[1];

    if (userContextHandler) {
      userContextHandler(mockMessage);

      expect(dispatchEventSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'cms-user-context-updated',
          detail: mockMessage.payload,
        })
      );
    }

    dispatchEventSpy.mockRestore();
  });
});