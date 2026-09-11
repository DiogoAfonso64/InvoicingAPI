<script setup>
import BaseModal from './BaseModal.vue'

defineProps({
  modelValue: { type: Boolean, default: false },
  title: { type: String, default: 'Are you sure?' },
  message: { type: String, default: 'This action cannot be undone.' },
  confirmLabel: { type: String, default: 'Delete' },
})

const emit = defineEmits(['update:modelValue', 'confirm'])

function confirm() {
  emit('confirm')
  emit('update:modelValue', false)
}
</script>

<template>
  <BaseModal :model-value="modelValue" :title="title" max-width="max-w-sm" @update:model-value="$emit('update:modelValue', $event)">
    <p class="text-sm text-slate-600">{{ message }}</p>

    <template #footer>
      <button type="button" class="btn btn-secondary" @click="$emit('update:modelValue', false)">Cancel</button>
      <button type="button" class="btn btn-danger" @click="confirm">{{ confirmLabel }}</button>
    </template>
  </BaseModal>
</template>
