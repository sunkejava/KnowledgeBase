<script setup lang="ts">
import { Moon, RotateCcw, Settings2, Sun, Monitor } from 'lucide-vue-next'
import { useAppearanceStore } from '../stores/appearance'
import { useLocale } from '../composables/useLocale'

const model = defineModel<boolean>({ default: false })
const appearance = useAppearanceStore()
const { t } = useLocale()
</script>

<template>
  <el-drawer v-model="model" :title="t('appearance')" size="390px" class="appearance-drawer">
    <section class="setting-group">
      <label>{{ t('theme') }}</label>
      <div class="theme-options">
        <button :class="{active: appearance.settings.theme==='light'}" @click="appearance.settings.theme='light'"><Sun :size="17"/>{{ t('light') }}</button>
        <button :class="{active: appearance.settings.theme==='dark'}" @click="appearance.settings.theme='dark'"><Moon :size="17"/>{{ t('dark') }}</button>
        <button :class="{active: appearance.settings.theme==='system'}" @click="appearance.settings.theme='system'"><Monitor :size="17"/>{{ t('followSystem') }}</button>
      </div>
    </section>

    <section class="setting-group">
      <label>{{ t('language') }}</label>
      <el-select v-model="appearance.settings.locale" style="width:100%">
        <el-option label="简体中文" value="zh-CN" />
        <el-option label="English" value="en-US" />
      </el-select>
    </section>

    <section class="setting-group">
      <label>{{ t('fontSize') }} <span>{{ appearance.settings.fontSize }}px</span></label>
      <el-slider v-model="appearance.settings.fontSize" :min="12" :max="18" :step="1" show-stops />
    </section>

    <section class="setting-group switch-row">
      <div><label>{{ t('watermark') }}</label><small>适合内部资料、演示及截图追踪场景</small></div>
      <el-switch v-model="appearance.settings.watermarkEnabled" />
    </section>
    <section v-if="appearance.settings.watermarkEnabled" class="setting-group">
      <label>{{ t('watermarkText') }}</label>
      <el-input v-model="appearance.settings.watermarkText" maxlength="40" show-word-limit />
    </section>

    <section class="setting-group switch-row">
      <div><label>{{ t('compact') }}</label><small>缩小导航与内容间距，提高信息密度</small></div>
      <el-switch v-model="appearance.settings.compactMode" />
    </section>

    <div class="drawer-actions">
      <el-button @click="appearance.reset()"><RotateCcw :size="15"/> {{ t('reset') }}</el-button>
    </div>
  </el-drawer>
</template>
