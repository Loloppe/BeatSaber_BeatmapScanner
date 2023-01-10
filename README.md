# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance as base
+ Average angle strain + type + inverted for tech
+ Average intensity for speed

To fix:
- Some maps with Mapping Extension still end up being analyzed for some reason (it shouldn't). Haven't really tested much, I don't use those plugins.
- Lower acc map with a lot of DD are bugged atm. Also inverted notes calculation is wrong.

To add:
- Movement algorithm instead of inverted, new value called M(ovement)
