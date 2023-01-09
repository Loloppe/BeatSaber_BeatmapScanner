# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance as base
+ Average angle strain + type for tech
+ Average note per second for speed

To fix:
- Data still show if you press song pack filter, favorite filter or a playlist (no idea how to fix that, would probably be easier to just link the text to an actual GameObject that already exist instead. That way we could remove the rest of the Harmony Patches.
- Some maps with Mapping Extension still end up being analyzed for some reason (it shouldn't). Haven't really tested much.
