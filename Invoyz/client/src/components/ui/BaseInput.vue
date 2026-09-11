<script setup>
defineProps({
  modelValue: { type: [String, Number], default: '' },
  label: { type: String, default: '' },
  type: { type: String, default: 'text' },
  placeholder: { type: String, default: '' },
  error: { type: String, default: '' },
  required: { type: Boolean, default: false },
  step: { type: String, default: undefined },
  min: { type: [String, Number], default: undefined },
})

defineEmits(['update:modelValue'])
</script>

<template>
  <div>
    <label v-if="label" class="field-label">
      {{ label }}
      <span v-if="required" class="text-red-500">*</span>
    </label>
    <input
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :step="step"
      :min="min"
      class="field-input"
      :class="{ 'field-input-error': error }"
      @input="$emit('update:modelValue', $event.target.value)"
    />
    <p v-if="error" class="field-error">{{ error }}</p>
  </div>
</template>
