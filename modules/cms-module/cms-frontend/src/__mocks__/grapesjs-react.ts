import React from 'react';

export default function GrapesJSReact({ onEditor, ...props }: any) {
  React.useEffect(() => {
    if (onEditor) {
      onEditor({
        getHtml: jest.fn().mockReturnValue('<div>Mock HTML</div>'),
        getCss: jest.fn().mockReturnValue('body { margin: 0; }'),
        setComponents: jest.fn(),
        setStyle: jest.fn(),
        on: jest.fn(),
        off: jest.fn(),
        destroy: jest.fn(),
      });
    }
  }, [onEditor]);

  return React.createElement('div', { 
    'data-testid': 'grapesjs-editor',
    ...props 
  }, 'Mock GrapesJS Editor');
}
