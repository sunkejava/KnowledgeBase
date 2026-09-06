import { http } from '../http'

export const collaborationApi = {
  comments: (documentId: string, page = 1, pageSize = 20, keyword = '') =>
    http.get(`/collaboration/documents/${documentId}/comments`, { params: { page, pageSize, keyword } }),
  mentionUsers: (documentId: string, keyword = '', take = 30) =>
    http.get(`/collaboration/documents/${documentId}/mention-users`, { params: { keyword, take } }),
  createComment: (documentId: string, payload: { content: string; parentId?: string | null; mentionUserIds?: string[] }) =>
    http.post(`/collaboration/documents/${documentId}/comments`, payload),
  updateComment: (commentId: string, payload: { content: string; mentionUserIds?: string[] }) =>
    http.put(`/collaboration/comments/${commentId}`, payload),
  deleteComment: (commentId: string) => http.delete(`/collaboration/comments/${commentId}`),
  notifications: (page = 1, pageSize = 20, keyword = '') =>
    http.get('/collaboration/notifications', { params: { page, pageSize, keyword } }),
  notificationSummary: () => http.get('/collaboration/notifications/summary'),
  markRead: (id: string) => http.post(`/collaboration/notifications/${id}/read`),
  markAllRead: () => http.post('/collaboration/notifications/read-all')
}
