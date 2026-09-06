import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import KnowledgeBases from '../views/KnowledgeBases.vue'

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: Dashboard },
    { path: '/knowledge-bases', component: KnowledgeBases }
  ]
})
