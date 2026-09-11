<script setup>
import { onMounted, ref } from 'vue'
import { useProductStore } from '../stores/products'
import { describeApiError } from '../api/http'
import ProductFormModal from '../components/products/ProductFormModal.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'

const { items: products, loading, error, fetchAll, add, update, remove } = useProductStore()

onMounted(fetchAll)

const formOpen = ref(false)
const editingProduct = ref(null)
const formSubmitting = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const pendingDeleteId = ref(null)
const deleteError = ref('')

function openCreate() {
  editingProduct.value = null
  formError.value = ''
  formOpen.value = true
}

function openEdit(product) {
  editingProduct.value = product
  formError.value = ''
  formOpen.value = true
}

async function handleSubmit(payload) {
  formSubmitting.value = true
  formError.value = ''
  try {
    if (editingProduct.value) {
      await update(editingProduct.value.id, payload)
    } else {
      await add(payload)
    }
    formOpen.value = false
  } catch (err) {
    formError.value = describeApiError(err)
  } finally {
    formSubmitting.value = false
  }
}

function askDelete(id) {
  pendingDeleteId.value = id
  deleteError.value = ''
  confirmOpen.value = true
}

async function confirmDelete() {
  if (!pendingDeleteId.value) return
  try {
    await remove(pendingDeleteId.value)
  } catch (err) {
    deleteError.value = describeApiError(err)
  } finally {
    pendingDeleteId.value = null
  }
}

function currency(value) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' }).format(value)
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-xl font-semibold text-slate-900">Products</h1>
        <p class="mt-1 text-sm text-slate-500">{{ products.length }} total</p>
      </div>
      <button type="button" class="btn btn-primary" @click="openCreate">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
          <path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z" />
        </svg>
        New product
      </button>
    </div>

    <p v-if="deleteError" class="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ deleteError }}</p>

    <div class="card overflow-hidden">
      <div v-if="loading" class="px-4 py-12 text-center text-sm text-slate-500">Loading products…</div>
      <div v-else-if="error" class="px-4 py-12 text-center text-sm text-red-600">{{ error }}</div>
      <div v-else-if="products.length === 0" class="px-4 py-12 text-center text-sm text-slate-500">
        No products yet. Add your first one to get started.
      </div>
      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-100">
          <thead class="bg-slate-50">
            <tr>
              <th class="table-head-cell">Name</th>
              <th class="table-head-cell">Description</th>
              <th class="table-head-cell">Unit price</th>
              <th class="table-head-cell">Tax rate</th>
              <th class="table-head-cell text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="product in products" :key="product.id" class="hover:bg-slate-50">
              <td class="table-cell font-medium text-slate-900">{{ product.name }}</td>
              <td class="table-cell text-slate-500">{{ product.description || '—' }}</td>
              <td class="table-cell">{{ currency(product.unitPrice) }}</td>
              <td class="table-cell">{{ product.taxRate }}%</td>
              <td class="table-cell text-right">
                <div class="flex justify-end gap-1">
                  <button type="button" class="icon-btn" aria-label="Edit" @click="openEdit(product)">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                      <path d="M13.586 3.586a2 2 0 1 1 2.828 2.828l-.793.793-2.828-2.828.793-.793ZM11.379 5.793 3 14.172V17h2.828l8.38-8.379-2.83-2.828Z" />
                    </svg>
                  </button>
                  <button type="button" class="icon-btn hover:text-red-600" aria-label="Delete" @click="askDelete(product.id)">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                      <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 0 0 6 3.75v.5h-2.25a.75.75 0 0 0 0 1.5h.3l.815 10.19A2.75 2.75 0 0 0 7.607 18.5h4.786a2.75 2.75 0 0 0 2.742-2.56l.815-10.19h.3a.75.75 0 0 0 0-1.5H14v-.5A2.75 2.75 0 0 0 11.25 1h-2.5ZM10 8a.75.75 0 0 1 .75.75v5.5a.75.75 0 0 1-1.5 0v-5.5A.75.75 0 0 1 10 8Z" clip-rule="evenodd" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <ProductFormModal
      v-model="formOpen"
      :product="editingProduct"
      :submitting="formSubmitting"
      :server-error="formError"
      @submit="handleSubmit"
    />

    <ConfirmDialog
      v-model="confirmOpen"
      title="Delete product"
      message="This product will be permanently removed. This action cannot be undone."
      @confirm="confirmDelete"
    />
  </div>
</template>
