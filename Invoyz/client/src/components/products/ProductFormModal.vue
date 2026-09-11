<script setup>
import { reactive, watch } from 'vue'
import BaseModal from '../ui/BaseModal.vue'
import BaseInput from '../ui/BaseInput.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  product: { type: Object, default: null },
  submitting: { type: Boolean, default: false },
  serverError: { type: String, default: '' },
})

const emit = defineEmits(['update:modelValue', 'submit'])

const form = reactive({ name: '', description: '', unitPrice: '', taxRate: '' })
const errors = reactive({ name: '', unitPrice: '', taxRate: '' })

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    form.name = props.product?.name ?? ''
    form.description = props.product?.description ?? ''
    form.unitPrice = props.product?.unitPrice ?? ''
    form.taxRate = props.product?.taxRate ?? ''
    errors.name = errors.unitPrice = errors.taxRate = ''
  },
)

function validate() {
  errors.name = form.name.trim() ? '' : 'Name is required.'
  errors.unitPrice =
    form.unitPrice !== '' && Number(form.unitPrice) >= 0 ? '' : 'Unit price must be 0 or more.'
  errors.taxRate =
    form.taxRate !== '' && Number(form.taxRate) >= 0 && Number(form.taxRate) <= 100
      ? ''
      : 'Tax rate must be between 0 and 100.'

  return !errors.name && !errors.unitPrice && !errors.taxRate
}

function submit() {
  if (!validate()) return
  emit('submit', {
    name: form.name,
    description: form.description,
    unitPrice: Number(form.unitPrice),
    taxRate: Number(form.taxRate),
  })
}
</script>

<template>
  <BaseModal
    :model-value="modelValue"
    :title="product ? 'Edit product' : 'New product'"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <form class="space-y-4" @submit.prevent="submit">
      <p v-if="serverError" class="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ serverError }}</p>
      <BaseInput v-model="form.name" label="Name" required :error="errors.name" placeholder="Consulting Hour" />
      <div>
        <label class="field-label">Description</label>
        <textarea v-model="form.description" rows="2" class="field-input resize-none" placeholder="Optional" />
      </div>
      <div class="grid grid-cols-2 gap-4">
        <BaseInput
          v-model="form.unitPrice"
          type="number"
          step="0.01"
          min="0"
          label="Unit price"
          required
          :error="errors.unitPrice"
        />
        <BaseInput
          v-model="form.taxRate"
          type="number"
          step="0.01"
          min="0"
          label="Tax rate (%)"
          required
          :error="errors.taxRate"
        />
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn btn-secondary" :disabled="submitting" @click="$emit('update:modelValue', false)">Cancel</button>
      <button type="button" class="btn btn-primary" :disabled="submitting" @click="submit">
        {{ submitting ? 'Saving…' : product ? 'Save changes' : 'Add product' }}
      </button>
    </template>
  </BaseModal>
</template>
