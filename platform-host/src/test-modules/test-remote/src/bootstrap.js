import React from 'react';
import ReactDOM from 'react-dom/client';
import TestApp from './TestApp';

// Standalone app initialization
const container = document.getElementById('root');
if (container) {
  const root = ReactDOM.createRoot(container);
  root.render(
    <React.StrictMode>
      <TestApp />
    </React.StrictMode>
  );
}