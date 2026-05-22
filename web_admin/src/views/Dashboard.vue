<template>
  <div class="dashboard">
    <h2 class="welcome-title">欢迎回来，管理员</h2>
    <p class="welcome-sub">以下是协同防线的最新数据概览</p>

    <div class="stats-grid">
      <div
        v-for="(item, idx) in stats"
        :key="item.label"
        class="stat-card"
        :class="'card-' + idx"
        @click="$router.push(item.link)"
      >
        <div class="stat-bg-pattern"></div>
        <div class="stat-body">
          <div class="stat-icon-wrap">
            <el-icon class="stat-icon" :size="24">
              <component :is="item.icon" />
            </el-icon>
          </div>
          <div class="stat-info">
            <div class="stat-value">
              <span class="stat-number" ref="numRefs">{{ item.display }}</span>
            </div>
            <div class="stat-label">{{ item.label }}</div>
          </div>
        </div>
        <div class="stat-arrow">
          <el-icon><ArrowRight /></el-icon>
        </div>
      </div>
    </div>

    <el-card class="recent-card">
      <template #header>
        <div class="card-header-wrap">
          <div class="header-icon">
            <el-icon><Trophy /></el-icon>
          </div>
          <span class="card-header-title">最近游戏记录</span>
        </div>
      </template>
      <el-table :data="recentGames" stripe>
        <el-table-column prop="username" label="玩家" />
        <el-table-column prop="level_id" label="关卡" width="80" />
        <el-table-column prop="score" label="得分" sortable />
        <el-table-column prop="kill_count" label="击杀数" />
        <el-table-column prop="time_used" label="用时(秒)" />
        <el-table-column prop="is_win" label="结果" width="80" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.is_win ? 'success' : 'danger'" effect="dark" size="small">
              {{ scope.row.is_win ? '胜利' : '失败' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="played_at" label="时间" width="170" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'

const stats = ref([
  { label: '防御塔', value: 0, display: '0', link: '/towers', icon: 'Cpu' },
  { label: '怪物种类', value: 0, display: '0', link: '/monsters', icon: 'MagicStick' },
  { label: '关卡', value: 0, display: '0', link: '/levels', icon: 'Flag' },
  { label: '游戏记录', value: 0, display: '0', link: '/leaderboard', icon: 'DataAnalysis' },
])

const recentGames = ref([])

function animateCounts() {
  stats.value.forEach((s, i) => {
    const target = s.value
    const duration = 800
    const start = performance.now()
    const tick = (now) => {
      const elapsed = now - start
      const progress = Math.min(elapsed / duration, 1)
      const eased = 1 - Math.pow(1 - progress, 3)
      s.display = Math.floor(eased * target)
      if (progress < 1) {
        requestAnimationFrame(tick)
      } else {
        s.display = target
      }
    }
    requestAnimationFrame(tick)
  })
}

onMounted(async () => {
  try {
    const [t, m, l, lb] = await Promise.all([
      api.get('/towers'),
      api.get('/monsters'),
      api.get('/levels'),
      api.get('/leaderboard?limit=10'),
    ])
    stats.value[0].value = t.data.length
    stats.value[1].value = m.data.length
    stats.value[2].value = l.data.length
    const ranking = lb.data.data?.ranking || []
    stats.value[3].value = ranking.length
    recentGames.value = ranking.slice(0, 10)
    animateCounts()
  } catch (_) { /* ignore */ }
})
</script>

<style scoped>
.dashboard {
  animation: fadeUp 0.4s ease;
}

@keyframes fadeUp {
  from { opacity: 0; transform: translateY(12px); }
  to { opacity: 1; transform: translateY(0); }
}

.welcome-title {
  margin: 0 0 4px;
  font-size: 22px;
  font-weight: 700;
  color: #1e293b;
}

.welcome-sub {
  margin: 0 0 24px;
  font-size: 14px;
  color: #94a3b8;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  margin-bottom: 24px;
}

.stat-card {
  position: relative;
  border-radius: 16px;
  padding: 24px 20px;
  cursor: pointer;
  overflow: hidden;
  transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.15);
}

.stat-bg-pattern {
  position: absolute;
  top: -20px;
  right: -20px;
  width: 120px;
  height: 120px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.08);
}

.card-0 { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
.card-1 { background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); }
.card-2 { background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); }
.card-3 { background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); }

.stat-body {
  display: flex;
  align-items: center;
  gap: 16px;
  position: relative;
  z-index: 1;
}

.stat-icon-wrap {
  width: 52px;
  height: 52px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(4px);
}

.stat-icon { color: #fff; }

.stat-info {
  flex: 1;
  min-width: 0;
}

.stat-value { margin-bottom: 2px; }

.stat-number {
  font-size: 30px;
  font-weight: 800;
  color: #fff;
  line-height: 1.2;
}

.stat-label {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.8);
  font-weight: 500;
}

.stat-arrow {
  position: absolute;
  right: 16px;
  bottom: 16px;
  color: rgba(255, 255, 255, 0.4);
  font-size: 18px;
  transition: all 0.3s;
}

.stat-card:hover .stat-arrow {
  right: 10px;
  color: rgba(255, 255, 255, 0.8);
}

.recent-card {
  margin-top: 0;
}

.card-header-wrap {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: linear-gradient(135deg, #667eea, #764ba2);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 16px;
}

.card-header-title {
  font-size: 16px;
  font-weight: 600;
  color: #1e293b;
}

@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
}
</style>
