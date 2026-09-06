<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
const route=useRoute(),doc=ref<any>(null),loading=ref(true),notFound=ref(false)
onMounted(async()=>{try{const base=import.meta.env.VITE_API_BASE_URL||'/api';doc.value=(await axios.get(`${base}/share/public/${route.params.token}`)).data}catch{notFound.value=true}finally{loading.value=false}})
</script>
<template><main class="share-page"><div class="share-brand">KnowledgeBase <span>共享文档</span></div><article v-if="doc" class="share-card"><h1>{{doc.title}}</h1><div class="share-meta">只读分享<span v-if="doc.expiresAt"> · 有效期至 {{doc.expiresAt}}</span></div><pre>{{doc.markdown}}</pre></article><div v-else-if="loading" class="share-state">正在加载文档…</div><div v-else class="share-state"><strong>分享链接不可用</strong><p>链接可能已停用、过期或文档不存在。</p></div></main></template>
<style scoped>.share-page{min-height:100vh;background:#f6f7f9;color:#172033;padding:28px 20px 70px}.share-brand{max-width:900px;margin:0 auto 20px;font-weight:700;font-size:14px}.share-brand span{font-weight:400;color:#7b8492;margin-left:8px}.share-card{max-width:900px;margin:auto;background:white;border:1px solid #e3e7ed;border-radius:10px;padding:42px 54px}.share-card h1{font-size:30px;margin:0 0 10px}.share-meta{font-size:12px;color:#8792a4;border-bottom:1px solid #edf0f4;padding-bottom:20px}.share-card pre{white-space:pre-wrap;word-break:break-word;font:14px/1.85 Inter,"Microsoft YaHei",sans-serif;margin-top:28px}.share-state{max-width:900px;margin:120px auto;text-align:center;color:#697386}.share-state strong{font-size:20px;color:#283246}</style>
