<template>
  <div>
    <el-card>
      <div style="display: flex; justify-content: space-between; align-items: center">
        <span></span>
        <el-button type="primary" @click="openDialog()">新增怪物</el-button>
      </div>

      <el-table :data="list" stripe style="margin-top: 16px">
        <el-table-column prop="monster_id" label="ID" width="60" />
        <el-table-column prop="name" label="名称" />
        <el-table-column prop="hp" label="血量" />
        <el-table-column prop="speed" label="速度" />
        <el-table-column prop="score_value" label="得分" />
        <el-table-column prop="reward_gold" label="赏金" />
        <el-table-column prop="damage_to_base" label="基地伤害" />
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
            <el-popconfirm title="确定删除？" @confirm="handleDelete(scope.row.monster_id)">
              <template #reference>
                <el-button size="small" type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="visible" :title="isEdit ? '编辑怪物' : '新增怪物'" width="500px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="名称">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="血量">
          <el-input-number v-model="form.hp" :min="1" />
        </el-form-item>
        <el-form-item label="速度">
          <el-input-number v-model="form.speed" :min="0.1" :precision="1" :step="0.5" />
        </el-form-item>
        <el-form-item label="得分">
          <el-input-number v-model="form.score_value" :min="0" />
        </el-form-item>
        <el-form-item label="赏金">
          <el-input-number v-model="form.reward_gold" :min="0" />
        </el-form-item>
        <el-form-item label="基地伤害">
          <el-input-number v-model="form.damage_to_base" :min="0" />
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

const list = ref([])
const visible = ref(false)
const isEdit = ref(false)
const editId = ref(null)

const defaultForm = {
  name: '', hp: 100, speed: 1.0, score_value: 100,
  reward_gold: 10, damage_to_base: 1, is_active: 1,
}
const form = ref({ ...defaultForm })

function fetchList() {
  api.get('/monsters').then((res) => { list.value = res.data })
}

function openDialog(row) {
  if (row) {
    isEdit.value = true
    editId.value = row.monster_id
    form.value = { ...row }
  } else {
    isEdit.value = false
    editId.value = null
    form.value = { ...defaultForm }
  }
  visible.value = true
}

function handleSave() {
  const payload = { ...form.value }
  const req = isEdit.value
    ? api.put(`/monsters/${editId.value}`, payload)
    : api.post('/monsters', payload)
  req.then(() => {
    visible.value = false
    fetchList()
  })
}

function handleDelete(id) {
  api.delete(`/monsters/${id}`).then(() => fetchList())
}

function toggleActive(row) {
  api.put(`/monsters/${row.monster_id}`, { ...row, is_active: row.is_active === 1 ? 0 : 1 }).then(() => fetchList())
}

onMounted(fetchList)
</script>
