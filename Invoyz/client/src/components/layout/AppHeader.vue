<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const navItems = [
  { to: '/customers', label: 'Customers' },
  { to: '/products', label: 'Products' },
  { to: '/invoices', label: 'Invoices' },
]

const route = useRoute()
const router = useRouter()

const current = computed({
  get: () => navItems.find((item) => route.path.startsWith(item.to))?.to ?? navItems[0].to,
  set: (to) => router.push(to),
})
</script>

<template>
  <header class="border-b border-slate-200 bg-white">
    <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-4 py-4 sm:px-6">
      <div class="flex items-center gap-2">
        <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-sm font-bold text-white">
          I
        </div>
        <span class="text-lg font-semibold tracking-tight text-slate-900">Invoyz</span>
      </div>

      <select v-model="current" class="field-input w-44 font-medium" aria-label="Navigate to section">
        <option v-for="item in navItems" :key="item.to" :value="item.to">{{ item.label }}</option>
      </select>
    </div>
  </header>
</template>
