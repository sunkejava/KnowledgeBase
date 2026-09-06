<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../components/common/BaseDataTable.vue'
import BaseTreeManager from '../components/common/BaseTreeManager.vue'
import type { TableColumn } from '../types/table'
import { systemApi } from '../api/modules/system'

const tab = ref('users')
const loading = ref(false)
const rows = ref<any[]>([])
const roles = ref<any[]>([])
const menus = ref<any[]>([])
const keyword = ref('')
const total = ref(0)
const pageQuery = reactive({ page: 1, pageSize: 20 })

const userDialog = ref(false)
const roleDialog = ref(false)
const treeDialog = ref(false)
const treeMode = ref<'departments' | 'organizations' | 'menus'>('departments')
const user = ref<any>({ userName: '', displayName: '', password: '', enabled: true, roleIds: [] })
const role = ref<any>({ code: '', name: '', enabled: true, menuIds: [] })
const treeForm = reactive<any>({ id: '', parentId: null, name: '', code: '', path: '', permission: '', type: 'Menu', icon: '', sort: 0, enabled: true })

const isRemoteTab = computed(() => tab.value === 'users' || tab.value === 'audit')
const isTreeTab = computed(() => ['departments', 'organizations', 'menus'].includes(tab.value))

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
    { key: 'name', label: '部门名称', minWidth: 260 },
    { key: 'sort', label: '排序', width: 100, sortable: true },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' },
    { key: 'id', label: '节点 ID', minWidth: 280, visible: false }
  ]
  if (tab.value === 'organizations') return [
    { key: 'name', label: '组织名称', minWidth: 260 },
    { key: 'code', label: '组织编码', width: 180 },
    { key: 'sort', label: '排序', width: 100, sortable: true },
    { key: 'enabled', label: '状态', width: 100, formatter: row => row.enabled ? '启用' : '停用' },
    { key: 'id', label: '节点 ID', minWidth: 280, visible: false }
  ]
  if (tab.value === 'menus') return [
    { key: 'name', label: '菜单名称', minWidth: 220 },
    { key: 'type', label: '类型', width: 100 },
    { key: 'path', label: '路由', minWidth: 220 },
    { key: 'permission', label: '权限标识', minWidth: 220 },
    { key: 'icon', label: '图标', width: 120, visible: false },
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
  if (isRemoteTab.value) return rows.value
  const value = keyword.value.trim().toLowerCase()
  if (!value) return rows.value
  return rows.value.filter(row => Object.values(row).some(item => String(item ?? '').toLowerCase().includes(value)))
})

const parentOptions = computed(() => rows.value.filter(item => item.id !== treeForm.id))

function formatTime(value: string) {
  return value ? new Date(value).toLocaleString() : '-'
}

async function load() {
  loading.value = true
  try {
    if (tab.value === 'users') {
      const [data, roleData] = await Promise.all([
        systemApi.users({ page: pageQuery.page, pageSize: pageQuery.pageSize, keyword: keyword.value || undefined }),
        systemApi.roles()
      ])
      rows.value = data.data.items
      total.value = data.data.total
      roles.value = roleData.data
    } else if (tab.value === 'roles') {
      const [data, menuData] = await Promise.all([systemApi.roles(), systemApi.menus()])
      rows.value = data.data
      menus.value = menuData.data
      total.value = rows.value.length
    } else if (tab.value === 'departments') {
      rows.value = (await systemApi.departments()).data
      total.value = rows.value.length
    } else if (tab.value === 'organizations') {
      rows.value = (await systemApi.organizations()).data
      total.value = rows.value.length
    } else if (tab.value === 'menus') {
      rows.value = (await systemApi.menus()).data
      menus.value = rows.value
      total.value = rows.value.length
    } else {
      const data = await systemApi.auditLogs({ page: pageQuery.page, pageSize: pageQuery.pageSize, keyword: keyword.value || undefined })
      rows.value = data.data.items
      total.value = data.data.total
    }
  } finally {
    loading.value = false
  }
}

async function changeTab() {
  keyword.value = ''
  pageQuery.page = 1
  await load()
}

async function search() {
  pageQuery.page = 1
  await load()
}

