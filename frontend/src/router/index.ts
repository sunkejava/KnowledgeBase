import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import KnowledgeBases from '../views/KnowledgeBases.vue'
import KnowledgeWorkspace from '../views/KnowledgeWorkspace.vue'
import Login from '../views/Login.vue'
import SystemManagement from '../views/SystemManagement.vue'

const router = createRouter({ history: createWebHistory(), routes: [
  { path: '/login', name: 'login', component: Login, meta: { public: true } },
  { path: '/', component: Dashboard },
  { path: '/knowledge-bases', component: KnowledgeBases },
  { path: '/knowledge-bases/:id', component: KnowledgeWorkspace },
  { path: '/system', component: SystemManagement }
] })
router.beforeEach(to => { const token = localStorage.getItem('kb_access_token'); if (!to.meta.public && !token) return '/login'; if (to.path === '/login' && token) return '/' })
export default router
