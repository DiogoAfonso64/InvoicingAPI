import { http } from './http'

export function startPdfJob(invoiceId) {
  return http.post(`/invoices/${invoiceId}/pdf`).then((res) => res.data)
}

export function getPdfStatus(invoiceId) {
  return http.get(`/invoices/${invoiceId}/pdf`).then((res) => res.data)
}

export function getPdfDownloadUrl(invoiceId) {
  return `${http.defaults.baseURL}/invoices/${invoiceId}/pdf/download`
}
