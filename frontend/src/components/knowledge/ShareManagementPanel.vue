<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import BaseDataTable from '../common/BaseDataTable.vue'
import type { TableColumn } from '../../types/table'
import { documentApi } from '../../api/modules/documents'

const props = defineProps<{ modelValue:boolean; documentId:string }>()
const emit = defineEmits<{ 'update:modelValue':[value:boolean] }>()
const visible = computed({ get:()=>props.modelValue, set:value=>emit('update:modelValue',value) })
const tab = ref('links')
const links = ref<any[]>([])
const logs = ref<any[]>([])
const loading = ref(false)
const createDialog = ref(false)
const form = ref({ expiresAt: '', password: '' })

const linkColumns: TableColumn<any>[] = [
  { key:'token',label:'Token',minWidth:220 },
  { key:'passwordProtected',label:'密码保护',width:110,formatter:r=>r.passwordProtected?'是':'否' },
  { key:'accessCount',label:'访问次数',width:110 },
  { key:'lastAccessAt',label:'最近访问',width:190,formatter:r=>formatTime(r.lastAccessAt) },
  { key:'expiresAt',label:'过期时间',width:190,formatter:r=>formatTime(r.expiresAt) },
  { key:'enabled',label:'状态',width:100,formatter:r=>r.enabled?'启用':'停用' },
  { key:'createdAt',label:'创建时间',width:190,formatter:r=>formatTime(r.createdAt) }
]
const logColumns: TableColumn<any>[] = [
  { key:'accessedAt',label:'访问时间',width:190,formatter:r=>formatTime(r.accessedAt) },
  { key:'ipAddress',label:'IP',width:150 },
  { key:'success',label:'结果',width:90,formatter:r=>r.success?'成功':'失败' },
  { key:'message',label:'说明',minWidth:180 },
  { key:'userAgent',label:'User-Agent',minWidth:320 }
]
function formatTime(value?:string){return value?new Date(value).toLocaleString():'-'}
async function load(){if(!visible.value||!props.documentId)return;loading.value=true;try{const [a,b]=await Promise.all([documentApi.shares(props.documentId),documentApi.shareAccessLogs(props.documentId)]);links.value=a.data;logs.value=b.data}finally{loading.value=false}}
async function createShare(){const expires=form.value.expiresAt?new Date(form.value.expiresAt).toISOString():null;const {data}=await documentApi.createShare(props.documentId,expires,form.value.password||null);createDialog.value=false;await navigator.clipboard.writeText(`${location.origin}/share/${data.token}`);ElMessage.success('分享链接已创建并复制');await load()}
async function copy(row:any){await navigator.clipboard.writeText(`${location.origin}/share/${row.token}`);ElMessage.success('链接已复制')}
async function disable(row:any){await ElMessageBox.confirm('确认停用该分享链接？','停用确认',{type:'warning'});await documentApi.disableShare(row.id);await load()}
watch(()=>props.modelValue,v=>{if(v)void load()});watch(()=>props.documentId,()=>{if(visible.value)void load()})
</script>

<template>
  <el-drawer v-model="visible" title="分享管理" size="860px">
    <el-tabs v-model="tab">
      <el-tab-pane label="分享链接" name="links">
        <BaseDataTable :rows="links" :columns="linkColumns" :loading="loading" storage-key="share-links" export-file-name="KnowledgeBase-分享链接" :action-width="150" @refresh="load">
          <template #toolbar><el-button type="primary" @click="form={expiresAt:'',password:''};createDialog=true">创建分享</el-button></template>
          <template #actions="{row}"><el-button link type="primary" @click="copy(row)">复制</el-button><el-button link type="danger" :disabled="!row.enabled" @click="disable(row)">停用</el-button></template>
        </BaseDataTable>
      </el-tab-pane>
      <el-tab-pane label="访问日志" name="logs">
        <BaseDataTable :rows="logs" :columns="logColumns" :loading="loading" storage-key="share-access-logs" export-file-name="KnowledgeBase-分享访问日志" :show-actions="false" @refresh="load"/>
      </el-tab-pane>
    </el-tabs>
    <el-dialog v-model="createDialog" title="创建文档分享" width="520px" append-to-body>
      <el-form label-position="top">
        <el-form-item label="访问密码（可选）"><el-input v-model="form.password" type="password" show-password maxlength="64" placeholder="留空表示无需密码"/></el-form-item>
        <el-form-item label="过期时间（可选）"><el-date-picker v-model="form.expiresAt" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width:100%"/></el-form-item>
      </el-form>
      <template #footer><el-button @click="createDialog=false">取消</el-button><el-button type="primary" @click="createShare">创建并复制</el-button></template>
    </el-dialog>
  </el-drawer>
</template>
