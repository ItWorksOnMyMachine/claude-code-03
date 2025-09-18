/**
 * Cross-module communication service for micro-frontends
 * This is a copy of the platform host communication service for the CMS module
 */

export interface ModuleMessage {
    type: string;
    source: string;
    target: string;
    payload: any;
    timestamp: number;
    id: string;
}

export interface ModuleMessageHandler {
    (message: ModuleMessage): void | Promise<void>;
}

export interface ModuleMessageFilter {
    type?: string;
    source?: string;
    target?: string;
}

export class ModuleCommunication {
    private static instance: ModuleCommunication;
    private handlers: Map<string, Set<ModuleMessageHandler>> = new Map();
    private globalHandlers: Set<ModuleMessageHandler> = new Set();
    private moduleId: string;

    private constructor(moduleId: string) {
        this.moduleId = moduleId;
        this.setupWindowMessaging();
    }

    static getInstance(moduleId: string = "cmsModule"): ModuleCommunication {
        if (!ModuleCommunication.instance) {
            ModuleCommunication.instance = new ModuleCommunication(moduleId);
        }
        return ModuleCommunication.instance;
    }

    /**
     * Set up window-level messaging for cross-module communication
     */
    private setupWindowMessaging(): void {
        window.addEventListener("message", (event) => {
            // Only handle messages from our own origin or trusted origins
            if (event.origin !== window.location.origin && !this.isTrustedOrigin(event.origin)) {
                return;
            }

            if (event.data && event.data.type === "MODULE_FEDERATION_MESSAGE") {
                this.handleIncomingMessage(event.data.message);
            }
        });

        // Set up custom event system for same-origin communication
        window.addEventListener(
            "module-message" as any,
            ((event: CustomEvent<ModuleMessage>) => {
                this.handleIncomingMessage(event.detail);
            }) as EventListener
        );
    }

    /**
     * Check if an origin is trusted for cross-module communication
     */
    private isTrustedOrigin(origin: string): boolean {
        const trustedOrigins = [
            "https://host-fe.platform.local:3002",
            "https://cms-fe.platform.local:3003",
            "https://forms.platform.local:3004", // Future module
        ];

        return trustedOrigins.includes(origin);
    }

    /**
     * Send a message to another module
     */
    send(type: string, target: string, payload: any): string {
        const message: ModuleMessage = {
            type,
            source: this.moduleId,
            target,
            payload,
            timestamp: Date.now(),
            id: this.generateMessageId(),
        };

        // Try sending via custom events first (same-origin)
        try {
            const event = new CustomEvent("module-message", { detail: message });
            window.dispatchEvent(event);
        } catch (error) {
            console.warn("Failed to send custom event message:", error);
        }

        // Also send via postMessage for cross-origin communication
        if (target !== this.moduleId) {
            try {
                window.postMessage(
                    {
                        type: "MODULE_FEDERATION_MESSAGE",
                        message,
                    },
                    window.location.origin
                );
            } catch (error) {
                console.warn("Failed to send post message:", error);
            }
        }

        console.log(`Message sent from ${this.moduleId} to ${target}:`, message);
        return message.id;
    }

    /**
     * Subscribe to messages of a specific type
     */
    subscribe(type: string, handler: ModuleMessageHandler): () => void {
        if (!this.handlers.has(type)) {
            this.handlers.set(type, new Set());
        }

        this.handlers.get(type)!.add(handler);

        // Return unsubscribe function
        return () => {
            const typeHandlers = this.handlers.get(type);
            if (typeHandlers) {
                typeHandlers.delete(handler);
                if (typeHandlers.size === 0) {
                    this.handlers.delete(type);
                }
            }
        };
    }

    /**
     * Request data from another module and wait for response
     */
    async request(type: string, target: string, payload: any, timeout: number = 5000): Promise<any> {
        const requestId = this.generateMessageId();
        const responseType = `${type}_RESPONSE_${requestId}`;

        return new Promise((resolve, reject) => {
            const timeoutId = setTimeout(() => {
                cleanup();
                reject(new Error(`Request timeout: ${type} to ${target}`));
            }, timeout);

            const cleanup = this.subscribe(responseType, (message) => {
                clearTimeout(timeoutId);
                cleanup();
                resolve(message.payload);
            });

            this.send(type, target, { ...payload, requestId });
        });
    }

    /**
     * Respond to a request message
     */
    respond(originalMessage: ModuleMessage, responsePayload: any): void {
        const requestId = originalMessage.payload?.requestId;
        if (!requestId) {
            console.warn("Cannot respond to message without requestId:", originalMessage);
            return;
        }

        const responseType = `${originalMessage.type}_RESPONSE_${requestId}`;
        this.send(responseType, originalMessage.source, responsePayload);
    }

    /**
     * Handle incoming messages
     */
    private async handleIncomingMessage(message: ModuleMessage): Promise<void> {
        // Skip messages from self
        if (message.source === this.moduleId) {
            return;
        }

        // Skip messages not targeted to us (unless it's a broadcast)
        if (message.target !== this.moduleId && message.target !== "*") {
            return;
        }

        console.log(`Message received by ${this.moduleId}:`, message);

        // Call global handlers
        this.globalHandlers.forEach(async (handler) => {
            try {
                await handler(message);
            } catch (error) {
                console.error("Error in global message handler:", error);
            }
        });

        // Call type-specific handlers
        const typeHandlers = this.handlers.get(message.type);
        if (typeHandlers) {
            typeHandlers.forEach(async (handler) => {
                try {
                    await handler(message);
                } catch (error) {
                    console.error(`Error in message handler for type ${message.type}:`, error);
                }
            });
        }
    }

    /**
     * Broadcast a message to all modules
     */
    broadcast(type: string, payload: any): string {
        return this.send(type, "*", payload);
    }

    /**
     * Generate a unique message ID
     */
    private generateMessageId(): string {
        return `${this.moduleId}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Get the module ID
     */
    getModuleId(): string {
        return this.moduleId;
    }

    /**
     * Remove all handlers (cleanup)
     */
    cleanup(): void {
        this.handlers.clear();
        this.globalHandlers.clear();
    }
}

// Predefined message types for common communication patterns
export const MessageTypes = {
    // Authentication and user context
    USER_CONTEXT_UPDATED: "USER_CONTEXT_UPDATED",
    TENANT_CHANGED: "TENANT_CHANGED",
    LOGOUT: "LOGOUT",

    // Navigation
    NAVIGATE_TO: "NAVIGATE_TO",
    NAVIGATION_CHANGED: "NAVIGATION_CHANGED",

    // Data sharing
    DATA_REQUEST: "DATA_REQUEST",
    DATA_RESPONSE: "DATA_RESPONSE",
    DATA_UPDATED: "DATA_UPDATED",

    // Module lifecycle
    MODULE_READY: "MODULE_READY",
    MODULE_ERROR: "MODULE_ERROR",
    MODULE_UNMOUNTING: "MODULE_UNMOUNTING",

    // UI interactions
    THEME_CHANGED: "THEME_CHANGED",
    MODAL_OPEN: "MODAL_OPEN",
    MODAL_CLOSE: "MODAL_CLOSE",
    NOTIFICATION: "NOTIFICATION",

    // CMS-specific
    CONTENT_SAVED: "CONTENT_SAVED",
    ASSET_UPLOADED: "ASSET_UPLOADED",
    TEMPLATE_CREATED: "TEMPLATE_CREATED",
} as const;

// Export singleton instance for the CMS module
export const cmsCommunication = ModuleCommunication.getInstance("cmsModule");
export default ModuleCommunication;
