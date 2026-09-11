import { ref } from 'vue'
import * as invoicesApi from '../api/invoices'
import { describeApiError } from '../api/http'

const items = ref([])
const loading = ref(false)
const error = ref('')

async function fetchAll() {
  loading.value = true
  error.value = ''
  try {
    items.value = await invoicesApi.getInvoices()
  } catch (err) {
    error.value = describeApiError(err)
  } finally {
    loading.value = false
  }
}

async function add(payload) {
  const created = await invoicesApi.createInvoice(payload)
  items.value = [...items.value, created]
  return created
}

async function update(id, payload) {
  const updated = await invoicesApi.updateInvoice(id, payload)
  items.value = items.value.map((item) => (item.id === id ? updated : item))
  return updated
}

async function remove(id) {
  await invoicesApi.deleteInvoice(id)
  items.value = items.value.filter((item) => item.id !== id)
}

const store = { items, loading, error, fetchAll, add, update, remove }

export function useInvoiceStore() {
  return store
}
