# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance as base
+ Average angle strain + type + inverted for tech
+ Average intensity for speed

To fix:
- Some maps with Mapping Extension still end up being analyzed for some reason (it shouldn't). Haven't really tested much, I don't use those plugins.
- Tech map that heavily rely on wrist rolls end up super overweighted because it both affect Tech and Movement, need to nerf that somehow.
