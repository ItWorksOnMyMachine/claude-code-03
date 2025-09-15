import { useState, useEffect, useCallback } from 'react';

export interface EntitlementData {
  entitlements: string[];
  accessibleModules: string[];
  userId: string;
  tenantId: string;
}

export interface EntitlementHookResult {
  entitlements: string[];
  accessibleModules: string[];
  isLoading: boolean;
  error: string | null;
  hasEntitlement: (entitlement: string) => boolean;
  hasAnyEntitlement: (...entitlements: string[]) => boolean;
  hasAllEntitlements: (...entitlements: string[]) => boolean;
  canAccessModule: (moduleName: string) => boolean;
  refresh: () => Promise<void>;
}

/**
 * React hook for checking user entitlements and module access
 * Integrates with the platform's entitlement system
 */
export function useEntitlements(): EntitlementHookResult {
  const [entitlements, setEntitlements] = useState<string[]>([]);
  const [accessibleModules, setAccessibleModules] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadEntitlements = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const response = await fetch('/api/entitlements', {
        credentials: 'include', // Include cookies for authentication
      });

      if (!response.ok) {
        throw new Error(`Failed to load entitlements: ${response.statusText}`);
      }

      const data: EntitlementData = await response.json();
      setEntitlements(data.entitlements);
      setAccessibleModules(data.accessibleModules);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load entitlements';
      setError(errorMessage);
      console.error('Entitlement loading error:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Load entitlements on mount
  useEffect(() => {
    loadEntitlements();
  }, [loadEntitlements]);

  // Entitlement checking functions
  const hasEntitlement = useCallback((entitlement: string): boolean => {
    return entitlements.includes(entitlement);
  }, [entitlements]);

  const hasAnyEntitlement = useCallback((...requiredEntitlements: string[]): boolean => {
    return requiredEntitlements.some(e => entitlements.includes(e));
  }, [entitlements]);

  const hasAllEntitlements = useCallback((...requiredEntitlements: string[]): boolean => {
    return requiredEntitlements.every(e => entitlements.includes(e));
  }, [entitlements]);

  const canAccessModule = useCallback((moduleName: string): boolean => {
    return accessibleModules.includes(moduleName);
  }, [accessibleModules]);

  const refresh = useCallback(async () => {
    await loadEntitlements();
  }, [loadEntitlements]);

  return {
    entitlements,
    accessibleModules,
    isLoading,
    error,
    hasEntitlement,
    hasAnyEntitlement,
    hasAllEntitlements,
    canAccessModule,
    refresh,
  };
}

/**
 * Common entitlements used in the CMS module
 * Mirrors the backend PlatformEntitlements constants
 */
export const CmsEntitlements = {
  CMS_ACCESS: 'CMS_ACCESS',
  CMS_MANAGE: 'CMS_MANAGE',
  CMS_ASSETS: 'CMS_ASSETS',
  CMS_TEMPLATES: 'CMS_TEMPLATES',
  CMS_PUBLISH: 'CMS_PUBLISH',
} as const;

/**
 * Platform-wide entitlements
 */
export const PlatformEntitlements = {
  PLATFORM_ACCESS: 'PLATFORM_ACCESS',
  PLATFORM_ADMIN: 'PLATFORM_ADMIN',
  TENANT_ADMIN: 'TENANT_ADMIN',
} as const;