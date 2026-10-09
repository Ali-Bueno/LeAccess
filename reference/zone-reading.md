# Area and era announcements

`Zones/ZoneAnnouncer` speaks an area once per scene from `SceneDetails.LocalizedName` (plus the location banner's
visible year/chapter when it matches), after loading and when no window owns speech; `ZoneAnnouncementState` keeps it
to one delivery.

## Eras (2026-10-08)

User request: say which era we are in. `SceneDetails.era` is `SceneList.Era` (Ancient, Divine, Imperial, Ruined,
EndOfTime). The game's string tables (`localization-string-tables-*` addressable bundles, Common table) name them
`Era_Name_Ancient`, `Era_Name_Divine`, `Era_Name_Imperial`, `Era_Name_Ruined` ("Era Antigua", "Era Divina", … in
es-ES); the End of Time has no era name. `ZoneEra.Name` reads them through `Localization.GetText`. The area
announcement adds the era only when it differs from the last one spoken ("Ruinas desmoronadas. Era en ruinas", then
just the area names until travel changes the era); the remembered era survives resets. The Create+Circle / Ctrl+L
status report adds the era to the zone. MenuZone 46 checks; listening pending.
