import { http } from './http'

export function getCustomers() {
  return http.get('/customers').then((res) => res.data)
}

export function getCustomer(id) {
  return http.get(`/customers/${id}`).then((res) => res.data)
}

export function createCustomer(customer) {
  return http.post('/customers', customer).then((res) => res.data)
}

export function updateCustomer(id, customer) {
  return http.put(`/customers/${id}`, customer).then((res) => res.data)
}

export function deleteCustomer(id) {
  return http.delete(`/customers/${id}`)
}
