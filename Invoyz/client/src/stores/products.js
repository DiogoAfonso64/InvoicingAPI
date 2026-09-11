import { ref } from 'vue'
import * as productsApi from '../api/products'
import { describeApiError } from '../api/http'

const items = ref([])
const loading = ref(false)
const error = ref('')

async function fetchAll() {
  loading.value = true
  error.value = ''
  try {
    items.value = await productsApi.getProducts()
  } catch (err) {
    error.value = describeApiError(err)
  } finally {
    loading.value = false
  }
}

async function add(payload) {
  const created = await productsApi.createProduct(payload)
  items.value = [...items.value, created]
  return created
}

async function update(id, payload) {
  const updated = await productsApi.updateProduct(id, payload)
  items.value = items.value.map((item) => (item.id === id ? updated : item))
  return updated
}

async function remove(id) {
  await productsApi.deleteProduct(id)
  items.value = items.value.filter((item) => item.id !== id)
}

const store = { items, loading, error, fetchAll, add, update, remove }

export function useProductStore() {
  return store
}
