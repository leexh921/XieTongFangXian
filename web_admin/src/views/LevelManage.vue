<template>
  <div>
    <el-card>
      <div style="display: flex; justify-content: space-between; align-items: center">
        <span></span>
        <el-button type="primary" @click="openDialog()">新增关卡</el-button>
      </div>

      <el-table :data="list" stripe style="margin-top: 16px">
        <el-table-column prop="level_id" label="ID" width="60" />
        <el-table-column prop="level_name" label="关卡名称" />
        <el-table-column prop="initial_gold" label="初始金币" />
        <el-table-column prop="base_hp" label="基地血量" />
        <el-table-column prop="gold_per_second" label="每秒金币" />
        <el-table-column prop="description" label="描述" />
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
            <el-popconfirm title="确定删除？" @confirm="handleDelete(scope.row.level_id)">
              <template #reference>
                <el-button size="small" type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="visible" :title="isEdit ? '编辑关卡' : '新增关卡'" width="500px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="关卡名称">
          <el-input v-model="form.level_name" />
        </el-form-item>
        <el-form-item label="初始金币">
          <el-input-number v-model="form.initial_gold" :min="0" />
        </el-form-item>
        <el-form-item label="基地血量">
          <el-input-number v-model="form.base_hp" :min="1" />
        </el-form-item>
        <el-form-item label="每秒金币">
          <el-input-number v-model="form.gold_per_second" :min="0" :precision="1" :step="0.5" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" />
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
  level_name: '', initial_gold: 100, base_hp: 10,
  gold_per_second: 1.0, description: '', is_active: 1,
}
const form = ref({ ...defaultForm })

function fetchList() {
  api.get('/levels').then((res) => { list.value = res.data })
}

function openDialog(row) {
  if (row) {
    isEdit.value = true
    editId.value = row.level_id
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
    ? api.put(`/levels/${editId.value}`, payload)
    : api.post('/levels', payload)
  req.then(() => {
    visible.value = false
    fetchList()
  })
}

function handleDelete(id) {
  api.delete(`/levels/${id}`).then(() => fetchList())
}

function toggleActive(row) {
  api.put(`/levels/${row.level_id}`, { ...row, is_active: row.is_active === 1 ? 0 : 1 }).then(() => fetchList())
}

onMounted(fetchList)
</script>
