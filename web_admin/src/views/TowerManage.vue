<template>
  <div>
    <el-card>
      <div style="display: flex; justify-content: space-between; align-items: center">
        <span></span>
        <el-button type="primary" @click="openDialog()">新增防御塔</el-button>
      </div>

      <el-table :data="list" stripe style="margin-top: 16px">
        <el-table-column prop="tower_id" label="ID" width="60" />
        <el-table-column prop="name" label="名称" />
        <el-table-column prop="cost" label="造价" />
        <el-table-column prop="attack" label="攻击力" />
        <el-table-column prop="range_value" label="范围" />
        <el-table-column prop="cooldown" label="冷却(秒)" />
        <el-table-column prop="refund_rate" label="返金比例" />
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
            <el-popconfirm title="确定删除？" @confirm="handleDelete(scope.row.tower_id)">
              <template #reference>
                <el-button size="small" type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="visible" :title="isEdit ? '编辑防御塔' : '新增防御塔'" width="500px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="名称">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="造价">
          <el-input-number v-model="form.cost" :min="0" />
        </el-form-item>
        <el-form-item label="攻击力">
          <el-input-number v-model="form.attack" :min="0" />
        </el-form-item>
        <el-form-item label="攻击范围">
          <el-input-number v-model="form.range_value" :min="0" :precision="1" :step="0.5" />
        </el-form-item>
        <el-form-item label="冷却(秒)">
          <el-input-number v-model="form.cooldown" :min="0.1" :precision="1" :step="0.1" />
        </el-form-item>
        <el-form-item label="返金比例">
          <el-input-number v-model="form.refund_rate" :min="0" :max="1" :precision="2" :step="0.1" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.is_active" :active-value="1" :inactive-value="0" />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" />
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
  name: '', cost: 0, attack: 0, range_value: 0, cooldown: 1.0,
  refund_rate: 0.5, description: '', is_active: 1,
}
const form = ref({ ...defaultForm })

function fetchList() {
  api.get('/towers').then((res) => { list.value = res.data })
}

function openDialog(row) {
  if (row) {
    isEdit.value = true
    editId.value = row.tower_id
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
    ? api.put(`/towers/${editId.value}`, payload)
    : api.post('/towers', payload)
  req.then(() => {
    visible.value = false
    fetchList()
  })
}

function handleDelete(id) {
  api.delete(`/towers/${id}`).then(() => fetchList())
}

function toggleActive(row) {
  api.put(`/towers/${row.tower_id}`, { ...row, is_active: row.is_active === 1 ? 0 : 1 }).then(() => fetchList())
}

onMounted(fetchList)
</script>