async function onPageChange(page: number, pageSize: number) {
  pageQuery.page = page
  pageQuery.pageSize = pageSize
  await load()
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

async function removeTableRow(row: any) {
  await ElMessageBox.confirm(`确认删除“${row.displayName || row.name || row.code}”？`, '删除确认', { type: 'warning' })
  if (tab.value === 'users') await systemApi.deleteUser(row.id)
  else if (tab.value === 'roles') await systemApi.deleteRole(row.id)
  ElMessage.success('已删除')
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

function openTreeDialog(parent: any | null, editing?: any) {
  treeMode.value = tab.value as typeof treeMode.value
  Object.assign(treeForm, {
    id: editing?.id || '',
    parentId: editing?.parentId ?? parent?.id ?? null,
    name: editing?.name || '',
    code: editing?.code || '',
    path: editing?.path || '',
    permission: editing?.permission || '',
    type: editing?.type || 'Menu',
    icon: editing?.icon || '',
    sort: editing?.sort || 0,
    enabled: editing?.enabled ?? true
  })
  treeDialog.value = true
}

async function saveTreeNode() {
  if (!treeForm.name.trim()) return ElMessage.warning('请输入名称')
  const id = treeForm.id || null
  if (treeMode.value === 'departments') {
    const payload = { parentId: treeForm.parentId || null, name: treeForm.name, sort: Number(treeForm.sort), enabled: treeForm.enabled }
    if (id) await systemApi.updateDepartment(id, payload)
    else await systemApi.createDepartment(payload)
  } else if (treeMode.value === 'organizations') {
    const payload = { parentId: treeForm.parentId || null, name: treeForm.name, code: treeForm.code, sort: Number(treeForm.sort), enabled: treeForm.enabled }
    if (id) await systemApi.updateOrganization(id, payload)
    else await systemApi.createOrganization(payload)
  } else {
    const payload = {
      parentId: treeForm.parentId || null,
      name: treeForm.name,
      path: treeForm.path,
      permission: treeForm.permission,
      type: treeForm.type,
      icon: treeForm.icon,
      sort: Number(treeForm.sort),
      enabled: treeForm.enabled
    }
    if (id) await systemApi.updateMenu(id, payload)
    else await systemApi.createMenu(payload)
  }
  treeDialog.value = false
  ElMessage.success('节点已保存')
  await load()
}

async function removeTreeNode(row: any) {
  await ElMessageBox.confirm(`确认删除“${row.name}”？请先确认该节点没有仍需保留的子级。`, '删除确认', { type: 'warning' })
  if (tab.value === 'departments') await systemApi.deleteDepartment(row.id)
  else if (tab.value === 'organizations') await systemApi.deleteOrganization(row.id)
  else await systemApi.deleteMenu(row.id)
  ElMessage.success('节点已删除')
  await load()
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>系统管理</h1><p>账号、角色、部门、组织机构、菜单权限与审计日志统一维护。</p></div></div>
    <el-tabs v-model="tab" @tab-change="changeTab">
      <el-tab-pane label="用户" name="users"/><el-tab-pane label="角色" name="roles"/>
      <el-tab-pane label="部门" name="departments"/><el-tab-pane label="组织机构" name="organizations"/>
      <el-tab-pane label="菜单权限" name="menus"/><el-tab-pane label="审计日志" name="audit"/>
    </el-tabs>

    <BaseTreeManager
      v-if="isTreeTab"
      :items="rows"
      :columns="columns"
      :loading="loading"
      :storage-key="`system-tree-${tab}`"
      :export-file-name="`KnowledgeBase-${tab}`"
      :add-text="tab==='departments'?'新增部门':tab==='organizations'?'新增组织':'新增菜单'"
      @add="parent => openTreeDialog(parent)"
      @edit="row => openTreeDialog(null, row)"
      @remove="removeTreeNode"
      @refresh="load"
    >
      <template #toolbar>
        <el-input v-model="keyword" clearable placeholder="筛选当前树节点" style="width:260px" />
      </template>
    </BaseTreeManager>

    <BaseDataTable
      v-else
      :rows="filteredRows"
      :columns="columns"
      :loading="loading"
      :storage-key="`system-${tab}`"
      :export-file-name="`KnowledgeBase-${tab}`"
      :server-paging="isRemoteTab"
      :total-count="total"
      :show-actions="tab==='users'||tab==='roles'"
      :action-width="170"
      @refresh="load"
      @page-change="onPageChange"
    >
      <template #toolbar>
        <el-input v-model="keyword" clearable :placeholder="isRemoteTab?'输入关键字后搜索':'输入关键字筛选当前列表'" style="width:280px" @keyup.enter="search" />
        <el-button v-if="isRemoteTab" @click="search">查询</el-button>
        <el-button v-if="tab==='users'" type="primary" @click="addUser">新增用户</el-button>
        <el-button v-if="tab==='roles'" type="primary" @click="addRole">新增角色</el-button>
      </template>
      <template #actions="{ row }">
        <el-button v-if="tab==='users'" link type="primary" @click="editUser(row)">编辑</el-button>
        <el-button v-if="tab==='roles'" link type="primary" @click="editRole(row)">编辑</el-button>
        <el-button v-if="tab==='users'||tab==='roles'" link type="danger" @click="removeTableRow(row)">删除</el-button>
      </template>
    </BaseDataTable>

    <el-dialog v-model="userDialog" title="用户维护" width="520">
      <el-form label-width="90">
        <el-form-item label="用户名"><el-input v-model="user.userName" :disabled="!!user.id"/></el-form-item>
        <el-form-item label="姓名"><el-input v-model="user.displayName"/></el-form-item>
        <el-form-item label="密码"><el-input v-model="user.password" type="password" show-password placeholder="编辑时留空表示不修改"/></el-form-item>
        <el-form-item label="角色"><el-select v-model="user.roleIds" multiple style="width:100%"><el-option v-for="item in roles" :key="item.id" :label="item.name" :value="item.id"/></el-select></el-form-item>
        <el-form-item label="状态"><el-switch v-model="user.enabled" active-text="启用" inactive-text="停用"/></el-form-item>
      </el-form>
      <template #footer><el-button @click="userDialog=false">取消</el-button><el-button type="primary" @click="saveUser">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="roleDialog" title="角色维护" width="620">
      <el-form label-width="90">
        <el-form-item label="编码"><el-input v-model="role.code"/></el-form-item>
        <el-form-item label="名称"><el-input v-model="role.name"/></el-form-item>
        <el-form-item label="菜单权限"><el-select v-model="role.menuIds" multiple filterable collapse-tags style="width:100%"><el-option v-for="item in menus" :key="item.id" :label="item.name+' · '+item.permission" :value="item.id"/></el-select></el-form-item>
        <el-form-item label="状态"><el-switch v-model="role.enabled" active-text="启用" inactive-text="停用"/></el-form-item>
      </el-form>
      <template #footer><el-button @click="roleDialog=false">取消</el-button><el-button type="primary" @click="saveRole">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="treeDialog" :title="treeForm.id?'编辑节点':'新增节点'" width="620">
      <el-form label-width="100">
        <el-form-item label="上级节点">
          <el-select v-model="treeForm.parentId" clearable filterable style="width:100%" placeholder="不选择表示根节点">
            <el-option v-for="item in parentOptions" :key="item.id" :label="item.name" :value="item.id"/>
          </el-select>
        </el-form-item>
        <el-form-item label="名称"><el-input v-model="treeForm.name" maxlength="120"/></el-form-item>
        <el-form-item v-if="treeMode==='organizations'" label="组织编码"><el-input v-model="treeForm.code" maxlength="80"/></el-form-item>
        <template v-if="treeMode==='menus'">
          <el-form-item label="节点类型"><el-select v-model="treeForm.type" style="width:100%"><el-option label="目录" value="Directory"/><el-option label="菜单" value="Menu"/><el-option label="按钮" value="Button"/></el-select></el-form-item>
          <el-form-item label="路由"><el-input v-model="treeForm.path" placeholder="例如 /system/users"/></el-form-item>
          <el-form-item label="权限标识"><el-input v-model="treeForm.permission" placeholder="例如 system:user:edit"/></el-form-item>
          <el-form-item label="图标"><el-input v-model="treeForm.icon" placeholder="Lucide 图标名称"/></el-form-item>
        </template>
        <el-form-item label="排序"><el-input-number v-model="treeForm.sort" :min="0" :max="99999"/></el-form-item>
        <el-form-item label="状态"><el-switch v-model="treeForm.enabled" active-text="启用" inactive-text="停用"/></el-form-item>
      </el-form>
      <template #footer><el-button @click="treeDialog=false">取消</el-button><el-button type="primary" @click="saveTreeNode">保存</el-button></template>
    </el-dialog>
  </section>
</template>
