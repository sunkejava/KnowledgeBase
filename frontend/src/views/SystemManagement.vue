<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { http } from '../api/http'

const active = ref('users')
const loading = ref(false)
const data = ref<any[]>([])
const endpoints: Record<string, string> = { users: 'users', roles: 'roles', departments: 'departments', organizations: 'organizations', menus: 'menus' }

async function load(tab = active.value) {
  active.value = tab
  loading.value = true
  try { data.value = (await http.get(`/system/${endpoints[tab]}`)).data }
  finally { loading.value = false }
}
onMounted(() => load())
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>系统管理</h1><p>统一维护用户、角色、组织机构和菜单权限基础数据。</p></div></div>
    <el-tabs :model-value="active" @tab-change="(name:any) => load(String(name))">
      <el-tab-pane label="用户" name="users"/><el-tab-pane label="角色" name="roles"/><el-tab-pane label="部门" name="departments"/><el-tab-pane label="组织机构" name="organizations"/><el-tab-pane label="菜单" name="menus"/>
    </el-tabs>
    <div class="panel" v-loading="loading">
      <el-table :data="data" style="width:100%">
        <el-table-column v-if="active==='users'" prop="userName" label="账号"/><el-table-column v-if="active==='users'" prop="displayName" label="姓名"/><el-table-column v-if="active==='users'" prop="enabled" label="状态"><template #default="scope">{{ scope.row.enabled ? '正常' : '停用' }}</template></el-table-column>
        <el-table-column v-if="active==='roles'" prop="code" label="角色编码"/><el-table-column v-if="active==='roles'" prop="name" label="角色名称"/>
        <el-table-column v-if="active==='departments' || active==='organizations' || active==='menus'" prop="name" label="名称"/>
        <el-table-column v-if="active==='organizations'" prop="code" label="编码"/>
        <el-table-column v-if="active==='menus'" prop="path" label="路由"/><el-table-column v-if="active==='menus'" prop="permission" label="权限标识"/>
      </el-table>
      <div v-if="!loading && data.length===0" class="empty-state">当前暂无数据，后续可在该模块继续增加新增、编辑、授权和树形维护能力。</div>
    </div>
  </section>
</template>
