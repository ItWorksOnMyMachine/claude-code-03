import { createApp } from '@modern-js/runtime';
import { router } from '@modern-js/runtime/plugins';

export default createApp({
  plugins: [
    router({
      supportHtml5History: true,
    }),
  ],
});