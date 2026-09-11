import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080/api'

export const http = axios.create({ baseURL })

export function describeApiError(err) {
  const data = err.response?.data
  if (Array.isArray(data)) return data.join(' ')
  if (typeof data === 'string' && data) return data
  if (err.request && !err.response) return 'Could not reach the server. Is the API running?'
  return err.message ?? 'Something went wrong.'
}
