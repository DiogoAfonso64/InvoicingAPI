import { http } from './http'

export function updateInvoiceLine(id, line) {
  return http.put(`/invoicelines/${id}`, line).then((res) => res.data)
}

export function deleteInvoiceLine(id) {
  return http.delete(`/invoicelines/${id}`)
}
