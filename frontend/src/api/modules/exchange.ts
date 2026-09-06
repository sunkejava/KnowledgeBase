import { http } from '../http'

export const exchangeApi = {
  exportTasks: (page = 1, pageSize = 20) => http.get('/export-tasks', { params: { page, pageSize } }),
  createExportTask: (knowledgeBaseId: string) => http.post('/export-tasks', { knowledgeBaseId, format: 'zip' }),
  downloadExportTask: (id: string) => http.get(`/export-tasks/${id}/download`, { responseType: 'blob' }),
  cancelExportTask: (id: string) => http.post(`/export-tasks/${id}/cancel`),
  retryExportTask: (id: string) => http.post(`/export-tasks/${id}/retry`),
  cleanupExportTasks: (olderThanDays = 7) => http.delete('/export-tasks/cleanup', { params: { olderThanDays } }),

  importTasks: (page = 1, pageSize = 20) => http.get('/import-tasks', { params: { page, pageSize } }),
  createImportTask: (knowledgeBaseId: string, form: FormData) => http.post(
    `/import-tasks/knowledge-bases/${knowledgeBaseId}/zip`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  ),
  cancelImportTask: (id: string) => http.post(`/import-tasks/${id}/cancel`),
  retryImportTask: (id: string) => http.post(`/import-tasks/${id}/retry`),
  cleanupImportTasks: (olderThanDays = 7) => http.delete('/import-tasks/cleanup', { params: { olderThanDays } }),

  importHtml: (knowledgeBaseId: string, form: FormData) => http.post(
    `/content-exchange/knowledge-bases/${knowledgeBaseId}/html`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  ),
  importDocx: (knowledgeBaseId: string, form: FormData) => http.post(
    `/content-exchange/knowledge-bases/${knowledgeBaseId}/docx`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  )
}
