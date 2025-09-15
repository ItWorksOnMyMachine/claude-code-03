export default {
  init: jest.fn().mockReturnValue({
    Commands: {
      add: jest.fn(),
      get: jest.fn(),
    },
    editor: {
      getHtml: jest.fn().mockReturnValue('<div>Test HTML</div>'),
      getCss: jest.fn().mockReturnValue('body { margin: 0; }'),
      setComponents: jest.fn(),
      setStyle: jest.fn(),
      on: jest.fn(),
      off: jest.fn(),
      destroy: jest.fn(),
    },
    BlockManager: {
      add: jest.fn(),
      get: jest.fn(),
    },
    AssetManager: {
      add: jest.fn(),
      get: jest.fn(),
    },
  }),
};
