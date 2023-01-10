# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance
+ Average (T)ech + angle strain
+ Average (I)ntensity
+ Average pattern (M)ovement + inverted


To fix:
- Some maps with Mapping Extension still end up being analyzed for some reason (it shouldn't). Haven't really tested much, I don't use those plugins.
- Tech map that heavily rely on wrist rolls end up super overweighted because it both affect Tech and Movement, need to nerf that somehow.
- DD heavy map ended up nerfed due to my fix to true acc reset maps, need to somehow fix that (or reduce the nerf).

To do:
- Port the plugin to ChroMapper
