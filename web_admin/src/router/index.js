import { createRouter, createWebHistory } from 'vue-router'
import Layout from '../components/Layout.vue'

const routes = [
  {
    path: '/',
    component: Layout,
    redirect: '/dashboard',
    children: [
      { path: 'dashboard', name: 'Dashboard', component: () => import('../views/Dashboard.vue'), meta: { title: '仪表盘' } },
      { path: 'towers', name: 'TowerManage', component: () => import('../views/TowerManage.vue'), meta: { title: '防御塔管理' } },
      { path: 'monsters', name: 'MonsterManage', component: () => import('../views/MonsterManage.vue'), meta: { title: '怪物管理' } },
      { path: 'levels', name: 'LevelManage', component: () => import('../views/LevelManage.vue'), meta: { title: '关卡管理' } },
      { path: 'wave-events', name: 'WaveEventManage', component: () => import('../views/WaveEventManage.vue'), meta: { title: '出怪时间轴' } },
      { path: 'leaderboard', name: 'Leaderboard', component: () => import('../views/Leaderboard.vue'), meta: { title: '排行榜' } },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
