# Ui — Hollow Knight-style UI Template (Class 8 integration)

A Unity UI template inspired by Hollow Knight, built by **Jesse (urosaar)** and ** Ouss (oussmac) **.
This branch adds the **Class 8** scene, a working on-disk **save/profile system**
(an expansion by **oussmac**), and wiring for the controls / key-rebinding panel.

Built with Unity **6000.3.2f1**.

## Showcase

![Start Game](Docs/Images/Start-Game.png)

![Sound settings](Docs/Images/Sound.png)

![Inventory UI / key rebinding](Docs/Images/Inventory-UI.png)

## What's in here

- Scene: `Assets/Scenes/Class-8.unity`
- Art & panels: `Assets/Class-8/`
- UI scripts: `Assets/UI-scripts/`
- Editor tooling: `Assets/UI-scripts/Editor/`

## Save system (expansion by oussmac)

The template was UI-only, so this expansion makes profiles **actually save data to disk**:

- Profiles live as JSON under
  `Documents/Undeved/Class8/Profiles/Profile-1..3/save.json`
- `ProfileSystem.cs` — disk I/O (create / save / load / delete / exists) with error handling
- `ProfileSlot.cs` — each profile row toggles **New Game ↔ Continue** based on real on-disk state
- `HoverReveal.cs` — flicker-free hover reveal for the slot art

One-click editor tools (menu **Profile System**):
- Setup / repair the 3 profile rows in the Class 8 scene
- Delete ALL profile data on disk
- Reveal the profiles folder

## Controls panel

Menu **UI Tools → Copy inventory-rebinder to all keyboard rebinders** clones the
perfectly-tuned `inventory-rebinder` (label, button, key text) onto every other
rebinder in the Keyboard panel — no more hand-tuning each row.

## Credits

- UI template & art: **Jesse / urosaar**
- Class 8 integration + working save system: **oussmac**
