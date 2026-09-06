<script setup lang="ts">
withDefaults(defineProps<{
  title: string
  description?: string
}>(), {
  description: ''
})
</script>

<template>
  <div class="page-head">
    <div>
      <h1>{{ title }}</h1>
      <p v-if="description">{{ description }}</p>
    </div>
    <div class="page-head__actions"><slot name="actions" /></div>
  </div>
</template>

<style scoped>
.page-head__actions{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
</style>
