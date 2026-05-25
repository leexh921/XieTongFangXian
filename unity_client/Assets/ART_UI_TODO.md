# 美术与 UI 交接清单

本文档给后续负责 Sprite 替换和 UI 美化的同学使用。当前 Demo 的核心流程、协议字段和建塔逻辑已经接好，建议优先改资源和 Inspector 配置，尽量不要改核心逻辑代码。

## 需要替换的 Sprite

- `Assets/Prefabs/Tile.prefab`：地块默认图。
- `MapManager` Inspector：可建造地块 `center / edge / corner` Sprite。
- `MapManager` Inspector：`pathSprite` 路径图。
- `MapManager` Inspector：`obstacleSprite` 障碍图。
- `MapManager` Inspector：`castleSprite` 堡垒图。
- `Assets/Prefabs/Tower.prefab`：默认塔图。
- `VisualConfigManager` Inspector：不同 `tower_id -> Sprite` 映射。
- `Assets/Prefabs/Monster.prefab`：默认怪物图。
- `VisualConfigManager` Inspector：不同 `monster_id -> Sprite` 映射。
- UI 按钮背景图，包括登录、开始、建塔弹窗、结算按钮。
- `Assets/Prefabs/TowerCard.prefab`：卡片背景和 `Icon` 图标。
- `BattleScene` 中 `ResultPanel`：结算面板背景。

## 需要美化的 UI

- `LoginScene`：昵称输入和登录按钮。
- `LobbyScene`：开始界面、关卡信息展示。
- `BattleScene`：顶部/侧边状态栏，包括 Gold、Score、Base HP、Kills、Time。
- `TowerBuildPopup`：点击地块后的建塔弹窗。
- `TowerCard`：塔卡片布局、按钮状态、图标尺寸。
- `ResultPanel`：胜利/失败结算界面。

## 需要配置的 Inspector 字段

- `MapManager`：地块、路径、障碍、堡垒 Sprite。
- `VisualConfigManager`：`tower_id -> Sprite` 映射。
- `VisualConfigManager`：`monster_id -> Sprite` 映射。
- `TowerCard.prefab`：`Icon`、背景 Image、文本样式。
- TextMeshPro：中文字体资源当前使用 `Assets/Fonts/SimHei Dynamic SDF.asset`，如替换字体，请更新 TMP Settings fallback。

## 建议不要改的核心逻辑文件

- `Assets/Scripts/Network/NetworkManager.cs`
- `Assets/Scripts/Network/MockServerClient.cs`
- `Assets/Scripts/Network/JsonModels.cs`
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/Battle/MapManager.cs` 的核心建塔逻辑
- `Assets/Scripts/Battle/StateRenderer.cs` 的状态渲染逻辑

## 后续新增塔

1. Web 后台新增 tower。
2. Server 在 `game_start.data.tower_config` 下发新塔配置。
3. Unity 的 `TowerBuildPopup` 会自动显示新塔。
4. 在 `VisualConfigManager` 添加对应 `tower_id` 的 Sprite。
5. 不需要改 `MapManager` 或 `NetworkManager`。

## 后续新增怪物

1. Web 后台新增 monster。
2. `wave_event` 使用新怪物。
3. Server 在 `state_update.data.monsters[].monster_id` 下发怪物 ID。
4. 在 `VisualConfigManager` 添加对应 `monster_id` 的 Sprite。
5. 不需要改 `StateRenderer` 核心逻辑。
