import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  // Flat config .gitignore'u otomatik okumaz; VS Code local history kopyalarını da hariç tut.
  globalIgnores(['dist', '.history']),
  {
    files: ['**/*.{js,jsx}'],
    extends: [
      js.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: { ecmaFeatures: { jsx: true } },
    },
    rules: {
      // useAsync(fn, deps) çağrılarının bağımlılık listesi de useEffect gibi kontrol edilsin
      'react-hooks/exhaustive-deps': ['warn', { additionalHooks: '^useAsync$' }],
    },
  },
  {
    // Bu dosyalar tarayıcıda değil Node.js'te (Vite/ESLint tarafından) çalışır: process, __dirname vb.
    files: ['*.config.js'],
    languageOptions: { globals: globals.node },
  },
])
