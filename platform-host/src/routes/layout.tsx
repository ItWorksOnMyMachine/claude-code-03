import { Outlet } from '@modern-js/runtime/router';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppShell } from '@/components/layout';
import { ModuleFederationProvider } from '@/contexts/ModuleFederationContext';
import ErrorBoundary from '@/components/ErrorBoundary';
import theme from '@/theme/theme';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
    },
  },
});

export default function Layout() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <ModuleFederationProvider>
          <ErrorBoundary>
            <AppShell>
              <Outlet />
            </AppShell>
          </ErrorBoundary>
        </ModuleFederationProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
