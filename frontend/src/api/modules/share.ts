import { publicHttp } from '../http'

export const shareApi = {
  publicDocument: (token: string, password: string | null = null) => publicHttp.post(`/share/public/${token}/access`, { password })
}
