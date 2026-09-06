import { publicHttp } from '../http'

export const shareApi = {
  publicDocument: (token: string) => publicHttp.get(`/share/public/${token}`)
}
