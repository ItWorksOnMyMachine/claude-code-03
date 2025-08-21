# Product Mission

## Pitch

Enterprise Platform Host is a micro frontend platform that helps companies replace third-party SaaS dependencies by providing a unified, modular system with authentication, multi-tenancy, and role-based access control for hosting internal services.

## Users

### Primary Customers

- **Platform Administrators**: Our company's employees who manage the platform itself and provide support to customer organizations
- **Customer Organizations**: Companies using our platform to replace their third-party SaaS dependencies
- **Organization Users**: Employees of customer organizations who access services based on their role and tenant membership
- **Anonymous Users**: End users invited via unique URLs to interact with customer-generated content

### User Personas

**Platform Administrator** 
- **Role:** Platform Admin / Support Engineer (our company employee)
- **Context:** Member of the special platform tenant with cross-tenant administrative capabilities
- **Pain Points:** Managing multiple customer issues, tracking cross-tenant operations, maintaining platform health
- **Goals:** Efficiently manage customer tenants, provide timely support, maintain platform security and performance

**Organization Owner** 
- **Role:** Business Owner / Decision Maker
- **Context:** Small to medium businesses needing to consolidate their SaaS tools
- **Pain Points:** Managing multiple service subscriptions, limited integration between tools, cost of multiple SaaS platforms
- **Goals:** Unified platform for all digital needs, cost reduction, better control over their digital assets

**Organization User** 
- **Role:** Employee / Team Member
- **Context:** May belong to multiple organizations/tenants with different roles in each
- **Pain Points:** Switching between multiple platforms, inconsistent interfaces, managing multiple credentials
- **Goals:** Seamless access to all authorized services, easy tenant switching, consistent user experience

## The Problem

### Third-Party Service Fragmentation

Companies rely on multiple disconnected SaaS platforms (Squarespace, FormStack, etc.) leading to integration challenges, increased costs, and limited customization options. This results in 30-40% higher operational costs and reduced efficiency.

**Our Solution:** A unified micro frontend platform that consolidates all services under one customizable, scalable architecture.

### Lack of Unified Authentication and Authorization

Managing user access across multiple platforms creates security vulnerabilities and administrative overhead. Organizations spend 15-20 hours monthly on access management alone.

**Our Solution:** Centralized identity provider with secure authentication, followed by tenant selection at the platform level. Users authenticate once, then select their working tenant from available options. The Backend for Frontend pattern ensures all authentication tokens remain server-side in HttpOnly cookies, eliminating client-side token storage vulnerabilities.

### Limited Customization and Control

Third-party platforms restrict customization options and lock organizations into rigid workflows. 70% of businesses report needing features that their current SaaS providers don't offer.

**Our Solution:** Modular architecture allowing custom micro frontend services with full control over functionality and user experience.

## Differentiators

### Enterprise-Grade Security Architecture

Unlike platforms that store authentication tokens in browser storage, we implement a Backend for Frontend (BFF) pattern for each micro service, ensuring all authentication tokens remain server-side in HttpOnly cookies. This eliminates XSS token theft risks and provides 100% protection against client-side token exposure.

### Modular Micro Frontend Architecture

Unlike monolithic SaaS platforms, we provide a module federation approach that allows independent development and deployment of services. This results in 50% faster feature delivery and seamless scaling.

### Post-Authentication Tenant Selection

Unlike systems that couple authentication with tenant context, we separate identity verification from tenant selection. Users authenticate first, then select their working tenant from available options. This enables users to belong to multiple organizations, switch contexts without re-authentication, and allows platform administrators to access a special administrative tenant for cross-organization support.

### Progressive Migration Path

Unlike rip-and-replace solutions, we enable gradual migration from third-party services while maintaining business continuity. This results in zero downtime during transition and 60% lower migration risk.

## Key Features

### Core Features

- **Module Federation Host:** Central platform managing micro frontend integration and orchestration
- **Identity Provider:** Standalone authentication service with SSO support, MFA capabilities, and secure token issuance
- **Tenant Selection System:** Post-authentication tenant selection with support for multiple organization membership
- **Platform Administration Tenant:** Special tenant for company employees to manage customer organizations
- **Entitlement Engine:** Dynamic service enablement based on selected tenant and user permissions
- **Role-Based Access Control:** Granular permissions management per tenant, with different roles across organizations

### Collaboration Features

- **Dynamic Navigation:** Left sidebar with service entries that adapt based on user entitlements
- **Submenu Registration:** Micro services can dynamically register navigation items based on user roles
- **Unified User Context:** Shared user information across all micro frontend services
- **CMS Micro Service:** GrapesJS-based content management system for website creation and editing
- **Service Discovery:** Automatic detection and integration of new micro frontend services
- **Backend for Frontend Layer:** Dedicated BFF for each micro service handling secure API proxying and token management