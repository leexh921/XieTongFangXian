<template>
  <el-container class="app-container">
    <el-aside class="sidebar">
      <div class="logo">
        <el-icon class="logo-icon"><Aim /></el-icon>
        <span class="logo-text">协同防线</span>
      </div>
      <div class="logo-divider"></div>

      <el-menu
        :default-active="route.path"
        background-color="transparent"
        text-color="#a5b4fc"
        active-text-color="#fff"
        router
        class="sidebar-menu"
      >
        <el-menu-item index="/dashboard">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><DataAnalysis /></el-icon>
              <span>仪表盘</span>
            </div>
          </template>
        </el-menu-item>
        <el-menu-item index="/towers">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><Cpu /></el-icon>
              <span>防御塔管理</span>
            </div>
          </template>
        </el-menu-item>
        <el-menu-item index="/monsters">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><MagicStick /></el-icon>
              <span>怪物管理</span>
            </div>
          </template>
        </el-menu-item>
        <el-menu-item index="/levels">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><Flag /></el-icon>
              <span>关卡管理</span>
            </div>
          </template>
        </el-menu-item>
        <el-menu-item index="/wave-events">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><Timer /></el-icon>
              <span>出怪时间轴</span>
            </div>
          </template>
        </el-menu-item>
        <el-menu-item index="/players">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><User /></el-icon>
              <span>玩家管理</span>
            </div>
          </template>
        </el-menu-item>
        <el-menu-item index="/leaderboard">
          <template #title>
            <div class="menu-item-inner">
              <el-icon class="menu-icon"><Trophy /></el-icon>
              <span>排行榜</span>
            </div>
          </template>
        </el-menu-item>
      </el-menu>

      <div class="sidebar-footer">
        <div class="version-tag">v1.0.0</div>
      </div>
    </el-aside>

    <el-container class="main-area">
      <el-header class="top-header">
        <div class="header-left">
          <h3 class="page-title">{{ route.meta.title }}</h3>
        </div>
        <div class="header-right"></div>
      </el-header>
      <el-main class="main-content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { useRoute } from 'vue-router'
const route = useRoute()
</script>

<style scoped>
.app-container { height: 100vh; }

.sidebar {
  width: 230px !important;
  background: linear-gradient(180deg, #0f0c29 0%, #1a1a3e 30%, #1e2a5e 100%);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

.sidebar::before {
  content: '';
  position: absolute;
  top: 0; left: 0; right: 0; bottom: 0;
  background: radial-gradient(ellipse at 50% 0%, rgba(99, 102, 241, 0.08) 0%, transparent 70%);
  pointer-events: none;
}

.logo {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 0 16px;
}

.logo-icon {
  font-size: 28px;
  color: #818cf8;
  filter: drop-shadow(0 0 8px rgba(129, 140, 248, 0.5));
}

.logo-text {
  font-size: 20px;
  font-weight: 700;
  color: #e0e7ff;
  text-shadow: 0 0 18px rgba(99, 102, 241, 0.4);
  letter-spacing: 2px;
}

.logo-divider {
  height: 1px;
  margin: 0 20px;
  background: linear-gradient(90deg, transparent, rgba(129, 140, 248, 0.3), transparent);
}

.sidebar-menu {
  border-right: none !important;
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.sidebar-menu .el-menu-item {
  margin: 4px 12px;
  border-radius: 10px;
  height: 44px;
  line-height: 44px;
  transition: all 0.3s ease;
  border-right: 3px solid transparent;
}

.sidebar-menu .el-menu-item:hover {
  background: linear-gradient(90deg, rgba(99, 102, 241, 0.2), rgba(99, 102, 241, 0.05)) !important;
  color: #c7d2fe !important;
}

.sidebar-menu .el-menu-item.is-active {
  background: linear-gradient(90deg, rgba(99, 102, 241, 0.35), rgba(99, 102, 241, 0.08)) !important;
  color: #fff !important;
  border-right: 3px solid #818cf8;
  box-shadow: inset 0 0 20px rgba(99, 102, 241, 0.1);
}

.menu-item-inner {
  display: flex;
  align-items: center;
  gap: 10px;
}

.menu-icon {
  font-size: 18px;
  width: 18px;
}

.sidebar-footer {
  padding: 16px;
  text-align: center;
  border-top: 1px solid rgba(99, 102, 241, 0.15);
}

.version-tag {
  font-size: 11px;
  color: rgba(165, 180, 252, 0.5);
  letter-spacing: 1px;
}

.main-area {
  background: #f0f2f8;
}

.top-header {
  height: 64px !important;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 28px !important;
  background: #fff;
  box-shadow: 0 1px 8px rgba(0, 0, 0, 0.04);
}

.page-title {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #1e293b;
  position: relative;
  padding-left: 14px;
}

.page-title::before {
  content: '';
  position: absolute;
  left: 0;
  top: 2px;
  bottom: 2px;
  width: 4px;
  background: linear-gradient(180deg, #6366f1, #a5b4fc);
  border-radius: 2px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.main-content {
  padding: 24px 28px !important;
  min-height: 0;
}
</style>
