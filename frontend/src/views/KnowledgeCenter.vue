<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { http } from '../api/http'
const router=useRouter(),tab=ref('search'),keyword=ref(''),rows=ref<any[]>([]),loading=ref(false)
async function load(){loading.value=true;try{if(tab.value==='recent')rows.value=(await http.get('/knowledge-assets/recent?take=50')).data;else if(tab.value==='favorites')rows.value=(await http.get('/knowledge-assets/favorites')).data;else if(tab.value==='tags')rows.value=(await http.get('/knowledge-assets/tags')).data;else if(keyword.value.trim())rows.value=(await http.get('/knowledge-assets/search',{params:{keyword:keyword.value,take:50}})).data;else rows.value=[]}finally{loading.value=false}}
function open(row:any){if(row.documentId)router.push(`/documents/${row.documentId}`)}
onMounted(load)
</script>
<template><section class="page"><div class="page-head"><div><h1>知识中心</h1><p>统一检索、收藏、最近访问与标签资产。</p></div></div>
<el-tabs v-model="tab" @tab-change="load"><el-tab-pane label="全文搜索" name="search"/><el-tab-pane label="最近浏览" name="recent"/><el-tab-pane label="我的收藏" name="favorites"/><el-tab-pane label="标签" name="tags"/></el-tabs>
<div v-if="tab==='search'" class="toolbar"><input v-model="keyword" placeholder="搜索标题或 Markdown 正文" @keyup.enter="load"/><button class="primary" @click="load">搜索</button></div>
<el-table :data="rows" v-loading="loading" stripe @row-dblclick="open"><el-table-column v-if="tab!=='tags'" prop="title" label="文档" min-width="220"/><el-table-column v-if="tab==='search'" prop="snippet" label="内容摘要" min-width="420"/><el-table-column v-if="tab==='recent'" prop="viewCount" label="访问次数" width="100"/><el-table-column v-if="tab==='recent'" prop="lastViewedAt" label="最近访问" width="190"/><el-table-column v-if="tab==='favorites'" prop="createdAt" label="收藏时间" width="190"/><el-table-column v-if="tab==='tags'" prop="name" label="标签"/><el-table-column v-if="tab==='tags'" prop="color" label="颜色"/></el-table></section></template>
