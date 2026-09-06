<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { http } from '../lib/http'
const tab=ref('users'), loading=ref(false), rows=ref<any[]>([]), roles=ref<any[]>([]), menus=ref<any[]>([])
const userDialog=ref(false), user=ref<any>({userName:'',displayName:'',password:'',enabled:true,roleIds:[]})
const roleDialog=ref(false), role=ref<any>({code:'',name:'',enabled:true,menuIds:[]})
async function load(){loading.value=true;try{const path=tab.value==='audit'?'audit-logs':tab.value;rows.value=(await http.get(`/system/${path}`)).data;if(tab.value==='users')roles.value=(await http.get('/system/roles')).data;if(tab.value==='roles'){menus.value=(await http.get('/system/menus')).data}}finally{loading.value=false}}
async function saveUser(){const id=user.value.id;await http[id?'put':'post'](`/system/users${id?'/'+id:''}`,user.value);userDialog.value=false;await load()}
async function saveRole(){const id=role.value.id;await http[id?'put':'post'](`/system/roles${id?'/'+id:''}`,role.value);roleDialog.value=false;await load()}
async function remove(type:string,id:string){await http.delete(`/system/${type}/${id}`);await load()}
function addUser(){user.value={userName:'',displayName:'',password:'',enabled:true,roleIds:[]};userDialog.value=true}
function addRole(){role.value={code:'',name:'',enabled:true,menuIds:[]};roleDialog.value=true}
onMounted(load)
</script>
<template><section class="page"><div class="page-head"><div><h1>系统管理</h1><p>账号、角色、组织与权限统一维护。</p></div></div>
<el-tabs v-model="tab" @tab-change="load"><el-tab-pane label="用户" name="users"/><el-tab-pane label="角色" name="roles"/><el-tab-pane label="部门" name="departments"/><el-tab-pane label="组织机构" name="organizations"/><el-tab-pane label="菜单权限" name="menus"/><el-tab-pane label="审计日志" name="audit"/></el-tabs>
<div class="toolbar" v-if="tab==='users'"><button class="primary" @click="addUser">新增用户</button></div><div class="toolbar" v-if="tab==='roles'"><button class="primary" @click="addRole">新增角色</button></div>
<el-table :data="rows" v-loading="loading" stripe>
<el-table-column v-if="tab==='users'" prop="userName" label="用户名"/><el-table-column v-if="tab==='users'" prop="displayName" label="姓名"/><el-table-column v-if="tab==='users'" label="角色"><template #default="s">{{s.row.roles?.join('、')}}</template></el-table-column>
<el-table-column v-if="tab==='roles'" prop="code" label="编码"/><el-table-column v-if="tab==='roles'" prop="name" label="角色名称"/>
<el-table-column v-if="['departments','organizations'].includes(tab)" prop="name" label="名称"/><el-table-column v-if="tab==='organizations'" prop="code" label="编码"/>
<el-table-column v-if="tab==='menus'" prop="name" label="菜单"/><el-table-column v-if="tab==='menus'" prop="path" label="路由"/><el-table-column v-if="tab==='menus'" prop="permission" label="权限标识"/>
<el-table-column v-if="tab==='audit'" prop="createdAt" label="时间"/><el-table-column v-if="tab==='audit'" prop="userName" label="用户"/><el-table-column v-if="tab==='audit'" prop="action" label="操作"/><el-table-column v-if="tab==='audit'" prop="target" label="对象"/>
<el-table-column v-if="tab==='users'||tab==='roles'" label="操作" width="160"><template #default="s"><el-button link type="primary" @click="tab==='users'?(user={...s.row,roleIds:[]},userDialog=true):(role={...s.row},roleDialog=true)">编辑</el-button><el-button link type="danger" @click="remove(tab,s.row.id)">删除</el-button></template></el-table-column>
</el-table>
<el-dialog v-model="userDialog" title="用户维护" width="520"><el-form label-width="90"><el-form-item label="用户名"><el-input v-model="user.userName" :disabled="!!user.id"/></el-form-item><el-form-item label="姓名"><el-input v-model="user.displayName"/></el-form-item><el-form-item label="密码"><el-input v-model="user.password" type="password" show-password/></el-form-item><el-form-item label="角色"><el-select v-model="user.roleIds" multiple style="width:100%"><el-option v-for="r in roles" :key="r.id" :label="r.name" :value="r.id"/></el-select></el-form-item></el-form><template #footer><el-button @click="userDialog=false">取消</el-button><el-button type="primary" @click="saveUser">保存</el-button></template></el-dialog>
<el-dialog v-model="roleDialog" title="角色维护" width="560"><el-form label-width="90"><el-form-item label="编码"><el-input v-model="role.code"/></el-form-item><el-form-item label="名称"><el-input v-model="role.name"/></el-form-item><el-form-item label="菜单权限"><el-select v-model="role.menuIds" multiple filterable style="width:100%"><el-option v-for="m in menus" :key="m.id" :label="m.name+' · '+m.permission" :value="m.id"/></el-select></el-form-item></el-form><template #footer><el-button @click="roleDialog=false">取消</el-button><el-button type="primary" @click="saveRole">保存</el-button></template></el-dialog>
</section></template>
