# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance as base
+ Average angle strain + type for tech
+ Average note per second for speed

To fix:
- Some maps with Mapping Extension still end up being analyzed for some reason (it shouldn't). Haven't really tested much, I don't use those plugins.
- Some time the UI seems to broke and the label start duplicating, couldn't reproduce it properly.
