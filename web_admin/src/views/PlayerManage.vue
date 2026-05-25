<template>
  <div>
    <el-card>
      <div style="display: flex; justify-content: space-between; align-items: center">
        <span></span>
        <el-button type="primary" @click="openDialog()">新增玩家</el-button>
      </div>

      <el-table :data="list" stripe style="margin-top: 16px">
        <el-table-column prop="player_id" label="ID" width="80" />
        <el-table-column prop="username" label="用户名" />
        <el-table-column prop="created_at" label="创建时间" width="180" />
        <el-table-column label="操作" width="160">
          <template #default="scope">
            <el-button size="small" @click="openDialog(scope.row)">编辑</el-button>
            <el-popconfirm title="确定删除？" @confirm="handleDelete(scope.row.player_id)">
              <template #reference>
                <el-button size="small" type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="visible" :title="isEdit ? '编辑玩家' : '新增玩家'" width="460px">
      <el-form :model="form" label-width="80px">
        <el-form-item label="用户名">
          <el-input v-model="form.username" placeholder="请输入用户名" />
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

const defaultForm = { username: '' }
const form = ref({ ...defaultForm })

function fetchList() {
  api.get('/players').then((res) => { list.value = res.data })
}

function openDialog(row) {
  if (row) {
    isEdit.value = true
    editId.value = row.player_id
    form.value = { username: row.username }
  } else {
    isEdit.value = false
    editId.value = null
    form.value = { ...defaultForm }
  }
  visible.value = true
}

function handleSave() {
  const req = isEdit.value
    ? api.put(`/players/${editId.value}`, form.value)
    : api.post('/players', form.value)
  req.then(() => {
    visible.value = false
    fetchList()
  })
}

function handleDelete(id) {
  api.delete(`/players/${id}`).then(() => fetchList())
}

onMounted(fetchList)
</script>
