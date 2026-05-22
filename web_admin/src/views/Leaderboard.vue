<template>
  <div>
    <el-card>
      <el-table :data="list" stripe>
        <el-table-column prop="result_id" label="ID" width="60" />
        <el-table-column prop="game_id" label="对局ID" width="120" />
        <el-table-column prop="username" label="玩家" />
        <el-table-column prop="level_id" label="关卡" width="80" />
        <el-table-column prop="score" label="得分" sortable />
        <el-table-column prop="kill_count" label="击杀数" />
        <el-table-column prop="time_used" label="用时(秒)" sortable />
        <el-table-column prop="is_win" label="结果" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.is_win ? 'success' : 'danger'">{{ scope.row.is_win ? '胜利' : '失败' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="played_at" label="时间" width="180" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'

const list = ref([])

onMounted(async () => {
  try {
    const res = await api.get('/leaderboard?limit=200')
    list.value = res.data.data?.ranking || []
  } catch (_) { /* ignore */ }
})
</script>
