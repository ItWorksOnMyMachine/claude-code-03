/**
 * API service for CMS content operations
 * Handles communication with the CMS BFF backend
 */

interface CmsContent {
  id?: string;
  title: string;
  slug: string;
  content: string;
  contentType: string;
  status: string;
  templateId?: string;
  metaTitle?: string;
  metaDescription?: string;
  tags?: string;
  featuredImage?: string;
}

interface CmsPage {
  id?: string;
  title: string;
  slug: string;
  metaTitle?: string;
  metaDescription?: string;
  status: 'Draft' | 'Published' | 'Archived';
  publishedAt?: string;
  featuredImageUrl?: string;
  templateId?: string;
  metadata: string;
  sortOrder: number;
  showInNavigation: boolean;
  parentPageId?: string;
}

interface CmsContentBlock {
  id?: string;
  pageId: string;
  blockType: string;
  zone: string;
  sortOrder: number;
  content: string;
  configuration: string;
  isActive: boolean;
  displayName?: string;
}

interface PlatformContext {
  authToken?: string;
  tenantId?: string;
  userId?: string;
}

export class CmsApiService {
  private baseUrl: string;
  private context: PlatformContext;

  constructor(context: PlatformContext, baseUrl = '/api/cms') {
    this.baseUrl = baseUrl;
    this.context = context;
  }

  private getHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (this.context.authToken) {
      headers['Authorization'] = `Bearer ${this.context.authToken}`;
    }

    if (this.context.tenantId) {
      headers['X-Tenant-Id'] = this.context.tenantId;
    }

    return headers;
  }

  // Content Operations (Legacy support)
  async createContent(content: Omit<CmsContent, 'id'>): Promise<CmsContent> {
    const response = await fetch(`${this.baseUrl}/content`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(content),
    });

    if (!response.ok) {
      throw new Error(`Failed to create content: ${response.statusText}`);
    }

    return response.json();
  }

  async updateContent(id: string, content: Partial<CmsContent>): Promise<CmsContent> {
    const response = await fetch(`${this.baseUrl}/content/${id}`, {
      method: 'PUT',
      headers: this.getHeaders(),
      body: JSON.stringify({ ...content, id }),
    });

    if (!response.ok) {
      throw new Error(`Failed to update content: ${response.statusText}`);
    }

    return response.json();
  }

  async getContent(id: string): Promise<CmsContent | null> {
    const response = await fetch(`${this.baseUrl}/content/${id}`, {
      headers: this.getHeaders(),
    });

    if (response.status === 404) {
      return null;
    }

    if (!response.ok) {
      throw new Error(`Failed to get content: ${response.statusText}`);
    }

    return response.json();
  }

  async getAllContent(): Promise<CmsContent[]> {
    const response = await fetch(`${this.baseUrl}/content`, {
      headers: this.getHeaders(),
    });

    if (!response.ok) {
      throw new Error(`Failed to get content list: ${response.statusText}`);
    }

    return response.json();
  }

  async deleteContent(id: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/content/${id}`, {
      method: 'DELETE',
      headers: this.getHeaders(),
    });

    if (!response.ok) {
      throw new Error(`Failed to delete content: ${response.statusText}`);
    }
  }

  // Page Operations (New schema)
  async createPage(page: Omit<CmsPage, 'id'>): Promise<CmsPage> {
    const response = await fetch(`${this.baseUrl}/pages`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(page),
    });

    if (!response.ok) {
      throw new Error(`Failed to create page: ${response.statusText}`);
    }

    return response.json();
  }

  async updatePage(id: string, page: Partial<CmsPage>): Promise<CmsPage> {
    const response = await fetch(`${this.baseUrl}/pages/${id}`, {
      method: 'PUT',
      headers: this.getHeaders(),
      body: JSON.stringify({ ...page, id }),
    });

    if (!response.ok) {
      throw new Error(`Failed to update page: ${response.statusText}`);
    }

    return response.json();
  }

  async getPage(id: string): Promise<CmsPage | null> {
    const response = await fetch(`${this.baseUrl}/pages/${id}`, {
      headers: this.getHeaders(),
    });

    if (response.status === 404) {
      return null;
    }

    if (!response.ok) {
      throw new Error(`Failed to get page: ${response.statusText}`);
    }

    return response.json();
  }

  async getAllPages(): Promise<CmsPage[]> {
    const response = await fetch(`${this.baseUrl}/pages`, {
      headers: this.getHeaders(),
    });

    if (!response.ok) {
      throw new Error(`Failed to get pages: ${response.statusText}`);
    }

    return response.json();
  }

  // Content Block Operations
  async getPageContentBlocks(pageId: string): Promise<CmsContentBlock[]> {
    const response = await fetch(`${this.baseUrl}/pages/${pageId}/blocks`, {
      headers: this.getHeaders(),
    });

    if (!response.ok) {
      throw new Error(`Failed to get content blocks: ${response.statusText}`);
    }

    return response.json();
  }

  async savePageContentBlocks(pageId: string, blocks: CmsContentBlock[]): Promise<void> {
    const response = await fetch(`${this.baseUrl}/pages/${pageId}/blocks`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(blocks),
    });

    if (!response.ok) {
      throw new Error(`Failed to save content blocks: ${response.statusText}`);
    }
  }

  // Asset Operations
  async uploadAsset(file: File): Promise<any> {
    const formData = new FormData();
    formData.append('files', file);

    const headers: HeadersInit = {};
    if (this.context.authToken) {
      headers['Authorization'] = `Bearer ${this.context.authToken}`;
    }
    if (this.context.tenantId) {
      headers['X-Tenant-Id'] = this.context.tenantId;
    }

    const response = await fetch(`${this.baseUrl}/assets/upload`, {
      method: 'POST',
      headers,
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`Failed to upload asset: ${response.statusText}`);
    }

    return response.json();
  }

  async getAllAssets(): Promise<any[]> {
    const response = await fetch(`${this.baseUrl}/assets`, {
      headers: this.getHeaders(),
    });

    if (!response.ok) {
      throw new Error(`Failed to get assets: ${response.statusText}`);
    }

    return response.json();
  }
}