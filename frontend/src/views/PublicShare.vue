<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { shareApi } from '../api/modules/share'

const route = useRoute()
const doc = ref<any>(null)
const loading = ref(true)
const needPassword = ref(false)
const password = ref('')
const error = ref('')

async function load(passwordValue: string | null = null) {
  loading.value = true; error.value = ''
  try { doc.value = (await shareApi.publicDocument(String(route.params.token), passwordValue)).data; needPassword.value = false }
  catch { doc.value = null; needPassword.value = true; error.value = passwordValue ? '密码错误、链接已过期或已停用' : '该分享可能需要访问密码' }
  finally { loading.value = false }
}

onMounted(() => load())
</script>

<template>
  <main class="share-page">
    <div class="share-brand">KnowledgeBase <span>共享文档</span></div>
    <article v-if="doc" class="share-card"><h1>{{ doc.title }}</h1><div class="share-meta">只读分享<span v-if="doc.expiresAt"> · 有效期至 {{ new Date(doc.expiresAt).toLocaleString() }}</span></div><pre>{{ doc.markdown }}</pre></article>
    <div v-else-if="loading" class="share-state">正在验证分享链接…</div>
    <div v-else-if="needPassword" class="share-auth"><strong>受保护的共享文档</strong><p>{{ error }}</p><el-input v-model="password" type="password" show-password placeholder="输入访问密码" @keyup.enter="load(password)"/><el-button type="primary" @click="load(password)">访问文档</el-button></div>
    <div v-else class="share-state"><strong>分享链接不可用</strong><p>链接可能已停用、过期或文档不存在。</p></div>
  </main>
</template>

<style scoped>
.share-page{min-height:100vh;background:#f6f7f9;color:#172033;padding:28px 20px 70px}.share-brand{max-width:900px;margin:0 auto 20px;font-weight:700;font-size:14px}.share-brand span{font-weight:400;color:#7b8492;margin-left:8px}.share-card,.share-auth{max-width:900px;margin:auto;background:white;border:1px solid #e3e7ed;border-radius:10px;padding:42px 54px}.share-card h1{font-size:30px;margin:0 0 10px}.share-meta{font-size:12px;color:#8792a4;border-bottom:1px solid #edf0f4;padding-bottom:20px}.share-card pre{white-space:pre-wrap;word-break:break-word;font:14px/1.85 Inter,"Microsoft YaHei",sans-serif;margin-top:28px}.share-state{max-width:900px;margin:120px auto;text-align:center;color:#697386}.share-state strong,.share-auth strong{font-size:20px;color:#283246}.share-auth{max-width:460px;margin-top:100px}.share-auth p{color:#7b8492}.share-auth .el-button{width:100%;margin-top:14px}
</style>
