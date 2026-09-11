<script setup>
import { computed, reactive, watch } from 'vue'
import BaseModal from '../ui/BaseModal.vue'
import BaseInput from '../ui/BaseInput.vue'
import { calculateLineTotals } from '../../utils/invoiceTotals'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  line: { type: Object, default: null },
  products: { type: Array, default: () => [] },
  submitting: { type: Boolean, default: false },
  serverError: { type: String, default: '' },
})

const emit = defineEmits(['update:modelValue', 'submit'])

const form = reactive({ productId: '', quantity: 1, unitPrice: 0, taxRate: 0 })
const errors = reactive({ productId: '', quantity: '' })

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    form.productId = props.line?.productId ?? ''
    form.quantity = props.line?.quantity ?? 1
    form.unitPrice = props.line?.unitPrice ?? 0
    form.taxRate = props.line?.taxRate ?? 0
    errors.productId = errors.quantity = ''
  },
)

function onProductChange() {
  const product = props.products.find((p) => p.id === form.productId)
  if (product) {
    form.unitPrice = product.unitPrice
    form.taxRate = product.taxRate
  }
}

const preview = computed(() =>
  calculateLineTotals(Number(form.quantity), Number(form.unitPrice), Number(form.taxRate)),
)

function currency(value) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' }).format(value || 0)
}

function validate() {
  errors.productId = form.productId ? '' : 'Select a product.'
  errors.quantity = Number(form.quantity) > 0 ? '' : 'Quantity must be greater than 0.'
  return !errors.productId && !errors.quantity
}

function submit() {
  if (!validate()) return
  emit('submit', {
    productId: form.productId,
    quantity: Number(form.quantity),
    unitPrice: Number(form.unitPrice),
    taxRate: Number(form.taxRate),
    ...preview.value,
  })
}
</script>

<template>
  <BaseModal
    :model-value="modelValue"
    title="Edit invoice line"
    max-width="max-w-md"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <form class="space-y-4" @submit.prevent="submit">
      <p v-if="serverError" class="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ serverError }}</p>
      <div>
        <label class="field-label">
          Product <span class="text-red-500">*</span>
        </label>
        <select
          v-model="form.productId"
          class="field-input"
          :class="{ 'field-input-error': errors.productId }"
          @change="onProductChange"
        >
          <option value="" disabled>Select…</option>
          <option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }}</option>
        </select>
        <p v-if="errors.productId" class="field-error">{{ errors.productId }}</p>
      </div>

      <div class="grid grid-cols-2 gap-4">
        <BaseInput v-model="form.quantity" type="number" min="1" label="Quantity" required :error="errors.quantity" />
        <BaseInput v-model="form.unitPrice" type="number" min="0" step="0.01" label="Unit price" />
        <BaseInput v-model="form.taxRate" type="number" min="0" max="100" step="0.01" label="Tax rate (%)" />
      </div>

      <div class="flex items-center justify-between rounded-lg bg-slate-50 p-3 text-sm">
        <span class="text-slate-500">Line total (tax {{ currency(preview.lineTax) }})</span>
        <span class="font-semibold text-slate-900">{{ currency(preview.lineTotal) }}</span>
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn btn-secondary" :disabled="submitting" @click="$emit('update:modelValue', false)">Cancel</button>
      <button type="button" class="btn btn-primary" :disabled="submitting" @click="submit">
        {{ submitting ? 'Saving…' : 'Save changes' }}
      </button>
    </template>
  </BaseModal>
</template>
