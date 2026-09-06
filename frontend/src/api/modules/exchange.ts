import { http } from '../http'

export const exchangeApi = {
  tasks: (page = 1, pageSize = 20) => http.get('/export-tasks', { params: { page, pageSize } }),
  createTask: (knowledgeBaseId: string) => http.post('/export-tasks', { knowledgeBaseId, format: 'zip' }),
  downloadTask: (id: string) => http.get(`/export-tasks/${id}/download`, { responseType: 'blob' }),
  importZip: (knowledgeBaseId: string, form: FormData) => http.post(
    `/content-exchange/knowledge-bases/${knowledgeBaseId}/markdown-zip`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  )
}
