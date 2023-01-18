using static BeatmapSaveDataVersion3.BeatmapSaveData;
using BeatmapScanner.Algorithm.LackWiz;
using System.Collections.Generic;
using System.Linq;

namespace BeatmapScanner.Algorithm.Loloppe
{
    internal class Test
    {
        #region Testing

        #region Flow

        // Find dot and arrow direction based on very basic depth searching
        public static double Pepega(SwingData previous, List<ColorNoteData> notes)
        {
            #region Prep

            var previousNote = notes.Find(n => n == previous.Note);
            var indexToFind = notes.IndexOf(previousNote) + 1;
            double possibleAngle;

            if (previous.Angle >= 180)
            {
                possibleAngle = previous.Angle - 180;
            }
            else
            {
                possibleAngle = previous.Angle + 180;
            }

            var testAngle = possibleAngle;

            #endregion

            #region Default

            for (int i = indexToFind; i < notes.Count() - 1; i++) // We check if the current flow work the the next arrow, or if it end up on a reset
            {
                if ((int)notes[i + 1].cutDirection != 8) // Arrow found
                {
                    var nextAngle = (double)(Method.CutDirectionIndex[(int)notes[i + 1].cutDirection] + notes[i + 1].angleOffset);
                    if (Funny(testAngle, nextAngle)) // Check if it's a DD
                    {
                        break;
                    }
                    else // This angle is good
                    {
                        return possibleAngle;
                    }
                }

                if (previous.Angle >= 180) // For every any direction met, reverse rotation
                {
                    testAngle -= 180;
                }
                else
                {
                    testAngle += 180;
                }
            }

            #endregion

            #region 45

            testAngle = possibleAngle;
            if (previousNote.color == 0)
            {
                testAngle = (testAngle + 45) % 360;
            }
            else
            {
                testAngle = (testAngle - 45) % 360;
            }

            for (int i = indexToFind; i < notes.Count() - 1; i++) // We check if the current flow work the the next arrow, or if it end up on a reset
            {
                if ((int)notes[i + 1].cutDirection != 8) // Arrow found
                {
                    var nextAngle = (double)(Method.CutDirectionIndex[(int)notes[i + 1].cutDirection] + notes[i + 1].angleOffset);
                    if (Funny(testAngle, nextAngle)) // Check if it's a DD
                    {
                        break;
                    }
                    else // This angle is good
                    {
                        if (previousNote.color == 0)
                        {
                            return (possibleAngle + 45) % 360;
                        }
                        else
                        {
                            return (possibleAngle - 45) % 360;
                        }
                    }
                }

                if (previous.Angle >= 180) // For every any direction met, reverse rotation
                {
                    testAngle -= 180;
                }
                else
                {
                    testAngle += 180;
                }
            }

            testAngle = possibleAngle;
            if (previousNote.color == 0)
            {
                testAngle = (testAngle - 45) % 360;
            }
            else
            {
                testAngle = (testAngle + 45) % 360;
            }

            for (int i = indexToFind; i < notes.Count() - 1; i++) // We check if the current flow work the the next arrow, or if it end up on a reset
            {
                if ((int)notes[i + 1].cutDirection != 8) // Arrow found
                {
                    var nextAngle = (double)(Method.CutDirectionIndex[(int)notes[i + 1].cutDirection] + notes[i + 1].angleOffset);
                    if (Funny(testAngle, nextAngle)) // Check if it's a DD
                    {
                        break;
                    }
                    else // This angle is good
                    {
                        if (previousNote.color == 0)
                        {
                            return (possibleAngle - 45) % 360;
                        }
                        else
                        {
                            return (possibleAngle + 45) % 360;
                        }
                    }
                }

                if (previous.Angle >= 180) // For every any direction met, reverse rotation
                {
                    testAngle -= 180;
                }
                else
                {
                    testAngle += 180;
                }
            }

            #endregion

            #region 90

            testAngle = possibleAngle;
            if (previousNote.color == 0)
            {
                testAngle = (testAngle + 90) % 360;
            }
            else
            {
                testAngle = (testAngle - 90) % 360;
            }

            for (int i = indexToFind; i < notes.Count() - 1; i++) // We check if the current flow work the the next arrow, or if it end up on a reset
            {
                if ((int)notes[i + 1].cutDirection != 8) // Arrow found
                {
                    var nextAngle = (double)(Method.CutDirectionIndex[(int)notes[i + 1].cutDirection] + notes[i + 1].angleOffset);
                    if (Funny(testAngle, nextAngle)) // Check if it's a DD
                    {
                        break;
                    }
                    else // This angle is good
                    {
                        if (previousNote.color == 0)
                        {
                            return (possibleAngle + 90) % 360;
                        }
                        else
                        {
                            return (possibleAngle - 90) % 360;
                        }
                    }
                }

                if (previous.Angle >= 180) // For every any direction met, reverse rotation
                {
                    testAngle -= 180;
                }
                else
                {
                    testAngle += 180;
                }
            }

            testAngle = possibleAngle;
            if (previousNote.color == 0)
            {
                testAngle = (testAngle - 90) % 360;
            }
            else
            {
                testAngle = (testAngle + 90) % 360;
            }

            for (int i = indexToFind; i < notes.Count() - 1; i++) // We check if the current flow work the the next arrow, or if it end up on a reset
            {
                if ((int)notes[i + 1].cutDirection != 8) // Arrow found
                {
                    var nextAngle = (double)(Method.CutDirectionIndex[(int)notes[i + 1].cutDirection] + notes[i + 1].angleOffset);
                    if (Funny(testAngle, nextAngle)) // Check if it's a DD
                    {
                        break;
                    }
                    else // This angle is good
                    {
                        if (previousNote.color == 0)
                        {
                            return (possibleAngle - 90) % 360;
                        }
                        else
                        {
                            return (possibleAngle + 90) % 360;
                        }
                    }
                }

                if (previous.Angle >= 180) // For every any direction met, reverse rotation
                {
                    testAngle -= 180;
                }
                else
                {
                    testAngle += 180;
                }
            }

            #endregion

            // Let just assume that 180 degree rotation are DD at that point..

            return possibleAngle; // Gave up
        }

