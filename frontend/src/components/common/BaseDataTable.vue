<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { Download, RefreshCw, RotateCcw, Settings2 } from 'lucide-vue-next'
import type { TableColumn, TableColumnPreference } from '../../types/table'
import { exportRowsToCsv } from '../../utils/exportCsv'

const props = withDefaults(defineProps<{
  rows: Record<string, any>[]
  columns: TableColumn<any>[]
  loading?: boolean
  storageKey: string
  rowKey?: string
  defaultPageSize?: number
  pageSizes?: number[]
  exportFileName?: string
  showExport?: boolean
  showColumnSetting?: boolean
  showRefresh?: boolean
  actionWidth?: number
}>(), {
  loading: false,
  rowKey: 'id',
  defaultPageSize: 20,
  pageSizes: () => [10, 20, 50, 100],
  exportFileName: 'export',
  showExport: true,
  showColumnSetting: true,
  showRefresh: true,
  actionWidth: 180
})

const emit = defineEmits<{
  refresh: []
  rowDblclick: [row: Record<string, any>]
}>()

const currentPage = ref(1)
const pageSize = ref(props.defaultPageSize)
const preferences = reactive<Record<string, TableColumnPreference>>({})

function preferenceStorageKey() {
  return `kb_table_columns_${props.storageKey}`
}

function loadPreferences() {
  try {
    const saved = JSON.parse(localStorage.getItem(preferenceStorageKey()) || '{}')
    Object.assign(preferences, saved)
  } catch {
    Object.keys(preferences).forEach(key => delete preferences[key])
  }
}

function savePreferences() {
  localStorage.setItem(preferenceStorageKey(), JSON.stringify(preferences))
}

loadPreferences()

const resolvedColumns = computed(() => props.columns.map(column => ({
  ...column,
  visible: preferences[column.key]?.visible ?? column.visible ?? true,
  width: preferences[column.key]?.width ?? column.width
})))

const visibleColumns = computed(() => resolvedColumns.value.filter(column => column.visible))
const total = computed(() => props.rows.length)
const pagedRows = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return props.rows.slice(start, start + pageSize.value)
})

watch(() => props.rows.length, () => {
  const maxPage = Math.max(1, Math.ceil(total.value / pageSize.value))
  if (currentPage.value > maxPage) currentPage.value = maxPage
})

function valueOf(row: Record<string, any>, column: TableColumn<any>) {
  return column.formatter ? column.formatter(row) : row[column.prop || column.key]
}

function updateVisible(key: string, visible: boolean) {
  preferences[key] = { ...(preferences[key] || { visible: true }), visible }
  savePreferences()
}

function resetColumns() {
  Object.keys(preferences).forEach(key => delete preferences[key])
  localStorage.removeItem(preferenceStorageKey())
}

function onHeaderDragend(newWidth: number, _oldWidth: number, column: any) {
  const target = props.columns.find(item => (item.prop || item.key) === column.property || item.label === column.label)
  if (!target) return
  preferences[target.key] = { visible: preferences[target.key]?.visible ?? target.visible ?? true, width: Math.round(newWidth) }
  savePreferences()
}

function exportData() {
  exportRowsToCsv(props.exportFileName, visibleColumns.value, props.rows)
}

function onSizeChange() {
  currentPage.value = 1
}
</script>

<template>
  <div class="base-data-table">
    <div class="base-table-toolbar">
      <div class="base-table-toolbar__left"><slot name="toolbar" /></div>
      <div class="base-table-toolbar__right">
        <el-button v-if="showRefresh" @click="emit('refresh')"><RefreshCw :size="15" />刷新</el-button>
        <el-button v-if="showExport" @click="exportData"><Download :size="15" />导出当前数据</el-button>
        <el-popover v-if="showColumnSetting" placement="bottom-end" :width="320" trigger="click">
          <template #reference><el-button><Settings2 :size="15" />列设置</el-button></template>
          <div class="column-setting-head"><strong>显示列与列宽</strong><el-button link @click="resetColumns"><RotateCcw :size="14" />恢复默认</el-button></div>
          <div class="column-setting-list">
            <div v-for="column in resolvedColumns" :key="column.key" class="column-setting-row">
              <el-checkbox :model-value="column.visible" @change="value => updateVisible(column.key, Boolean(value))">{{ column.label }}</el-checkbox>
              <span>{{ column.width ? `${column.width}px` : '自适应' }}</span>
            </div>
          </div>
          <div class="column-setting-tip">可直接拖动表头分隔线调整列宽，设置会自动保存到当前浏览器。</div>
        </el-popover>
      </div>
    </div>

    <el-table
      :data="pagedRows"
      :row-key="rowKey"
      :loading="loading"
      stripe
      border
      table-layout="fixed"
      @row-dblclick="row => emit('rowDblclick', row)"
      @header-dragend="onHeaderDragend"
    >
      <el-table-column
        v-for="column in visibleColumns"
        :key="column.key"
        :prop="column.prop || column.key"
        :label="column.label"
        :width="column.width"
        :min-width="column.minWidth"
        :sortable="column.sortable"
        :align="column.align || 'left'"
        :fixed="column.fixed"
        resizable
        show-overflow-tooltip
      >
        <template #default="scope">
          <slot :name="`cell-${column.key}`" :row="scope.row" :value="valueOf(scope.row, column)">
            {{ valueOf(scope.row, column) }}
          </slot>
        </template>
      </el-table-column>
      <el-table-column v-if="$slots.actions" label="操作" fixed="right" :width="actionWidth">
        <template #default="scope"><slot name="actions" :row="scope.row" /></template>
      </el-table-column>
      <template #empty><slot name="empty">暂无数据</slot></template>
    </el-table>

    <div class="base-table-pagination">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        background
        :page-sizes="pageSizes"
        :total="total"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onSizeChange"
      />
    </div>
  </div>
</template>

<style scoped>
.base-data-table{background:var(--surface);border:1px solid var(--border);border-radius:8px;overflow:hidden}.base-table-toolbar{min-height:52px;padding:8px 12px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:12px}.base-table-toolbar__left,.base-table-toolbar__right{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.base-table-pagination{display:flex;justify-content:flex-end;padding:14px 12px;border-top:1px solid var(--border)}.column-setting-head{display:flex;justify-content:space-between;align-items:center;padding-bottom:8px;border-bottom:1px solid var(--border)}.column-setting-list{max-height:320px;overflow:auto}.column-setting-row{display:flex;justify-content:space-between;align-items:center;padding:8px 2px;border-bottom:1px solid var(--border)}.column-setting-row span,.column-setting-tip{font-size:11px;color:var(--muted)}.column-setting-tip{padding-top:10px;line-height:1.6}
</style>
