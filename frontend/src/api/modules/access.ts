import { http } from '../http'

export const accessApi = {
  users: (knowledgeBaseId: string, keyword = '', take = 50) =>
    http.get(`/access/knowledge-bases/${knowledgeBaseId}/users`, { params: { keyword, take } }),
  members: (knowledgeBaseId: string) =>
    http.get(`/access/knowledge-bases/${knowledgeBaseId}/members`),
  setMember: (knowledgeBaseId: string, payload: { userId: string; role: string }) =>
    http.put(`/access/knowledge-bases/${knowledgeBaseId}/members`, payload),
  removeMember: (knowledgeBaseId: string, userId: string) =>
    http.delete(`/access/knowledge-bases/${knowledgeBaseId}/members/${userId}`),
  documentPermissions: (documentId: string) =>
    http.get(`/access/documents/${documentId}/permissions`),
  setDocumentPermission: (documentId: string, payload: { userId: string; canView: boolean; canEdit: boolean; canManage: boolean }) =>
    http.put(`/access/documents/${documentId}/permissions`, payload),
  removeDocumentPermission: (documentId: string, userId: string) =>
    http.delete(`/access/documents/${documentId}/permissions/${userId}`)
}
