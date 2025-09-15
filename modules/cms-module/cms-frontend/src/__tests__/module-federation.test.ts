import moduleFederationConfig from '../../module-federation.config';

describe('Module Federation Configuration', () => {
  test('should have correct module name', () => {
    expect(moduleFederationConfig.name).toBe('cmsModule');
  });

  test('should expose CmsApp component', () => {
    expect(moduleFederationConfig.exposes).toHaveProperty('./CmsApp');
    expect(moduleFederationConfig.exposes['./CmsApp']).toBe('./src/CmsApp');
  });

  test('should expose CmsRouter component', () => {
    expect(moduleFederationConfig.exposes).toHaveProperty('./CmsRouter');
    expect(moduleFederationConfig.exposes['./CmsRouter']).toBe('./src/CmsRouter');
  });

  test('should have correct remote entry filename', () => {
    expect(moduleFederationConfig.filename).toBe('remoteEntry.js');
  });

  test('should configure React as singleton shared dependency', () => {
    expect(moduleFederationConfig.shared).toHaveProperty('react');
    expect(moduleFederationConfig.shared.react).toMatchObject({
      singleton: true,
      eager: true,
    });
  });

  test('should configure React DOM as singleton shared dependency', () => {
    expect(moduleFederationConfig.shared).toHaveProperty('react-dom');
    expect(moduleFederationConfig.shared['react-dom']).toMatchObject({
      singleton: true,
      eager: true,
    });
  });

  test('should configure Material-UI as shared dependencies', () => {
    expect(moduleFederationConfig.shared).toHaveProperty('@mui/material');
    expect(moduleFederationConfig.shared).toHaveProperty('@mui/system');
    expect(moduleFederationConfig.shared).toHaveProperty('@mui/icons-material');

    expect(moduleFederationConfig.shared['@mui/material']).toMatchObject({
      singleton: true,
      eager: true,
    });
  });

  test('should NOT share GrapesJS dependencies to avoid version conflicts', () => {
    expect(moduleFederationConfig.shared.grapesjs).toBe(false);
    expect(moduleFederationConfig.shared['grapesjs-react']).toBe(false);
    expect(moduleFederationConfig.shared['grapesjs-preset-webpage']).toBe(false);
    expect(moduleFederationConfig.shared['grapesjs-plugin-forms']).toBe(false);
  });

  test('should NOT share Emotion dependencies to prevent styling conflicts', () => {
    expect(moduleFederationConfig.shared['@emotion/react']).toBe(false);
    expect(moduleFederationConfig.shared['@emotion/styled']).toBe(false);
  });
});