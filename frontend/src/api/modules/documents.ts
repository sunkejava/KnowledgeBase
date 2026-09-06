import { http } from '../http'

export const documentApi = {
  knowledgeBase: (id: string) => http.get(`/knowledge-bases/${id}`),
  list: (knowledgeBaseId: string) => http.get('/documents', { params: { knowledgeBaseId } }),
  get: (id: string) => http.get(`/documents/${id}`),
  create: (payload: unknown) => http.post('/documents', payload),
  update: (id: string, payload: unknown) => http.put(`/documents/${id}`, payload),
  remove: (id: string) => http.delete(`/documents/${id}`),
  recent: (id: string) => http.post(`/knowledge-assets/documents/${id}/recent`),
  favorite: (id: string) => http.post(`/knowledge-assets/documents/${id}/favorite`),
  favorites: () => http.get('/knowledge-assets/favorites'),
  versions: (id: string) => http.get(`/knowledge-assets/documents/${id}/versions`),
  createVersion: (id: string, changeNote: string) => http.post(`/knowledge-assets/documents/${id}/versions`, { changeNote }),
  restoreVersion: (versionId: string) => http.post(`/knowledge-assets/versions/${versionId}/restore`),
  diffVersion: (versionId: string) => http.get(`/content-exchange/versions/${versionId}/diff-current`),
  tags: () => http.get('/knowledge-assets/tags'),
  documentTags: (id: string) => http.get(`/knowledge-assets/documents/${id}/tags`),
  saveDocumentTags: (id: string, tagIds: string[]) => http.put(`/knowledge-assets/documents/${id}/tags`, tagIds),
  attachments: (id: string) => http.get(`/attachments/document/${id}`),
  uploadAttachment: (id: string, form: FormData) => http.post(`/attachments/document/${id}`, form, { headers: { 'Content-Type': 'multipart/form-data' } }),
  downloadAttachment: (url: string) => http.get(url, { responseType: 'blob' }),
  deleteAttachment: (id: string) => http.delete(`/attachments/${id}`),
  exportMarkdown: (id: string) => http.get(`/content-exchange/documents/${id}/markdown`),
  importMarkdown: (knowledgeBaseId: string, form: FormData) => http.post(`/content-exchange/knowledge-bases/${knowledgeBaseId}/markdown`, form, { headers: { 'Content-Type': 'multipart/form-data' } }),
  createShare: (id: string, expiresAt: string | null = null) => http.post(`/share/documents/${id}`, { expiresAt }),
  shares: (id: string) => http.get(`/share/documents/${id}`)
}
