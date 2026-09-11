import { http } from './http'

export function getInvoices() {
  return http.get('/invoices').then((res) => res.data)
}

export function getInvoice(id) {
  return http.get(`/invoices/${id}`).then((res) => res.data)
}

export function createInvoice(invoice) {
  return http.post('/invoices', invoice).then((res) => res.data)
}

export function updateInvoice(id, invoice) {
  return http.put(`/invoices/${id}`, invoice).then((res) => res.data)
}

export function deleteInvoice(id) {
  return http.delete(`/invoices/${id}`)
}
