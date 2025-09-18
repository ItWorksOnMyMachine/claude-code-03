import React from 'react';

export const Routes = ({ children }: { children: React.ReactNode }) => children;
export const Route = ({ children }: { children?: React.ReactNode }) => children || null;
export const Navigate = () => null;
export const MemoryRouter = ({ children }: { children: React.ReactNode }) => React.createElement('div', {}, children);
export const Link = ({ children }: { children: React.ReactNode }) => React.createElement('a', {}, children);

// Mock hooks
export const useParams = jest.fn(() => ({}));
export const useNavigate = jest.fn(() => jest.fn());
export const useLocation = jest.fn(() => ({ pathname: '/' }));
