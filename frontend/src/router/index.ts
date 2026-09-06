import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import KnowledgeBases from '../views/KnowledgeBases.vue'
import KnowledgeWorkspace from '../views/KnowledgeWorkspace.vue'
import KnowledgeCenter from '../views/KnowledgeCenter.vue'
import ResourceAccess from '../views/ResourceAccess.vue'
import DataExchange from '../views/DataExchange.vue'
import SearchManagement from '../views/SearchManagement.vue'
import Collaboration from '../views/Collaboration.vue'
import Notifications from '../views/Notifications.vue'
import Login from '../views/Login.vue'
import SystemManagement from '../views/SystemManagement.vue'
import PublicShare from '../views/PublicShare.vue'

const router = createRouter({ history: createWebHistory(), routes: [
  { path: '/login', name: 'login', component: Login, meta: { public: true } },
  { path: '/share/:token', name: 'public-share', component: PublicShare, meta: { public: true } },
  { path: '/', component: Dashboard },
  { path: '/knowledge-bases', component: KnowledgeBases },
  { path: '/knowledge-bases/:id/access', component: ResourceAccess },
  { path: '/knowledge-bases/:id', component: KnowledgeWorkspace },
  { path: '/knowledge-center', component: KnowledgeCenter },
  { path: '/collaboration', component: Collaboration },
  { path: '/notifications', component: Notifications },
  { path: '/data-exchange', component: DataExchange },
  { path: '/search-management', component: SearchManagement },
  { path: '/system', component: SystemManagement }
] })
router.beforeEach(to => { const token=localStorage.getItem('kb_access_token'); if(!to.meta.public&&!token)return '/login'; if(to.path==='/login'&&token)return '/' })
export default router
