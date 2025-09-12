import { ModuleCommunication, MessageTypes } from './ModuleCommunication';

/**
 * CMS-specific communication service that extends the base ModuleCommunication
 */
export class CmsCommunication {
  private communication: ModuleCommunication;
  private static instance: CmsCommunication;

  private constructor() {
    this.communication = ModuleCommunication.getInstance('cmsModule');
    this.setupCmsSpecificHandlers();
  }

  static getInstance(): CmsCommunication {
    if (!CmsCommunication.instance) {
      CmsCommunication.instance = new CmsCommunication();
    }
    return CmsCommunication.instance;
  }

  /**
   * Set up CMS-specific message handlers
   */
  private setupCmsSpecificHandlers(): void {
    // Handle user context updates from platform
    this.communication.subscribe(MessageTypes.USER_CONTEXT_UPDATED, (message) => {
      console.log('CMS received user context update:', message.payload);
      this.handleUserContextUpdate(message.payload);
    });

    // Handle tenant changes
    this.communication.subscribe(MessageTypes.TENANT_CHANGED, (message) => {
      console.log('CMS received tenant change:', message.payload);
      this.handleTenantChange(message.payload);
    });

    // Handle theme changes
    this.communication.subscribe(MessageTypes.THEME_CHANGED, (message) => {
      console.log('CMS received theme change:', message.payload);
      this.handleThemeChange(message.payload);
    });

    // Handle logout
    this.communication.subscribe(MessageTypes.LOGOUT, () => {
      console.log('CMS received logout signal');
      this.handleLogout();
    });
  }

  /**
   * Notify platform that CMS module is ready
   */
  notifyReady(): void {
    this.communication.broadcast(MessageTypes.MODULE_READY, {
      module: 'cmsModule',
      version: '1.0.0',
      capabilities: ['content-editing', 'asset-management', 'template-creation'],
    });
  }

  /**
   * Notify platform about content being saved
   */
  notifyContentSaved(contentData: any): void {
    this.communication.send(MessageTypes.CONTENT_SAVED, 'platform_host', {
      contentId: contentData.id,
      contentType: contentData.type,
      title: contentData.title,
      timestamp: Date.now(),
    });
  }

  /**
   * Notify platform about asset upload
   */
  notifyAssetUploaded(assetData: any): void {
    this.communication.send(MessageTypes.ASSET_UPLOADED, 'platform_host', {
      assetId: assetData.id,
      fileName: assetData.name,
      fileType: assetData.type,
      fileSize: assetData.size,
      url: assetData.url,
      timestamp: Date.now(),
    });
  }

  /**
   * Request navigation to a specific route
   */
  requestNavigation(route: string, params?: any): void {
    this.communication.send(MessageTypes.NAVIGATE_TO, 'platform_host', {
      route,
      params,
      source: 'cmsModule',
    });
  }

  /**
   * Show notification in platform
   */
  showNotification(type: 'success' | 'error' | 'warning' | 'info', message: string): void {
    this.communication.send(MessageTypes.NOTIFICATION, 'platform_host', {
      type,
      message,
      source: 'CMS Module',
      timestamp: Date.now(),
    });
  }

  /**
   * Request user context from platform
   */
  async requestUserContext(): Promise<any> {
    try {
      return await this.communication.request(
        'GET_USER_CONTEXT',
        'platform_host',
        {},
        3000
      );
    } catch (error) {
      console.error('Failed to get user context:', error);
      return null;
    }
  }

  /**
   * Request tenant information from platform
   */
  async requestTenantInfo(): Promise<any> {
    try {
      return await this.communication.request(
        'GET_TENANT_INFO',
        'platform_host',
        {},
        3000
      );
    } catch (error) {
      console.error('Failed to get tenant info:', error);
      return null;
    }
  }

  /**
   * Handle user context updates
   */
  private handleUserContextUpdate(contextData: any): void {
    // Emit custom event for CMS components to listen to
    const event = new CustomEvent('cms-user-context-updated', {
      detail: contextData,
    });
    window.dispatchEvent(event);
  }

  /**
   * Handle tenant changes
   */
  private handleTenantChange(tenantData: any): void {
    // Emit custom event for CMS components to listen to
    const event = new CustomEvent('cms-tenant-changed', {
      detail: tenantData,
    });
    window.dispatchEvent(event);
  }

  /**
   * Handle theme changes
   */
  private handleThemeChange(themeData: any): void {
    // Emit custom event for CMS components to listen to
    const event = new CustomEvent('cms-theme-changed', {
      detail: themeData,
    });
    window.dispatchEvent(event);
  }

  /**
   * Handle logout
   */
  private handleLogout(): void {
    // Clear any CMS-specific data and notify components
    const event = new CustomEvent('cms-logout');
    window.dispatchEvent(event);
  }

  /**
   * Subscribe to CMS-specific events
   */
  subscribe(eventType: string, handler: (message: any) => void): () => void {
    return this.communication.subscribe(eventType, handler);
  }

  /**
   * Send custom message
   */
  send(type: string, target: string, payload: any): string {
    return this.communication.send(type, target, payload);
  }

  /**
   * Broadcast message to all modules
   */
  broadcast(type: string, payload: any): string {
    return this.communication.broadcast(type, payload);
  }

  /**
   * Cleanup communication
   */
  cleanup(): void {
    this.communication.cleanup();
  }
}

// Export singleton instance
export const cmsCommunication = CmsCommunication.getInstance();
export default cmsCommunication;