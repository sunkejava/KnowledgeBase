<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import type { TableColumn } from '../types/table'
import { systemApi } from '../api/modules/system'

const tab = ref('users')
const loading = ref(false)
const rows = ref<any[]>([])
const roles = ref<any[]>([])
const menus = ref<any[]>([])
const keyword = ref('')
const userDialog = ref(false)
const roleDialog = ref(false)
const user = ref<any>({ userName: '', displayName: '', password: '', enabled: true, roleIds: [] })
const role = ref<any>({ code: '', name: '', enabled: true, menuIds: [] })

const columns = computed<TableColumn<any>[]>(() => {
  if (tab.value === 'users') return [
    { key: 'userName', label: '用户名', width: 160, sortable: true },
    { key: 'displayName', label: '姓名', width: 160 },
    { key: 'roles', label: '角色', minWidth: 220, formatter: row => row.roles?.join('、') || '-' },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' },
    { key: 'createdAt', label: '创建时间', width: 190, formatter: row => formatTime(row.createdAt) }
  ]
  if (tab.value === 'roles') return [
    { key: 'code', label: '角色编码', width: 180, sortable: true },
    { key: 'name', label: '角色名称', minWidth: 200 },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' },
    { key: 'menuIds', label: '已授权菜单数', width: 130, formatter: row => row.menuIds?.length || 0 }
  ]
  if (tab.value === 'departments') return [
    { key: 'name', label: '部门名称', minWidth: 220 },
    { key: 'parentId', label: '上级节点', width: 260, formatter: row => row.parentId || '-' },
    { key: 'sort', label: '排序', width: 100, sortable: true },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' }
  ]
  if (tab.value === 'organizations') return [
    { key: 'name', label: '组织名称', minWidth: 220 },
    { key: 'code', label: '组织编码', width: 180 },
    { key: 'parentId', label: '上级节点', width: 260, formatter: row => row.parentId || '-' },
    { key: 'sort', label: '排序', width: 100, sortable: true },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' }
  ]
  if (tab.value === 'menus') return [
    { key: 'name', label: '菜单名称', width: 180 },
    { key: 'type', label: '类型', width: 100 },
    { key: 'path', label: '路由', minWidth: 220 },
    { key: 'permission', label: '权限标识', minWidth: 220 },
    { key: 'sort', label: '排序', width: 100, sortable: true },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' }
  ]
  return [
    { key: 'createdAt', label: '时间', width: 190, sortable: true, formatter: row => formatTime(row.createdAt) },
    { key: 'category', label: '类别', width: 120 },
    { key: 'userName', label: '用户', width: 140 },
    { key: 'action', label: '操作', width: 160 },
    { key: 'target', label: '对象', minWidth: 300 },
    { key: 'ipAddress', label: 'IP', width: 150 },
    { key: 'success', label: '结果', width: 100, formatter: row => row.success ? '成功' : '失败' },
    { key: 'message', label: '说明', minWidth: 260, formatter: row => row.message || '-' }
  ]
})

const filteredRows = computed(() => {
  const value = keyword.value.trim().toLowerCase()
  if (!value) return rows.value
  return rows.value.filter(row => Object.values(row).some(item => String(item ?? '').toLowerCase().includes(value)))
})

function formatTime(value: string) {
  return value ? new Date(value).toLocaleString() : '-'
}

async function load() {
  loading.value = true
  try {
    if (tab.value === 'users') {
      const [data, roleData] = await Promise.all([systemApi.users(), systemApi.roles()])
      rows.value = data.data
      roles.value = roleData.data
    } else if (tab.value === 'roles') {
      const [data, menuData] = await Promise.all([systemApi.roles(), systemApi.menus()])
      rows.value = data.data
      menus.value = menuData.data
    } else if (tab.value === 'departments') rows.value = (await systemApi.departments()).data
    else if (tab.value === 'organizations') rows.value = (await systemApi.organizations()).data
    else if (tab.value === 'menus') rows.value = (await systemApi.menus()).data
    else rows.value = (await systemApi.auditLogs()).data
  } finally {
    loading.value = false
  }
}

