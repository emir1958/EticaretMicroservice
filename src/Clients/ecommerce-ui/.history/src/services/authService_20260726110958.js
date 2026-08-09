import axios from 'axios';

const API_URL = 'http://localhost:5234/api/Auth';

export const register = async (username, email, password) => {
  const response = await axios.post(`${API_URL}/register`, {
    username,
    email,
    password
  });
  return response.data;
};

// ⚠️ 'export' kelimesinin fonksiyonun başında olduğundan emin ol!
export const login = async (email, password) => {
  const response = await axios.post(`${API_URL}/login`, {
    email,
    password
  });
  
  if (response.data.token) {
    localStorage.setItem('token', response.data.token);
  }
  return response.data;
};

export const logout = () => {
  localStorage.removeItem('token');
};

export const getCurrentToken = () => {
  return localStorage.getItem('token');
};