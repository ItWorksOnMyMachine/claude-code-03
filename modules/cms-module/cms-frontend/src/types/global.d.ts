/// <reference types="react" />

// Custom event types for module communication
interface ModuleMessage {
  type: string;
  payload?: any;
  source?: string;
  target?: string;
}

interface ModuleMessageEvent extends CustomEvent<ModuleMessage> {
  detail: ModuleMessage;
}

// Extend Window interface for custom events
interface WindowEventMap {
  'module-message': ModuleMessageEvent;
}

// GrapesJS type extensions
declare module 'grapesjs' {
  interface StorageManagerConfig {
    urlStore?: string;
    urlLoad?: string;
    [key: string]: any;
  }
}

// Fix for IntersectionObserver mock in tests
declare global {
  interface IntersectionObserver {
    root: Element | null;
    rootMargin: string;
    thresholds: ReadonlyArray<number>;
    takeRecords(): IntersectionObserverEntry[];
  }
}