<script setup lang="ts">
const bases = [
  { name: '研发技术中心', desc: '架构、编码规范、接口与工程实践', docs: 486, updated: '今天 09:18' },
  { name: '硬件与设备运维', desc: 'USB、串口、驱动及现场故障处理手册', docs: 213, updated: '昨天 18:42' },
  { name: '项目交付资料', desc: '部署、运维、交付与客户环境记录', docs: 169, updated: '09-03 14:21' }
]
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>我的知识库</h1><p>按业务边界管理团队知识与权限。</p></div><button class="primary">创建知识库</button></div>
    <div class="toolbar"><input placeholder="搜索知识库"/><select><option>全部状态</option><option>正常</option></select><button>查询</button></div>
    <div class="kb-grid"><article class="kb-card" v-for="kb in bases" :key="kb.name"><div class="kb-icon">KB</div><div><h3>{{ kb.name }}</h3><p>{{ kb.desc }}</p><footer><span>{{ kb.docs }} 篇文档</span><span>更新于 {{ kb.updated }}</span></footer></div></article></div>
  </section>
</template>
