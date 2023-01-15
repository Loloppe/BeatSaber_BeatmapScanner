using BeatmapScanner.Algorithm.Loloppe;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BeatmapSaveDataVersion3.BeatmapSaveData;
using static BeatmapScanner.Algorithm.BeatmapScanner;

namespace BeatmapScanner.Algorithm
{
    internal class Helper
    {
        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static void SwapValue(List<Cube> list, int indexA, int indexB)
        {
            (list[indexB].Head, list[indexA].Head) = (list[indexA].Head, list[indexB].Head);
            (list[indexB].Reset, list[indexA].Reset) = (list[indexA].Reset, list[indexB].Reset);
            (list[indexB].SoftReset, list[indexA].SoftReset) = (list[indexA].SoftReset, list[indexB].SoftReset);
        }

        public static void FixPatternHead(List<Cube> cubes)
        {
            for (int j = 1; j < cubes.Count(); j++)
            {
                for (int i = 1; i < cubes.Count(); i++)
                {
                    if (cubes[i].Note.beat == cubes[i - 1].Note.beat)
                    {
                        switch (cubes[i - 1].Direction)
                        {
                            case 0: // Either this is wrong or both note are on same layer (loloppe notes), swapping should be fine right...
                                if (cubes[i - 1].Layer > cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 1:
                                if (cubes[i - 1].Layer < cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 2:
                                if (cubes[i - 1].Line < cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 3:
                                if (cubes[i - 1].Line > cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 4: // I know it's not great, but good enough for now
                                if (cubes[i - 1].Line < cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer > cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 5:
                                if (cubes[i - 1].Line > cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer > cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 6:
                                if (cubes[i - 1].Line < cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer < cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                            case 7:
                                if (cubes[i - 1].Line > cubes[i].Line)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                else if (cubes[i - 1].Layer < cubes[i].Layer)
                                {
                                    Swap(cubes, i - 1, i);
                                    SwapValue(cubes, i - 1, i);
                                }
                                break;
                        }
                    }
                }
            }
        }

        public static void FindReset(List<Cube> cubes)
        {
            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head) // Not a reset, since it's the same swing
                {
                    continue; // Reset is false by default
                }

                if (SameDirection(cubes[i - 1].Direction, cubes[i].Direction)) // Reset
                {
                    cubes[i].Reset = true;
                    continue;
                }

                if (cubes[i].Beat - cubes[i - 1].Beat >= 0.9f) // Assume that everything that's 1 beat or higher are soft reset.
                {
                    cubes[i].SoftReset = true;
                }
            }
        }

        public static bool SameDirection(int before, int after)
        {
            switch(before)
            {
                case 0: 
                    if (UpSwing.Contains(after)) return true;
                    break;
                case 1:
                    if (DownSwing.Contains(after)) return true;
                    break;
                case 2:
                    if (LeftSwing.Contains(after)) return true;
                    break;
                case 3:
                    if (RightSwing.Contains(after)) return true;
                    break;
                case 4:
                    if (UpLeftSwing.Contains(after)) return true;
                    break;
                case 5:
                    if (UpRightSwing.Contains(after)) return true;
                    break;
                case 6:
                    if (DownLeftSwing.Contains(after)) return true;
                    break;
                case 7:
                    if (DownRightSwing.Contains(after)) return true;
                    break;
            }

            return false;
        }

        public static void FindNoteDirection(List<Cube> cubes, List<BombNoteData> bombs)
        {
            if (((int)cubes[0].Note.cutDirection) == 8) // We find the first arrow note and then go backward
            {
                var c = cubes.Where(c => !c.Assumed).FirstOrDefault();
                if (c != null)
                {
                    int temp = 1;
                    for (int i = 0; i < cubes.IndexOf(c); i++)
                    {
                        temp = ReverseCutDirection((int)c.Note.cutDirection);
                    }
                    cubes[0].Direction = temp;
                }
                else // No choice but to assume, there's no arrow
                {
                    if (cubes[0].Note.layer == 2)
                    {
                        cubes[0].Direction = 0;
                    }
                    else
                    {
                        cubes[0].Direction = 1;
                    }
                }
            }
            else
            {
                cubes[0].Direction = (int)cubes[0].Note.cutDirection;
            }

            bool pattern = false;

            // This won't fix dot note properly, we will have to call it again after this method run
            FixPatternHead(cubes);

            // So now the note should technically be in proper order.. (or at least it shouldn't matter much)

            for (int i = 1; i < cubes.Count(); i++)
            {
                // Here we try to find if notes are part of the same swing
                if (cubes[i].Beat - cubes[i - 1].Beat <= 0.15 && (cubes[i].Note.cutDirection == cubes[i - 1].Note.cutDirection || // A bit faster than 1/8 and same direction
                    cubes[i].Assumed || SameDirection(cubes[i - 1].Direction, (int)cubes[i].Note.cutDirection))) // Or if the next note is a dot, or if parity break
                {
                    if (!pattern)
                    {
                        cubes[i - 1].Head = true;
                    }

                    cubes[i - 1].Pattern = true;
                    cubes[i].Pattern = true;
                    pattern = true;
                }
                else
                {
                    pattern = false;
                }

                BombNoteData bo = null;
                if (i != cubes.Count() - 1)
                {
                    bo = bombs.FirstOrDefault(b => cubes[i - 1].Beat < b.beat && cubes[i].Beat >= b.beat && cubes[i].Line == b.line);
                }

                if (bo != null)
                {
                    cubes[i].Bomb = true; // Bomb between, could be a reset
                }

                if (cubes[i].Pattern && !cubes[i].Head && cubes[i - 1].Bomb)
                {
                    cubes[i].Bomb = cubes[i - 1].Bomb;
                }

                if (cubes[i].Assumed && !cubes[i].Pattern && !cubes[i].Bomb) // Reverse the direction if there's no bomb reset and it's a dot
                {
                    cubes[i].Direction = ReverseCutDirection(cubes[i - 1].Direction);
                }
                else if (cubes[i].Assumed && cubes[i].Pattern) // Part of a pattern, the direction is the same as the last probably
                {
                    cubes[i].Direction = cubes[i - 1].Direction;
                }
                else if (cubes[i].Assumed && cubes[i].Bomb) // Is a dot and there's a bomb near in the same lane, probably a reset
                {
                    // For simplicity purpose
                    if (bo.layer == 0)
                    {
                        cubes[i].Direction = 1;
                    }
                    else if (bo.layer == 1)
                    {
                        if (cubes[i].Layer == 0)
                        {
                            cubes[i].Direction = 0;
                        }
                        else
                        {
                            cubes[i].Direction = 1;
                        }
                    }
                    else if (bo.layer == 2)
                    {
                        cubes[i].Direction = 0;
                    }
                }
                else // Current direction is fine
                {
                    cubes[i].Direction = (int)cubes[i].Note.cutDirection;
                }
            }

            // We're gonna assume the dot is the reset that way
            for (int i = cubes.Count() - 1; i >= 1; i--)
            {
                if (cubes[i].Direction != 8 && cubes[i - 1].Direction == 8)
                {
                    if (cubes[i].Beat - cubes[i - 1].Beat >= 0.9)
                    {
                        cubes[i - 1].Direction = ReverseCutDirection(cubes[i].Direction);
                    }
                }
            }
        }

        public static int ReverseCutDirection(int direction)
        {
            return direction switch
            {
                0 => 1,
                1 => 0,
                2 => 3,
                3 => 2,
                4 => 7,
                5 => 6,
                6 => 5,
                7 => 4,
                _ => 8,
            };
        }
    }
}
