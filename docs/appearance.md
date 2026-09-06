# 界面个性化设计

当前版本支持主题、语言、字号、水印与紧凑模式。前端设置即时生效并持久化到 localStorage；后端提供账号级设置接口，为登录模块完成后的多设备同步预留。

## 接口

- `GET /api/settings/appearance?userKey=local`
- `PUT /api/settings/appearance?userKey=local`

登录模块接入后，应移除客户端传入的 `userKey`，统一从 JWT Claims 获取当前用户标识。

## 约束

- 主题：`light | dark | system`
- 语言：`zh-CN | en-US`
- 字号：12 ~ 18px
- 水印文字：最多 40 个字符
