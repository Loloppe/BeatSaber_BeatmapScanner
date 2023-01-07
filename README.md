# BeatSaber_BeatmapScanner
Analyze the difficulty of a beatmap

This plugin will show a difficulty value at the bottom of the selected custom beatmap. <br />

The algorithm currently use:
+ Average swing distance as base
+ Active NPS for speed
+ Strain calculator for tech
+ Divided by X factor to bring it closer to known value (SS and BL).

TODO:

1. Clean up GetScorePerHand (remove multiplier) and replace GetDistance with CalcSwingCurve data
2. Fix CalcSwingCurve, right now the data seems to be all over the place for some maps
3. Better way to represent speed? Or at least rework the NPS

BUG TO FIX:

+ Index was out of range get thrown out randomly for some specific maps (in ProcessSwing I think). There's trycatch so the algorithm will still work anyway for now.
