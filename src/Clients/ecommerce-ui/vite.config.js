import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  // Production build'de gateway adresi zorunlu. Eksikse hata kullanıcının tarayıcısında (beyaz ekran)
  // değil, build sırasında alınır. Geliştirmede (vite dev) src/config/env.js varsayılanı kullanır.
  const env = loadEnv(mode, process.cwd(), 'VITE_')
  if (command === 'build' && !env.VITE_API_URL) {
    throw new Error('VITE_API_URL tanımlı değil. .env.example dosyasını .env.local olarak kopyalayın veya ortam değişkeni verin.')
  }

  return {
    plugins: [react()],
  }
})
