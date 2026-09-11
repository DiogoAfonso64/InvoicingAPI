<script setup>
import { computed, reactive, watch } from 'vue'
import BaseModal from '../ui/BaseModal.vue'
import BaseInput from '../ui/BaseInput.vue'
import BaseSelect from '../ui/BaseSelect.vue'
import { createId } from '../../utils/id'
import { calculateInvoiceTotals, calculateLineTotals } from '../../utils/invoiceTotals'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  invoice: { type: Object, default: null },
  customers: { type: Array, default: () => [] },
  products: { type: Array, default: () => [] },
  submitting: { type: Boolean, default: false },
  serverError: { type: String, default: '' },
})

const emit = defineEmits(['update:modelValue', 'submit'])

const STATUS_OPTIONS = ['Draft', 'Sent', 'Paid', 'Overdue'].map((s) => ({ value: s, label: s }))

const form = reactive({
  invoiceNumber: '',
  customerId: '',
  issueDate: '',
  dueDate: '',
  status: 'Draft',
  lines: [],
})

const errors = reactive({ invoiceNumber: '', customerId: '', issueDate: '', dueDate: '', lines: '' })

const customerOptions = computed(() => props.customers.map((c) => ({ value: c.id, label: c.name })))
const productOptions = computed(() => props.products.map((p) => ({ value: p.id, label: p.name })))

function blankLine() {
  return { id: createId(), productId: '', quantity: 1, unitPrice: 0, taxRate: 0, lineTotal: 0, lineTax: 0 }
}

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    form.invoiceNumber = props.invoice?.invoiceNumber ?? ''
    form.customerId = props.invoice?.customerId ?? ''
    form.issueDate = props.invoice?.issueDate ?? new Date().toISOString().slice(0, 10)
    form.dueDate = props.invoice?.dueDate ?? ''
    form.status = props.invoice?.status ?? 'Draft'
    form.lines = props.invoice?.lines?.length
      ? props.invoice.lines.map((line) => ({ ...line }))
      : [blankLine()]
    errors.invoiceNumber = errors.customerId = errors.issueDate = errors.dueDate = errors.lines = ''
  },
)

function recalcLine(line) {
  const { lineTotal, lineTax } = calculateLineTotals(Number(line.quantity), Number(line.unitPrice), Number(line.taxRate))
  line.lineTotal = lineTotal
  line.lineTax = lineTax
}

function onProductChange(line) {
  const product = props.products.find((p) => p.id === line.productId)
  if (product) {
    line.unitPrice = product.unitPrice
    line.taxRate = product.taxRate
  }
  recalcLine(line)
}

function addLine() {
  form.lines.push(blankLine())
}

function removeLine(lineId) {
  if (form.lines.length === 1) return
  form.lines = form.lines.filter((line) => line.id !== lineId)
}

const totals = computed(() => calculateInvoiceTotals(form.lines))

function currency(value) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' }).format(value || 0)
}

function validate() {
  errors.invoiceNumber = form.invoiceNumber.trim() ? '' : 'Invoice number is required.'
  errors.customerId = form.customerId ? '' : 'Select a customer.'
  errors.issueDate = form.issueDate ? '' : 'Issue date is required.'
  errors.dueDate = !form.dueDate
    ? 'Due date is required.'
    : form.dueDate < form.issueDate
      ? 'Due date must be on or after the issue date.'
      : ''
  errors.lines = form.lines.every((line) => line.productId && Number(line.quantity) > 0)
    ? ''
    : 'Every line needs a product and a quantity greater than 0.'

  return !errors.invoiceNumber && !errors.customerId && !errors.issueDate && !errors.dueDate && !errors.lines
}

function submit() {
  if (!validate()) return
  emit('submit', {
    invoiceNumber: form.invoiceNumber,
    customerId: form.customerId,
    issueDate: form.issueDate,
    dueDate: form.dueDate,
    status: form.status,
    lines: form.lines.map((line) => ({ ...line, quantity: Number(line.quantity), unitPrice: Number(line.unitPrice), taxRate: Number(line.taxRate) })),
    ...totals.value,
  })
}
</script>

