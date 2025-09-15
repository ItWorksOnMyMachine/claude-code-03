import React from 'react';

export const Routes = ({ children }: { children: React.ReactNode }) => children;
export const Route = ({ children }: { children?: React.ReactNode }) => children || null;
export const Navigate = () => null;
export const MemoryRouter = ({ children }: { children: React.ReactNode }) => React.createElement('div', {}, children);
