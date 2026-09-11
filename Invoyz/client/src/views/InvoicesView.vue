<script setup>
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useInvoiceStore } from '../stores/invoices'
import { useCustomerStore } from '../stores/customers'
import { useProductStore } from '../stores/products'
import * as invoiceLinesApi from '../api/invoiceLines'
import { getPdfDownloadUrl, getPdfStatus, startPdfJob } from '../api/invoicePdf'
import { describeApiError } from '../api/http'
import { calculateInvoiceTotals } from '../utils/invoiceTotals'
import InvoiceFormModal from '../components/invoices/InvoiceFormModal.vue'
import InvoiceLineFormModal from '../components/invoices/InvoiceLineFormModal.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'

const { items: invoices, loading, error, fetchAll, add, update, remove } = useInvoiceStore()
const { items: customers, fetchAll: fetchCustomers } = useCustomerStore()
const { items: products, fetchAll: fetchProducts } = useProductStore()

onMounted(() => {
  fetchAll()
  fetchCustomers()
  fetchProducts()
})

const formOpen = ref(false)
const editingInvoice = ref(null)
const formSubmitting = ref(false)
const formError = ref('')

const confirmOpen = ref(false)
const pendingDeleteId = ref(null)
const deleteError = ref('')

const lineFormOpen = ref(false)
const editingLine = ref(null)
const editingLineInvoiceId = ref(null)
const lineFormSubmitting = ref(false)
const lineFormError = ref('')

const lineConfirmOpen = ref(false)
const pendingLineDelete = ref(null) // { invoiceId, lineId }
const lineDeleteError = ref('')

const expanded = reactive(new Set())

// Per-invoice PDF print state: { status: 'Pending'|'Printing'|'Printed'|'Failed', error }.
// Absent entry means idle - no print in progress for that invoice.
const pdfJobs = reactive({})
const pdfPollTimers = {}
const POLL_INTERVAL_MS = 2000

onBeforeUnmount(() => {
  Object.keys(pdfPollTimers).forEach(stopPdfPolling)
})