        #endregion

        #region DD

        public static bool Funny(double previous, double next)
        {
            switch (previous)
            {
                case (<= 360 and >= 337.5f) or < 22.5f: // Right direction
                    if (next < 67.5 || next >= 292.5)
                    {
                        return true;
                    }
                    break;
                case < 67.5 and >= 22.5f: // Up Right direction
                    if (next < 112.5)
                    {
                        return true;
                    }
                    break;
                case < 112.5 and >= 67.5f: // Up direction
                    if (next < 157.5 && next >= 22.5)
                    {
                        return true;
                    }
                    break;
                case < 157.5 and >= 112.5: // Up Left direction
                    if (next < 202.5 && next >= 67.5)
                    {
                        return true;
                    }
                    break;
                case < 202.5 and >= 157.5f: // Left direction
                    if (next < 247.5 && next >= 112.5)
                    {
                        return true;
                    }
                    break;
                case < 247.5 and >= 202.5f: // Down Left direction
                    if (next < 292.5 && next >= 157.5)
                    {
                        return true;
                    }
                    break;
                case < 292.5 and >= 247.5f: // Down direction
                    if (next < 337.5 && next >= 202.5)
                    {
                        return true;
                    }
                    break;
                case < 337.5 and >= 292.5f: // Down Right direction
                    if (next < 22.5 || next >= 247.5)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        public static bool Funny(SwingData previous, SwingData next)
        {
            switch (previous.Angle)
            {
                case (<= 360 and >= 337.5f) or < 22.5f: // Right direction
                    if (next.Angle < 67.5 || next.Angle >= 292.5)
                    {
                        return true;
                    }
                    break;
                case < 67.5 and >= 22.5f: // Up Right direction
                    if (next.Angle < 112.5)
                    {
                        return true;
                    }
                    break;
                case < 112.5 and >= 67.5f: // Up direction
                    if (next.Angle < 157.5 && next.Angle >= 22.5)
                    {
                        return true;
                    }
                    break;
                case < 157.5 and >= 112.5: // Up Left direction
                    if (next.Angle < 202.5 && next.Angle >= 67.5)
                    {
                        return true;
                    }
                    break;
                case < 202.5 and >= 157.5f: // Left direction
                    if (next.Angle < 247.5 && next.Angle >= 112.5)
                    {
                        return true;
                    }
                    break;
                case < 247.5 and >= 202.5f: // Down Left direction
                    if (next.Angle < 292.5 && next.Angle >= 157.5)
                    {
                        return true;
                    }
                    break;
                case < 292.5 and >= 247.5f: // Down direction
                    if (next.Angle < 337.5 && next.Angle >= 202.5)
                    {
                        return true;
                    }
                    break;
                case < 337.5 and >= 292.5f: // Down Right direction
                    if (next.Angle < 22.5 || next.Angle >= 247.5)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        #endregion

        #endregion
    }
}
