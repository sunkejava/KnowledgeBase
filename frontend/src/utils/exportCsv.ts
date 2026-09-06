import type { TableColumn } from '../types/table'

function escapeCsv(value: unknown) {
  const text = value == null ? '' : String(value)
  return `"${text.replace(/"/g, '""')}"`
}

export function exportRowsToCsv<T extends Record<string, unknown>>(fileName: string, columns: TableColumn<T>[], rows: T[]) {
  const exportColumns = columns.filter(column => column.exportable !== false)
  const header = exportColumns.map(column => escapeCsv(column.label)).join(',')
  const body = rows.map(row => exportColumns.map(column => {
    const value = column.formatter ? column.formatter(row) : row[column.prop || column.key]
    return escapeCsv(value)
  }).join(',')).join('\r\n')
  const blob = new Blob([`\ufeff${header}\r\n${body}`], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName.endsWith('.csv') ? fileName : `${fileName}.csv`
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
