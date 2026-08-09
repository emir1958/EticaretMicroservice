import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5234/api' // Gateway Adresimiz
});

// Her istek öncesi araya girip Token ekleyen Interceptor
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export default api;