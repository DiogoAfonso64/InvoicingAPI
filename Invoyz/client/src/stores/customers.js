import { ref } from 'vue'
import * as customersApi from '../api/customers'
import { describeApiError } from '../api/http'

const items = ref([])
const loading = ref(false)
const error = ref('')

async function fetchAll() {
  loading.value = true
  error.value = ''
  try {
    items.value = await customersApi.getCustomers()
  } catch (err) {
    error.value = describeApiError(err)
  } finally {
    loading.value = false
  }
}

async function add(payload) {
  const created = await customersApi.createCustomer(payload)
  items.value = [...items.value, created]
  return created
}

async function update(id, payload) {
  const updated = await customersApi.updateCustomer(id, payload)
  items.value = items.value.map((item) => (item.id === id ? updated : item))
  return updated
}

async function remove(id) {
  await customersApi.deleteCustomer(id)
  items.value = items.value.filter((item) => item.id !== id)
}

const store = { items, loading, error, fetchAll, add, update, remove }

export function useCustomerStore() {
  return store
}
