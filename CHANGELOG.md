# Changelog

## 1.0.0 (API version 1)

First version.

- Docking: `syx.register` / `syx.hello` / `syx.bye` on mod message channel 8346120579001.
- Endpoints: `Info.IsServer`, `Info.IsPlayer`, `Info.PromoteLevel`, `Net.Register`, `Net.Unregister`,
  `Net.SendToServer`, `Net.SendToPlayer`, `Net.SendToAll`, `Block.Get`, `Block.Set`, `Block.OnChanged`,
  `Block.RemoveOnChanged`, `Block.RequestSync`, `Chat.Register`, `Events.Subscribe`,
  `Events.Unsubscribe`, `Events.Publish`, `Notify.Player`.
- Secure network channel 52741, block storage GUID 583FC123-E62D-4269-A84C-14B32976D720.
- Dock file helpers: `SyntrixText`, `SyntrixLog`, `SyntrixControls<TBlock>`.