<template>
  <BaseModal
    :model-value="modelValue"
    :title="invoice ? 'Edit invoice' : 'New invoice'"
    max-width="max-w-3xl"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <form class="space-y-5" @submit.prevent="submit">
      <p v-if="serverError" class="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ serverError }}</p>
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <BaseInput v-model="form.invoiceNumber" label="Invoice number" required :error="errors.invoiceNumber" placeholder="INV-0004" />
        <BaseSelect v-model="form.customerId" label="Customer" required :options="customerOptions" :error="errors.customerId" />
        <BaseInput v-model="form.issueDate" type="date" label="Issue date" required :error="errors.issueDate" />
        <BaseInput v-model="form.dueDate" type="date" label="Due date" required :error="errors.dueDate" />
        <BaseSelect v-model="form.status" label="Status" required :options="STATUS_OPTIONS" />
      </div>

      <div>
        <div class="mb-2 flex items-center justify-between">
          <h3 class="text-sm font-semibold text-slate-700">Lines</h3>
          <button type="button" class="btn btn-ghost text-xs" @click="addLine">+ Add line</button>
        </div>

        <div class="overflow-x-auto rounded-lg ring-1 ring-slate-200">
          <table class="min-w-full divide-y divide-slate-100 text-sm">
            <thead class="bg-slate-50">
              <tr>
                <th class="table-head-cell">Product</th>
                <th class="table-head-cell w-20">Qty</th>
                <th class="table-head-cell w-28">Unit price</th>
                <th class="table-head-cell w-24">Tax %</th>
                <th class="table-head-cell w-28 text-right">Line total</th>
                <th class="w-10"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr v-for="line in form.lines" :key="line.id">
                <td class="p-2">
                  <select v-model="line.productId" class="field-input" @change="onProductChange(line)">
                    <option value="" disabled>Select…</option>
                    <option v-for="option in productOptions" :key="option.value" :value="option.value">
                      {{ option.label }}
                    </option>
                  </select>
                </td>
                <td class="p-2">
                  <input v-model.number="line.quantity" type="number" min="1" class="field-input" @input="recalcLine(line)" />
                </td>
                <td class="p-2">
                  <input v-model.number="line.unitPrice" type="number" min="0" step="0.01" class="field-input" @input="recalcLine(line)" />
                </td>
                <td class="p-2">
                  <input v-model.number="line.taxRate" type="number" min="0" max="100" step="0.01" class="field-input" @input="recalcLine(line)" />
                </td>
                <td class="p-2 text-right font-medium text-slate-700">{{ currency(line.lineTotal) }}</td>
                <td class="p-2 text-center">
                  <button
                    type="button"
                    class="icon-btn hover:text-red-600"
                    :disabled="form.lines.length === 1"
                    aria-label="Remove line"
                    @click="removeLine(line.id)"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
                      <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
                    </svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-if="errors.lines" class="field-error">{{ errors.lines }}</p>
      </div>

      <div class="ml-auto w-full max-w-xs space-y-1 rounded-lg bg-slate-50 p-4 text-sm">
        <div class="flex justify-between text-slate-500">
          <span>Subtotal</span>
          <span>{{ currency(totals.subTotal) }}</span>
        </div>
        <div class="flex justify-between text-slate-500">
          <span>Tax</span>
          <span>{{ currency(totals.totalTax) }}</span>
        </div>
        <div class="flex justify-between border-t border-slate-200 pt-1 font-semibold text-slate-900">
          <span>Grand total</span>
          <span>{{ currency(totals.grandTotal) }}</span>
        </div>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn btn-secondary" :disabled="submitting" @click="$emit('update:modelValue', false)">Cancel</button>
      <button type="button" class="btn btn-primary" :disabled="submitting" @click="submit">
        {{ submitting ? 'Saving…' : invoice ? 'Save changes' : 'Create invoice' }}
      </button>
    </template>
  </BaseModal>
</template>
