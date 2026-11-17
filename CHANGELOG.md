# Changelog

## 1.1.0 (API version 2)

Dock files of API version 1 keep working; no endpoint or dock file method of 1.0.0 changed.

### Dock files

- `Api/SyntrixApi.cs` stays the only file a mod needs. The UI service comes in three optional files
  that are always copied together: `Api/SyntrixUi.cs` (service, base elements), `Api/SyntrixUi.Controls.cs`
  (Rich HUD controls, HUD spaces, shapes) and `Api/SyntrixUi.Terminal.cs` (Rich HUD terminal pages).
- Registrations made before docking (network channels, block change handlers, chat commands, event
  subscriptions, settings) are kept and handed over when Syntrix is ready.
- `SyntrixFiles`: text files in world and local storage, never throws.
- `SyntrixLog`: optional own log file in local storage (`SyntrixLog(modName, fileName)`, `Close`).
- XML documentation on every public member.

### Services

- Chat: `Chat.Unregister`.
- Events between machines: `Events.SendToServer`, `Events.SendToAll`, `Events.SendToPlayer` (string
  arguments) and `Events.Sender` (Steam id of the sender of the event being handled).
- Settings: `Settings.Publish`, `Settings.Subscribe`; in the dock file `SettingsLoad`, `SettingsSave`,
  `SettingsText` (a text file in the world storage, the same text on every player).

### Terminal controls (`SyntrixControls<TBlock>`)

- New controls: on/off switch, colour picker, list box (multiselect, visible rows), sliders with
  `SyntrixSliderScale` (linear, logarithmic, dual logarithmic) and limits from functions, check box and
  on/off switch with own on/off texts. Every control comes bound to a block setting and with own getter
  and setter.
- Terminal properties for programmable blocks: `PropertyBool`, `PropertyFloat`, `PropertyText`,
  `PropertyColor` (bound) and `Property<TValue>` (own getter and setter), plus `AddProperty`.
- Toolbar actions: invalid toolbar types and "valid for groups".
- `ListMode` (one set of controls for several block types), `EnsureCreated` (dedicated servers),
  `Created`; all new controls and properties work with both.

### UI service (players only, needs Rich HUD Master, Workshop 1965654081)

- Endpoints: `Ui.State`, `Ui.Events`, `Ui.Release`, `Ui.Create`, `Ui.Remove`, `Ui.Clear`,
  `Ui.SetParent`, `Ui.Part`, `Ui.Listen`, `Ui.SetBool`, `Ui.SetInt`, `Ui.SetFloat`, `Ui.SetVector`,
  `Ui.SetColor`, `Ui.SetText`, `Ui.GetBool`, `Ui.GetInt`, `Ui.GetFloat`, `Ui.GetVector`, `Ui.GetColor`,
  `Ui.GetText`, `Ui.SetVector3`, `Ui.GetVector3`, `Ui.SetMatrix`, `Ui.SetPoints`, `Ui.ItemInsert`,
  `Ui.ItemRemove`, `Ui.ItemText`, `Ui.ItemEnabled`, `Ui.ItemKey`, `Ui.ItemFind`, `Ui.Focus`,
  `Ui.Measure`, `Ui.Screen`, `Ui.ScreenInfo`, `Ui.ResScale`, `Ui.CursorPos`, `Ui.CursorWorld`,
  `Ui.Cursor`, `Ui.Clipboard`, `Ui.SetClipboard`, `Ui.Terminal`, `Ui.TerminalOpen`, `Ui.Bind`,
  `Ui.BindKeys`, `Ui.BindPage`. Element kinds, properties and events are fixed numbers (`UiKind`,
  `UiProp`, `UiEvent`).
- Elements: window, panel, label, label box, button, text field, box (colour or texture), border,
  scroll box, chain, empty, click area, check box, slider; multi-line text box, drop-down, list box,
  tree list, tree box, colour picker (RGB and HSV), on/off button, named check box, named on/off
  button, named slider, scroll bar, slider bar, icon button, label button, bordered button, double
  label box, radial selection; HUD spaces (camera, world matrix, scaled); shapes (line, polygon, ring,
  triangle mesh).
- Rich HUD terminal: page categories, control pages with control categories, tiles and controls
  (button, check box, on/off, colour picker, drag box, drop-down, label, list, slider, text field),
  text pages; opening the terminal on a page.
- Tooltips with markup and own background colour, clipboard, screen values (aspect ratio, field of
  view, input mode, cursor state), key binds with a rebind page.
- Text markup: `[b]`, `[i]`, `[u]`, `[color=..]`, `[size=..]`, `[font=..]`, `[align=..]`, `[link=..]`,
  closed with `[/tag]`; `[[` is a literal `[`.
- The Rich HUD Framework client (MIT, Zach Hembree) is now part of Syntrix; Rich HUD Master is a
  dependency.

## 1.0.0 (API version 1)

First version.

- Docking: `syx.register` / `syx.hello` / `syx.bye` on mod message channel 8346120579001.
- Endpoints: `Info.IsServer`, `Info.IsPlayer`, `Info.PromoteLevel`, `Net.Register`, `Net.Unregister`,
  `Net.SendToServer`, `Net.SendToPlayer`, `Net.SendToAll`, `Block.Get`, `Block.Set`, `Block.OnChanged`,
  `Block.RemoveOnChanged`, `Block.RequestSync`, `Chat.Register`, `Events.Subscribe`,
  `Events.Unsubscribe`, `Events.Publish`, `Notify.Player`.
- Secure network channel 52741, block storage GUID 583FC123-E62D-4269-A84C-14B32976D720.
- Dock file helpers: `SyntrixText`, `SyntrixLog`, `SyntrixControls<TBlock>`.
