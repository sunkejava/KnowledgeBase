export type TableAlign = 'left' | 'center' | 'right'

export interface TableColumn<T = Record<string, unknown>> {
  key: string
  label: string
  prop?: string
  width?: number
  minWidth?: number
  visible?: boolean
  sortable?: boolean
  exportable?: boolean
  align?: TableAlign
  fixed?: boolean | 'left' | 'right'
  formatter?: (row: T) => string | number | null | undefined
}

export interface TableColumnPreference {
  visible: boolean
  width?: number
}

export interface PageResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
