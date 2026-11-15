# Changelog

## 1.1.0 (API version 2)

Dock files of API version 1 keep working. New endpoints:

- Chat, events, settings: `Chat.Unregister`, `Events.SendToServer`, `Events.SendToAll`,
  `Events.SendToPlayer`, `Events.Sender`, `Settings.Publish`, `Settings.Subscribe`.
- UI service (players only, needs Rich HUD Master, Workshop 1965654081): `Ui.State`, `Ui.Events`,
  `Ui.Release`, `Ui.Create`, `Ui.Remove`, `Ui.Clear`, `Ui.SetParent`, `Ui.Part`, `Ui.Listen`,
  `Ui.SetBool`, `Ui.SetInt`, `Ui.SetFloat`, `Ui.SetVector`, `Ui.SetColor`, `Ui.SetText`,
  `Ui.GetBool`, `Ui.GetInt`, `Ui.GetFloat`, `Ui.GetVector`, `Ui.GetColor`, `Ui.GetText`,
  `Ui.Focus`, `Ui.Measure`, `Ui.Screen`, `Ui.ResScale`, `Ui.CursorPos`, `Ui.Cursor`, `Ui.Bind`,
  `Ui.BindKeys`, `Ui.BindPage`. Element kinds, properties and events are fixed numbers
  (`UiKind`, `UiProp`, `UiEvent` in `Api/SyntrixUi.cs`).
- Text markup for the UI: `[b]`, `[i]`, `[u]`, `[color=..]`, `[size=..]`, `[font=..]`,
  `[align=..]`, `[link=..]`, closed with `[/tag]`; `[[` is a literal `[`.
- The Rich HUD Framework client (MIT, Zach Hembree) is now part of Syntrix; Rich HUD Master is a
  dependency.
- Dock files: `Api/SyntrixUi.cs` (optional second file with the UI wrappers `SyntrixUi`, `UiWindow`,
  `UiPanel`, `UiLabel`, `UiLabelBox`, `UiButton`, `UiTextField`, `UiBox`, `UiBorder`, `UiScrollBox`,
  `UiChain`, `UiCheckBox`, `UiSlider`); in `SyntrixApi.cs` registrations before docking are kept,
  plus `SyntrixFiles`, log files for `SyntrixLog` and more terminal controls in
  `SyntrixControls<TBlock>`.

## 1.0.0 (API version 1)

First version.

- Docking: `syx.register` / `syx.hello` / `syx.bye` on mod message channel 8346120579001.
- Endpoints: `Info.IsServer`, `Info.IsPlayer`, `Info.PromoteLevel`, `Net.Register`, `Net.Unregister`,
  `Net.SendToServer`, `Net.SendToPlayer`, `Net.SendToAll`, `Block.Get`, `Block.Set`, `Block.OnChanged`,
  `Block.RemoveOnChanged`, `Block.RequestSync`, `Chat.Register`, `Events.Subscribe`,
  `Events.Unsubscribe`, `Events.Publish`, `Notify.Player`.
- Secure network channel 52741, block storage GUID 583FC123-E62D-4269-A84C-14B32976D720.
- Dock file helpers: `SyntrixText`, `SyntrixLog`, `SyntrixControls<TBlock>`.