async function saveUser() {
  const id = user.value.id
  if (id) await systemApi.updateUser(id, user.value)
  else await systemApi.createUser(user.value)
  userDialog.value = false
  ElMessage.success('用户已保存')
  await load()
}

async function saveRole() {
  const id = role.value.id
  if (id) await systemApi.updateRole(id, role.value)
  else await systemApi.createRole(role.value)
  roleDialog.value = false
  ElMessage.success('角色已保存')
  await load()
}

async function remove(row: any) {
  await ElMessageBox.confirm(`确认删除“${row.displayName || row.name || row.code}”？`, '删除确认', { type: 'warning' })
  if (tab.value === 'users') await systemApi.deleteUser(row.id)
  else if (tab.value === 'roles') await systemApi.deleteRole(row.id)
  await load()
}

function addUser() {
  user.value = { userName: '', displayName: '', password: '', enabled: true, roleIds: [] }
  userDialog.value = true
}
function editUser(row: any) {
  user.value = { ...row, password: '', roleIds: [...(row.roleIds || [])] }
  userDialog.value = true
}
function addRole() {
  role.value = { code: '', name: '', enabled: true, menuIds: [] }
  roleDialog.value = true
}
function editRole(row: any) {
  role.value = { ...row, menuIds: [...(row.menuIds || [])] }
  roleDialog.value = true
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>系统管理</h1><p>账号、角色、组织、菜单与审计统一维护。</p></div></div>
    <el-tabs v-model="tab" @tab-change="() => { keyword=''; load() }">
      <el-tab-pane label="用户" name="users"/><el-tab-pane label="角色" name="roles"/>
      <el-tab-pane label="部门" name="departments"/><el-tab-pane label="组织机构" name="organizations"/>
      <el-tab-pane label="菜单权限" name="menus"/><el-tab-pane label="审计日志" name="audit"/>
    </el-tabs>

    <BaseDataTable
      :rows="filteredRows"
      :columns="columns"
      :loading="loading"
      :storage-key="`system-${tab}`"
      :export-file-name="`KnowledgeBase-${tab}`"
      :show-actions="tab==='users'||tab==='roles'"
      :action-width="170"
      @refresh="load"
    >
      <template #toolbar>
        <el-input v-model="keyword" clearable placeholder="输入关键字筛选当前列表" style="width:280px" />
        <el-button v-if="tab==='users'" type="primary" @click="addUser">新增用户</el-button>
        <el-button v-if="tab==='roles'" type="primary" @click="addRole">新增角色</el-button>
      </template>
      <template #actions="{ row }">
        <template v-if="tab==='users'||tab==='roles'">
          <el-button v-if="tab==='users'" link type="primary" @click="editUser(row)">编辑</el-button>
          <el-button v-else link type="primary" @click="editRole(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </template>
    </BaseDataTable>

    <el-dialog v-model="userDialog" title="用户维护" width="520">
      <el-form label-width="90">
        <el-form-item label="用户名"><el-input v-model="user.userName" :disabled="!!user.id"/></el-form-item>
        <el-form-item label="姓名"><el-input v-model="user.displayName"/></el-form-item>
        <el-form-item label="密码"><el-input v-model="user.password" type="password" show-password placeholder="编辑时留空表示不修改"/></el-form-item>
        <el-form-item label="角色"><el-select v-model="user.roleIds" multiple style="width:100%"><el-option v-for="item in roles" :key="item.id" :label="item.name" :value="item.id"/></el-select></el-form-item>
      </el-form>
      <template #footer><el-button @click="userDialog=false">取消</el-button><el-button type="primary" @click="saveUser">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="roleDialog" title="角色维护" width="560">
      <el-form label-width="90">
        <el-form-item label="编码"><el-input v-model="role.code"/></el-form-item>
        <el-form-item label="名称"><el-input v-model="role.name"/></el-form-item>
        <el-form-item label="菜单权限"><el-select v-model="role.menuIds" multiple filterable style="width:100%"><el-option v-for="item in menus" :key="item.id" :label="item.name+' · '+item.permission" :value="item.id"/></el-select></el-form-item>
      </el-form>
      <template #footer><el-button @click="roleDialog=false">取消</el-button><el-button type="primary" @click="saveRole">保存</el-button></template>
    </el-dialog>
  </section>
</template>
