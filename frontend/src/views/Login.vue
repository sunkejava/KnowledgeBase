<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { LockKeyhole, ShieldCheck } from 'lucide-vue-next'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()
const userName = ref('admin')
const password = ref('Admin123!')
const loading = ref(false)

async function submit() {
  if (!userName.value || !password.value) return
  loading.value = true
  try {
    await auth.login(userName.value, password.value)
    await router.replace('/')
  } catch (error: any) {
    ElMessage.error(error?.response?.data?.message || '登录失败，请检查账号、密码及后端服务')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-visual">
      <div class="login-brand"><span class="brand-mark">K</span><div><strong>KnowledgeBase</strong><small>Knowledge Asset Platform</small></div></div>
      <div class="login-copy">
        <span class="eyebrow">KNOWLEDGE INFRASTRUCTURE</span>
        <h1>让知识成为<br/>可持续维护的资产。</h1>
        <p>统一沉淀技术规范、业务资料、项目文档与团队经验，建立可检索、可追踪、可授权的知识基础设施。</p>
      </div>
      <div class="login-status"><ShieldCheck :size="16"/> JWT 身份认证 · RBAC 权限模型 · 审计能力预留</div>
    </div>
    <div class="login-panel">
      <div class="login-card">
        <div class="login-icon"><LockKeyhole :size="20"/></div>
        <h2>登录工作空间</h2>
        <p>使用系统账号进入知识资产平台</p>
        <label>账号</label>
        <el-input v-model="userName" size="large" autocomplete="username" @keyup.enter="submit" />
        <label>密码</label>
        <el-input v-model="password" type="password" show-password size="large" autocomplete="current-password" @keyup.enter="submit" />
        <el-button class="login-button" type="primary" size="large" :loading="loading" @click="submit">进入系统</el-button>
        <div class="login-hint">首次启动默认账号：admin / Admin123!，部署后请尽快修改。</div>
      </div>
    </div>
  </div>
</template>
