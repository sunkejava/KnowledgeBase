import { http } from '../http'

export interface DashboardRecentDocument {
  documentId: string
  knowledgeBaseId: string
  title: string
  knowledgeBaseName: string
  lastViewedAt: string
  viewCount: number
}

export interface DashboardPopularDocument {
  documentId: string
  knowledgeBaseId: string
  title: string
  viewCount: number
}

export interface DashboardTaskSummary {
  type: string
  pending: number
  running: number
  failed: number
}

export interface DashboardOverview {
  knowledgeBaseCount: number
  documentCount: number
  favoriteCount: number
  unreadNotificationCount: number
  memberCount: number
  failedTaskCount: number
  recentDocuments: DashboardRecentDocument[]
  popularDocuments: DashboardPopularDocument[]
  taskSummaries: DashboardTaskSummary[]
}

export const dashboardApi = {
  overview: () => http.get<DashboardOverview>('/dashboard/overview')
}
