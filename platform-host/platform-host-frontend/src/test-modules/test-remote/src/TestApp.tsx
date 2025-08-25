import React from 'react';

export interface TestAppProps {
  message?: string;
}

const TestApp: React.FC<TestAppProps> = ({ message = 'Hello from Test Remote Module!' }) => {
  return (
    <div style={{ padding: '20px', border: '2px solid #4CAF50', borderRadius: '8px' }}>
      <h2>Test Remote Module</h2>
      <p>{message}</p>
      <p>This module was dynamically loaded via Module Federation</p>
      <button onClick={() => alert('Remote module is working!')}>
        Test Interaction
      </button>
    </div>
  );
};

export default TestApp;