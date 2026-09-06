<script setup lang="ts">
import { computed } from 'vue'
import { Plus } from 'lucide-vue-next'
import BaseDataTable from './BaseDataTable.vue'
import type { TableColumn } from '../../types/table'

interface TreeItem {
  id: string
  parentId?: string | null
  name: string
  [key: string]: any
}

const props = withDefaults(defineProps<{
  items: TreeItem[]
  columns: TableColumn<any>[]
  loading?: boolean
  storageKey: string
  exportFileName: string
  addText?: string
  emptyText?: string
}>(), {
  loading: false,
  addText: '新增节点',
  emptyText: '暂无数据'
})

const emit = defineEmits<{
  add: [parent: TreeItem | null]
  edit: [row: TreeItem]
  remove: [row: TreeItem]
  refresh: []
}>()

const flattened = computed(() => {
  const children = new Map<string | null, TreeItem[]>()
  props.items.forEach(item => {
    const parentKey = item.parentId || null
    const list = children.get(parentKey) || []
    list.push(item)
    children.set(parentKey, list)
  })

  const result: any[] = []
  const visit = (parentId: string | null, depth: number) => {
    const list = [...(children.get(parentId) || [])].sort((a, b) => Number(a.sort || 0) - Number(b.sort || 0))
    for (const item of list) {
      result.push({ ...item, __depth: depth, __displayName: `${depth ? '　'.repeat(depth) + '└ ' : ''}${item.name}` })
      visit(item.id, depth + 1)
    }
  }
  visit(null, 0)
  return result
})

const resolvedColumns = computed<TableColumn<any>[]>(() => props.columns.map(column =>
  column.key === 'name'
    ? { ...column, prop: '__displayName', formatter: row => row.__displayName }
    : column))
</script>

<template>
  <BaseDataTable
    :rows="flattened"
    :columns="resolvedColumns"
    :loading="loading"
    :storage-key="storageKey"
    :export-file-name="exportFileName"
    :show-selection="false"
    :action-width="220"
    @refresh="emit('refresh')"
  >
    <template #toolbar>
      <el-button type="primary" @click="emit('add', null)"><Plus :size="15" />{{ addText }}</el-button>
      <slot name="toolbar" />
    </template>
    <template #actions="{ row }">
      <el-button link type="primary" @click="emit('add', row)">新增子级</el-button>
      <el-button link type="primary" @click="emit('edit', row)">编辑</el-button>
      <el-button link type="danger" @click="emit('remove', row)">删除</el-button>
    </template>
    <template #empty>{{ emptyText }}</template>
  </BaseDataTable>
</template>
