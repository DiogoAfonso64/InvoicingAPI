<script setup>
import { computed } from 'vue'

const props = defineProps({
  status: { type: String, required: true },
})

const styles = {
  Draft: 'bg-slate-100 text-slate-600',
  Sent: 'bg-blue-50 text-blue-700',
  Paid: 'bg-emerald-50 text-emerald-700',
  Overdue: 'bg-red-50 text-red-700',
}

const classes = computed(() => styles[props.status] ?? 'bg-slate-100 text-slate-600')
</script>

<template>
  <span class="badge" :class="classes">{{ status }}</span>
</template>
