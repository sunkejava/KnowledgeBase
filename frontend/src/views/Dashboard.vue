<script setup lang="ts">
const recent = [
  ['API 开发规范', '研发规范', '10 分钟前'],
  ['USB 设备故障排查手册', '硬件运维', '昨天'],
  ['.NET 10 服务开发规范', '研发规范', '2 天前']
]
</script>

<template>
  <section class="page">
    <div class="page-head"><div><h1>工作台</h1><p>集中查看知识资产、最近访问与内容变化。</p></div><button class="primary">新建文档</button></div>
    <div class="metrics">
      <div class="metric"><span>文档总数</span><strong>12,846</strong><small>本月 +328</small></div>
      <div class="metric"><span>知识空间</span><strong>86</strong><small>运行正常</small></div>
      <div class="metric"><span>团队成员</span><strong>136</strong><small>今日活跃 42</small></div>
      <div class="metric"><span>本周访问</span><strong>8,492</strong><small>较上周 +12.4%</small></div>
    </div>
    <div class="grid">
      <div class="panel wide"><div class="panel-title"><strong>最近访问</strong><span>查看全部</span></div><div class="doc-row" v-for="item in recent" :key="item[0]"><div><b>{{ item[0] }}</b><span>{{ item[1] }}</span></div><time>{{ item[2] }}</time></div></div>
      <div class="panel"><div class="panel-title"><strong>热门知识</strong><span>近 7 天</span></div><ol><li>API 身份认证规范 <em>1,284</em></li><li>生产环境发布清单 <em>986</em></li><li>USB 异常处理手册 <em>827</em></li><li>数据库设计规范 <em>641</em></li></ol></div>
      <div class="panel wide"><div class="panel-title"><strong>知识增长趋势</strong><span>最近 30 天</span></div><div class="trend"><i v-for="h in [28,40,34,52,46,64,58,72,66,82,76,91]" :key="h" :style="{height:h+'%'}"></i></div></div>
      <div class="panel"><div class="panel-title"><strong>待处理</strong><span>4 项</span></div><div class="todo"><b>3</b><span>待审核文档</span></div><div class="todo"><b>1</b><span>失败导入任务</span></div></div>
    </div>
  </section>
</template>