function triggerDownload(invoiceId) {
  const link = document.createElement('a')
  link.href = getPdfDownloadUrl(invoiceId)
  link.target = '_blank'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

function stopPdfPolling(invoiceId) {
  if (pdfPollTimers[invoiceId]) {
    clearInterval(pdfPollTimers[invoiceId])
    delete pdfPollTimers[invoiceId]
  }
}

function applyPdfJob(invoiceId, job) {
  pdfJobs[invoiceId] = { status: job.status, error: job.error ?? '' }

  if (job.status === 'Printed') {
    stopPdfPolling(invoiceId)
    triggerDownload(invoiceId)
    delete pdfJobs[invoiceId]
  } else if (job.status === 'Failed') {
    stopPdfPolling(invoiceId)
  }
}

function startPdfPolling(invoiceId) {
  stopPdfPolling(invoiceId)
  pdfPollTimers[invoiceId] = setInterval(async () => {
    try {
      applyPdfJob(invoiceId, await getPdfStatus(invoiceId))
    } catch (err) {
      stopPdfPolling(invoiceId)
      pdfJobs[invoiceId] = { status: 'Failed', error: describeApiError(err) }
    }
  }, POLL_INTERVAL_MS)
}

async function printInvoice(invoiceId) {
  const current = pdfJobs[invoiceId]?.status
  if (current === 'Pending' || current === 'Printing') return

  pdfJobs[invoiceId] = { status: 'Pending', error: '' }
  try {
    const job = await startPdfJob(invoiceId)
    applyPdfJob(invoiceId, job)
    if (job.status === 'Pending' || job.status === 'Printing') {
      startPdfPolling(invoiceId)
    }
  } catch (err) {
    pdfJobs[invoiceId] = { status: 'Failed', error: describeApiError(err) }
  }
}

function toggleExpanded(id) {
  if (expanded.has(id)) expanded.delete(id)
  else expanded.add(id)
}

function customerName(id) {
  return customers.value.find((c) => c.id === id)?.name ?? 'Unknown customer'
}

function productName(id) {
  return products.value.find((p) => p.id === id)?.name ?? 'Unknown product'
}

function openCreate() {
  editingInvoice.value = null
  formError.value = ''
  formOpen.value = true
}

function openEdit(invoice) {
  editingInvoice.value = invoice
  formError.value = ''
  formOpen.value = true
}

async function handleSubmit(payload) {
  formSubmitting.value = true
  formError.value = ''
  try {
    if (editingInvoice.value) {
      await update(editingInvoice.value.id, payload)
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

// Editing/removing a single line writes through the standalone /api/invoicelines
// endpoint (the separate line-level flow) and then also saves the whole invoice so
// its embedded lines/totals - what this view reads for display - stay in sync.
function invoicePayload(invoice, lines) {
  return {
    invoiceNumber: invoice.invoiceNumber,
    customerId: invoice.customerId,
    issueDate: invoice.issueDate,
    dueDate: invoice.dueDate,
    status: invoice.status,
    lines,
    ...calculateInvoiceTotals(lines),
  }
}

function openLineEdit(invoice, line) {
  editingLineInvoiceId.value = invoice.id
  editingLine.value = line
  lineFormError.value = ''
  lineFormOpen.value = true
}

async function handleLineSubmit(payload) {
  const invoice = invoices.value.find((inv) => inv.id === editingLineInvoiceId.value)
  if (!invoice) return

  lineFormSubmitting.value = true
  lineFormError.value = ''
  try {
    const mergedLine = { ...editingLine.value, ...payload }
    await invoiceLinesApi.updateInvoiceLine(editingLine.value.id, mergedLine)

    const lines = invoice.lines.map((line) => (line.id === editingLine.value.id ? mergedLine : line))
    await update(invoice.id, invoicePayload(invoice, lines))
    lineFormOpen.value = false
  } catch (err) {
    lineFormError.value = describeApiError(err)
  } finally {
    lineFormSubmitting.value = false
  }
}

function askLineDelete(invoice, line) {
  pendingLineDelete.value = { invoiceId: invoice.id, lineId: line.id }
  lineDeleteError.value = ''
  lineConfirmOpen.value = true
}

async function confirmLineDelete() {
  if (!pendingLineDelete.value) return
  const { invoiceId, lineId } = pendingLineDelete.value
  const invoice = invoices.value.find((inv) => inv.id === invoiceId)
  try {
    await invoiceLinesApi.deleteInvoiceLine(lineId)
    if (invoice) {
      const lines = invoice.lines.filter((line) => line.id !== lineId)
      await update(invoice.id, invoicePayload(invoice, lines))
    }
  } catch (err) {
    lineDeleteError.value = describeApiError(err)
  } finally {
    pendingLineDelete.value = null
  }
}

function currency(value) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' }).format(value || 0)
}

function formatDate(value) {
  if (!value) return '—'
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-xl font-semibold text-slate-900">Invoices</h1>
        <p class="mt-1 text-sm text-slate-500">{{ invoices.length }} total</p>
      </div>
      <button type="button" class="btn btn-primary" @click="openCreate">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
          <path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z" />
        </svg>
        New invoice
      </button>
    </div>

    <p v-if="deleteError" class="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ deleteError }}</p>
    <p v-if="lineDeleteError" class="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ lineDeleteError }}</p>

    <div class="card overflow-hidden">
      <div v-if="loading" class="px-4 py-12 text-center text-sm text-slate-500">Loading invoices…</div>
      <div v-else-if="error" class="px-4 py-12 text-center text-sm text-red-600">{{ error }}</div>
      <div v-else-if="invoices.length === 0" class="px-4 py-12 text-center text-sm text-slate-500">
        No invoices yet. Create your first one to get started.
      </div>
      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-100">
          <thead class="bg-slate-50">
            <tr>
              <th class="table-head-cell w-10"></th>
              <th class="table-head-cell">Invoice #</th>
              <th class="table-head-cell">Customer</th>
              <th class="table-head-cell">Issue date</th>
              <th class="table-head-cell">Due date</th>
              <th class="table-head-cell">Status</th>
              <th class="table-head-cell text-right">Grand total</th>
              <th class="table-head-cell text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <template v-for="invoice in invoices" :key="invoice.id">
              <tr class="cursor-pointer hover:bg-slate-50" @click="toggleExpanded(invoice.id)">
                <td class="table-cell">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    class="h-4 w-4 text-slate-400 transition-transform"
                    :class="{ 'rotate-90': expanded.has(invoice.id) }"
                  >
                    <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 0 1 .02-1.06L11.168 10 7.23 6.29a.75.75 0 1 1 1.04-1.08l4.5 4.25a.75.75 0 0 1 0 1.08l-4.5 4.25a.75.75 0 0 1-1.06-.02Z" clip-rule="evenodd" />
                  </svg>
                </td>
                <td class="table-cell font-medium text-slate-900">{{ invoice.invoiceNumber }}</td>
                <td class="table-cell">{{ customerName(invoice.customerId) }}</td>
                <td class="table-cell">{{ formatDate(invoice.issueDate) }}</td>
                <td class="table-cell">{{ formatDate(invoice.dueDate) }}</td>
                <td class="table-cell"><StatusBadge :status="invoice.status" /></td>
                <td class="table-cell text-right font-medium text-slate-900">{{ currency(invoice.grandTotal) }}</td>
                <td class="table-cell text-right" @click.stop>
                  <div class="flex items-center justify-end gap-1">
                    <span v-if="pdfJobs[invoice.id]?.status === 'Failed'" class="text-xs text-red-600" :title="pdfJobs[invoice.id].error">
                      Print failed
                    </span>
                    <button
                      type="button"
                      class="icon-btn"
                      :class="{ 'text-red-600': pdfJobs[invoice.id]?.status === 'Failed' }"
                      :disabled="pdfJobs[invoice.id]?.status === 'Pending' || pdfJobs[invoice.id]?.status === 'Printing'"
                      :aria-label="pdfJobs[invoice.id]?.status === 'Failed' ? 'Retry print' : 'Print PDF'"
                      :title="pdfJobs[invoice.id]?.status === 'Failed' ? 'Retry print' : 'Print PDF'"
                      @click="printInvoice(invoice.id)"
                    >
                      <svg
                        v-if="pdfJobs[invoice.id]?.status === 'Pending' || pdfJobs[invoice.id]?.status === 'Printing'"
                        class="h-4 w-4 animate-spin"
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                        fill="none"
                      >
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8V0C5.373 0 0 5.373 0 12h4Z" />
                      </svg>
                      <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                        <path fill-rule="evenodd" d="M5 2.75C5 1.784 5.784 1 6.75 1h6.5c.966 0 1.75.784 1.75 1.75v2.5h.75A2.25 2.25 0 0 1 18 7.5v5.25a2.25 2.25 0 0 1-2.25 2.25h-.75v.5A1.75 1.75 0 0 1 13.25 17.5h-6.5A1.75 1.75 0 0 1 5 15.75v-.5h-.75A2.25 2.25 0 0 1 2 13V7.5a2.25 2.25 0 0 1 2.25-2.25H5v-2.5ZM6.5 5.25h7v-2.5a.25.25 0 0 0-.25-.25h-6.5a.25.25 0 0 0-.25.25v2.5Zm0 8v2.5c0 .138.112.25.25.25h6.5a.25.25 0 0 0 .25-.25v-2.5h-7ZM15 9.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clip-rule="evenodd" />
                      </svg>
                    </button>
                    <button type="button" class="icon-btn" aria-label="Edit" @click="openEdit(invoice)">
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                        <path d="M13.586 3.586a2 2 0 1 1 2.828 2.828l-.793.793-2.828-2.828.793-.793ZM11.379 5.793 3 14.172V17h2.828l8.38-8.379-2.83-2.828Z" />
                      </svg>
                    </button>
                    <button type="button" class="icon-btn hover:text-red-600" aria-label="Delete" @click="askDelete(invoice.id)">
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                        <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 0 0 6 3.75v.5h-2.25a.75.75 0 0 0 0 1.5h.3l.815 10.19A2.75 2.75 0 0 0 7.607 18.5h4.786a2.75 2.75 0 0 0 2.742-2.56l.815-10.19h.3a.75.75 0 0 0 0-1.5H14v-.5A2.75 2.75 0 0 0 11.25 1h-2.5ZM10 8a.75.75 0 0 1 .75.75v5.5a.75.75 0 0 1-1.5 0v-5.5A.75.75 0 0 1 10 8Z" clip-rule="evenodd" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="expanded.has(invoice.id)">
                <td colspan="8" class="bg-slate-50 px-4 py-4">
                  <div class="overflow-x-auto rounded-lg bg-white ring-1 ring-slate-200">
                    <table class="min-w-full divide-y divide-slate-100 text-sm">
                      <thead>
                        <tr>
                          <th class="table-head-cell">Product</th>
                          <th class="table-head-cell text-right">Qty</th>
                          <th class="table-head-cell text-right">Unit price</th>
                          <th class="table-head-cell text-right">Tax %</th>
                          <th class="table-head-cell text-right">Line tax</th>
                          <th class="table-head-cell text-right">Line total</th>
                          <th class="table-head-cell text-right">Actions</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-slate-100">
                        <tr v-for="line in invoice.lines" :key="line.id">
                          <td class="table-cell">{{ productName(line.productId) }}</td>
                          <td class="table-cell text-right">{{ line.quantity }}</td>
                          <td class="table-cell text-right">{{ currency(line.unitPrice) }}</td>
                          <td class="table-cell text-right">{{ line.taxRate }}%</td>
                          <td class="table-cell text-right">{{ currency(line.lineTax) }}</td>
                          <td class="table-cell text-right font-medium text-slate-900">{{ currency(line.lineTotal) }}</td>
                          <td class="table-cell text-right">
                            <div class="flex justify-end gap-1">
                              <button type="button" class="icon-btn" aria-label="Edit line" @click="openLineEdit(invoice, line)">
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                                  <path d="M13.586 3.586a2 2 0 1 1 2.828 2.828l-.793.793-2.828-2.828.793-.793ZM11.379 5.793 3 14.172V17h2.828l8.38-8.379-2.83-2.828Z" />
                                </svg>
                              </button>
                              <button
                                type="button"
                                class="icon-btn hover:text-red-600"
                                aria-label="Delete line"
                                :disabled="invoice.lines.length === 1"
                                :title="invoice.lines.length === 1 ? 'An invoice needs at least one line' : undefined"
                                @click="askLineDelete(invoice, line)"
                              >
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

                  <div class="ml-auto mt-3 w-full max-w-xs space-y-1 text-sm">
                    <div class="flex justify-between text-slate-500">
                      <span>Subtotal</span>
                      <span>{{ currency(invoice.subTotal) }}</span>
                    </div>
                    <div class="flex justify-between text-slate-500">
                      <span>Tax</span>
                      <span>{{ currency(invoice.totalTax) }}</span>
                    </div>
                    <div class="flex justify-between border-t border-slate-200 pt-1 font-semibold text-slate-900">
                      <span>Grand total</span>
                      <span>{{ currency(invoice.grandTotal) }}</span>
                    </div>
                  </div>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>

    <InvoiceFormModal
      v-model="formOpen"
      :invoice="editingInvoice"
      :customers="customers"
      :products="products"
      :submitting="formSubmitting"
      :server-error="formError"
      @submit="handleSubmit"
    />

    <ConfirmDialog
      v-model="confirmOpen"
      title="Delete invoice"
      message="This invoice and its lines will be permanently removed. This action cannot be undone."
      @confirm="confirmDelete"
    />

    <InvoiceLineFormModal
      v-model="lineFormOpen"
      :line="editingLine"
      :products="products"
      :submitting="lineFormSubmitting"
      :server-error="lineFormError"
      @submit="handleLineSubmit"
    />

    <ConfirmDialog
      v-model="lineConfirmOpen"
      title="Delete line"
      message="This invoice line will be permanently removed. This action cannot be undone."
      @confirm="confirmLineDelete"
    />
  </div>
</template>
