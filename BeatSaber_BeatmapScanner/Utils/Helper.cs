using static BeatmapSaveDataVersion3.BeatmapSaveData;
using BeatmapScanner.Algorithm.Loloppe;
using System.Collections.Generic;
using System.Linq;

namespace BeatmapScanner.Algorithm
{
    internal class Helper
    {
        #region Array

        public static int[] VerticalSwing = { 0, 1, 4, 5, 6, 7 };
        public static int[] HorizontalSwing = { 2, 3, 4, 5, 6, 7 };
        public static int[] DiagonalSwing = { 4, 5, 6, 7 };
        public static int[] PureVerticalSwing = { 0, 1 };
        public static int[] PureHorizontalSwing = { 2, 3 };

        public static double[] UpSwing = { 0, 4, 5 };
        public static double[] DownSwing = { 1, 6, 7 };
        public static double[] LeftSwing = { 2, 4, 6 };
        public static double[] RightSwing = { 3, 5, 7 };
        public static double[] UpLeftSwing = { 0, 2, 4 };
        public static double[] DownLeftSwing = { 1, 2, 6 };
        public static double[] UpRightSwing = { 0, 3, 5 };
        public static double[] DownRightSwing = { 1, 3, 7 };

        #endregion

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static void SwapValue(List<Cube> list, int indexA, int indexB)
        {
            (list[indexB].Head, list[indexA].Head) = (list[indexA].Head, list[indexB].Head);
            (list[indexB].Reset, list[indexA].Reset) = (list[indexA].Reset, list[indexB].Reset);
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
                            case 0:
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
                            case 4: 
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
                if (cubes[i].Pattern && !cubes[i].Head)
                {
                    continue; 
                }

                if (SameDirection(cubes[i - 1].Direction, cubes[i].Direction))
                {
                    cubes[i].Reset = true;
                    continue;
                }
            }
        }

        public static bool SameDirection(double before, double after)
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

        public static void FindNoteDirection(List<Cube> cubes, List<BombNoteData> bombs, float bpm)
        {
            if (cubes[0].Assumed) 
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
                else 
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

            FixPatternHead(cubes);

            BombNoteData bo = null;

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Beat - cubes[i - 1].Beat <= (0.25 / 200 * bpm) && (cubes[i].Note.cutDirection == cubes[i - 1].Note.cutDirection ||
                    cubes[i].Assumed || cubes[i - 1].Assumed || SameDirection((int)cubes[i - 1].Note.cutDirection, (int)cubes[i].Note.cutDirection))) 
                {
                    if (!pattern)
                    {
                        cubes[i - 1].Head = true;
                        if(cubes[i].Beat - cubes[i - 1].Beat < 0.26 && cubes[i].Beat - cubes[i - 1].Beat >= 0.01)
                        {
                            cubes[i - 1].Slider = true;
                        }
                    }

                    cubes[i - 1].Pattern = true;
                    cubes[i].Pattern = true;
                    pattern = true;
                }
                else
                {
                    pattern = false;
                }

                bo = bombs.LastOrDefault(b => cubes[i - 1].Beat < b.beat && cubes[i].Beat >= b.beat && cubes[i].Line == b.line);

                if (bo != null)
                {
                    cubes[i].Bomb = true; 
                }

                if (cubes[i].Pattern && cubes[i - 1].Bomb)
                {
                    cubes[i].Bomb = cubes[i - 1].Bomb;
                }

                if (cubes[i].Assumed && !cubes[i].Pattern && !cubes[i].Bomb) 
                {
                    cubes[i].Direction = ReverseCutDirection((int)cubes[i - 1].Note.cutDirection);
                }
                else if (cubes[i].Assumed && cubes[i].Pattern)
                {
                    cubes[i].Direction = cubes[i - 1].Direction;
                }
                else if (cubes[i].Assumed && cubes[i].Bomb) 
                {
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
                else 
                {
                    cubes[i].Direction = (int)cubes[i].Note.cutDirection;
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
