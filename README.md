# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance as base
+ Active NPS for speed
+ Strain calculator for tech
+ Divided by X factor to bring it closer to known value (SS and BL).

TODO:

1. Fix CalcSwingCurve tech factor, right now the data seems to be all over the place for some maps
2. Better way to represent speed? Or at least rework the NPS
3. Detect MappingExtension/NoodleExtension maps and ignore them?
