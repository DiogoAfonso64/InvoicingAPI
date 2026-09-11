<script setup>
import { reactive, watch } from 'vue'
import BaseModal from '../ui/BaseModal.vue'
import BaseInput from '../ui/BaseInput.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  customer: { type: Object, default: null },
  submitting: { type: Boolean, default: false },
  serverError: { type: String, default: '' },
})

const emit = defineEmits(['update:modelValue', 'submit'])

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/
const VAT_PATTERN = /^[A-Z]{2}[0-9A-Z]{2,13}$/

const form = reactive({ name: '', email: '', address: '', vatNumber: '' })
const errors = reactive({ name: '', email: '', address: '', vatNumber: '' })

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    form.name = props.customer?.name ?? ''
    form.email = props.customer?.email ?? ''
    form.address = props.customer?.address ?? ''
    form.vatNumber = props.customer?.vatNumber ?? ''
    errors.name = errors.email = errors.address = errors.vatNumber = ''
  },
)

function validate() {
  errors.name = form.name.trim() ? '' : 'Name is required.'
  errors.email = !form.email.trim()
    ? 'Email is required.'
    : EMAIL_PATTERN.test(form.email)
      ? ''
      : 'Enter a valid email address.'
  errors.address = form.address.trim() ? '' : 'Address is required.'
  errors.vatNumber = !form.vatNumber.trim()
    ? 'VAT number is required.'
    : VAT_PATTERN.test(form.vatNumber)
      ? ''
      : 'Format: country prefix + digits, e.g. PT123456789.'

  return !errors.name && !errors.email && !errors.address && !errors.vatNumber
}

function submit() {
  if (!validate()) return
  emit('submit', { ...form })
}
</script>

<template>
  <BaseModal
    :model-value="modelValue"
    :title="customer ? 'Edit customer' : 'New customer'"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <form class="space-y-4" @submit.prevent="submit">
      <p v-if="serverError" class="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{{ serverError }}</p>
      <BaseInput v-model="form.name" label="Name" required :error="errors.name" placeholder="Acme Corp" />
      <BaseInput v-model="form.email" label="Email" required :error="errors.email" placeholder="billing@acme.test" />
      <BaseInput v-model="form.address" label="Address" required :error="errors.address" placeholder="1 Industrial Way, Lisbon" />
      <BaseInput v-model="form.vatNumber" label="VAT number" required :error="errors.vatNumber" placeholder="PT123456789" />
    </form>

    <template #footer>
      <button type="button" class="btn btn-secondary" :disabled="submitting" @click="$emit('update:modelValue', false)">Cancel</button>
      <button type="button" class="btn btn-primary" :disabled="submitting" @click="submit">
        {{ submitting ? 'Saving…' : customer ? 'Save changes' : 'Add customer' }}
      </button>
    </template>
  </BaseModal>
</template>
