import { http } from './http'

export function getProducts() {
  return http.get('/products').then((res) => res.data)
}

export function getProduct(id) {
  return http.get(`/products/${id}`).then((res) => res.data)
}

export function createProduct(product) {
  return http.post('/products', product).then((res) => res.data)
}

export function updateProduct(id, product) {
  return http.put(`/products/${id}`, product).then((res) => res.data)
}

export function deleteProduct(id) {
  return http.delete(`/products/${id}`)
}
