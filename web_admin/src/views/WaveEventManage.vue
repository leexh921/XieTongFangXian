<template>
  <div>
    <el-card>
      <div style="margin-bottom: 16px; display: flex; gap: 16px; align-items: center">
        <span>选择关卡：</span>
        <el-select v-model="selectedLevelId" placeholder="请选择关卡" @change="fetchList">
          <el-option v-for="l in levels" :key="l.level_id" :label="l.level_name" :value="l.level_id" />
        </el-select>
        <el-button type="primary" :disabled="!selectedLevelId" @click="openDialog()">新增出怪事件</el-button>
      </div>

      <el-table :data="list" stripe>
        <el-table-column prop="event_id" label="ID" width="60" />
        <el-table-column prop="wave_number" label="波次" width="80" />
        <el-table-column prop="spawn_time" label="生成时间(秒)" />
        <el-table-column prop="name" label="怪物" />
        <el-table-column prop="count" label="数量" />
        <el-table-column prop="interval_time" label="间隔(秒)" />
        <el-table-column prop="is_active" label="启用" width="80">
          <template #default="scope">
            <el-switch
              :model-value="scope.row.is_active === 1"
              @change="toggleActive(scope.row)"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160">
          <template #default="scope">
            <el-button size="small" @click="openDialog(scope.row)">编辑</el-button>
            <el-popconfirm title="确定删除？" @confirm="handleDelete(scope.row.event_id)">
              <template #reference>
                <el-button size="small" type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="visible" :title="isEdit ? '编辑出怪事件' : '新增出怪事件'" width="500px">
      <el-form :model="form" label-width="110px">
        <el-form-item label="关卡">
          <el-select v-model="form.level_id" placeholder="请选择关卡" :disabled="isEdit">
            <el-option v-for="l in levels" :key="l.level_id" :label="l.level_name" :value="l.level_id" />
          </el-select>
        </el-form-item>
        <el-form-item label="波次">
          <el-input-number v-model="form.wave_number" :min="1" />
        </el-form-item>
        <el-form-item label="生成时间(秒)">
          <el-input-number v-model="form.spawn_time" :min="0" :precision="1" :step="1" />
        </el-form-item>
        <el-form-item label="怪物">
          <el-select v-model="form.monster_id" placeholder="请选择怪物">
            <el-option v-for="m in monsters" :key="m.monster_id" :label="m.name" :value="m.monster_id" />
          </el-select>
        </el-form-item>
        <el-form-item label="数量">
          <el-input-number v-model="form.count" :min="1" />
        </el-form-item>
        <el-form-item label="间隔(秒)">
          <el-input-number v-model="form.interval_time" :min="0" :precision="1" :step="0.5" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.is_active" :active-value="1" :inactive-value="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="visible = false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'

const levels = ref([])
const monsters = ref([])
const selectedLevelId = ref(null)
const list = ref([])
const visible = ref(false)
const isEdit = ref(false)
const editId = ref(null)

const defaultForm = {
  level_id: null, wave_number: 1, spawn_time: 0,
  monster_id: null, count: 1, interval_time: 0.5, is_active: 1,
}
const form = ref({ ...defaultForm })

function fetchList() {
  if (!selectedLevelId.value) { list.value = []; return }
  api.get(`/levels/${selectedLevelId.value}/waves`).then((res) => { list.value = res.data })
}

function openDialog(row) {
  if (row) {
    isEdit.value = true
    editId.value = row.event_id
    form.value = { ...row }
  } else {
    isEdit.value = false
    editId.value = null
    form.value = { ...defaultForm, level_id: selectedLevelId.value }
  }
  visible.value = true
}

function handleSave() {
  const payload = { ...form.value }
  const req = isEdit.value
    ? api.put(`/wave-events/${editId.value}`, payload)
    : api.post('/wave-events', payload)
  req.then(() => {
    visible.value = false
    fetchList()
  })
}

function handleDelete(id) {
  api.delete(`/wave-events/${id}`).then(() => fetchList())
}

function toggleActive(row) {
  api.put(`/wave-events/${row.event_id}`, { ...row, is_active: row.is_active === 1 ? 0 : 1 }).then(() => fetchList())
}

onMounted(async () => {
  const [lRes, mRes] = await Promise.all([
    api.get('/levels'),
    api.get('/monsters'),
  ])
  levels.value = lRes.data
  monsters.value = mRes.data
  if (levels.value.length) {
    selectedLevelId.value = levels.value[0].level_id
    fetchList()
  }
})
</script>
