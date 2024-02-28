using System.Collections.Generic;
using Score2Stream.Commons.Enums;

namespace Score2Stream.ScoreboardService.Extensions
{
    internal static class TypeExtensions
    {
        #region Public Methods

        public static IEnumerable<ClipType> GetClipTypes(this AreaType areaType)
        {
            switch (areaType)
            {
                case AreaType.ClockGameMins:

                    yield return ClipType.ClockGameMin1;
                    yield return ClipType.ClockGameMin2;
                    break;

                case AreaType.ClockGameMin1:

                    yield return ClipType.ClockGameMin1;
                    break;

                case AreaType.ClockGameMin2:

                    yield return ClipType.ClockGameMin2;
                    break;

                case AreaType.ClockGameSecs:

                    yield return ClipType.ClockGameSec1;
                    yield return ClipType.ClockGameSec2;
                    break;

                case AreaType.ClockGameSec1:

                    yield return ClipType.ClockGameSec1;
                    break;

                case AreaType.ClockGameSec2:

                    yield return ClipType.ClockGameSec2;
                    break;

                case AreaType.ClockGameSplit:

                    yield return ClipType.ClockGameSplit;
                    break;

                case AreaType.ClockShot:

                    yield return ClipType.ClockShot1;
                    yield return ClipType.ClockShot2;
                    break;

                case AreaType.ClockShot1:

                    yield return ClipType.ClockShot1;
                    break;

                case AreaType.ClockShot2:

                    yield return ClipType.ClockShot2;
                    break;

                case AreaType.FoulHome:

                    yield return ClipType.FoulHome;
                    break;

                case AreaType.FoulGuest:

                    yield return ClipType.FoulGuest;
                    break;

                case AreaType.Period:

                    yield return ClipType.Period;
                    break;

                case AreaType.ScoreHomeTriple:

                    yield return ClipType.ScoreHome1;
                    yield return ClipType.ScoreHome2;
                    yield return ClipType.ScoreHome3;
                    break;

                case AreaType.ScoreHomeDouble:

                    yield return ClipType.ScoreHome1;
                    yield return ClipType.ScoreHome2;
                    break;

                case AreaType.ScoreHome1:

                    yield return ClipType.ScoreHome1;
                    break;

                case AreaType.ScoreHome2:

                    yield return ClipType.ScoreHome2;
                    break;

                case AreaType.ScoreHome3:

                    yield return ClipType.ScoreHome3;
                    break;

                case AreaType.ScoreGuestTriple:

                    yield return ClipType.ScoreGuest1;
                    yield return ClipType.ScoreGuest2;
                    yield return ClipType.ScoreGuest3;
                    break;

                case AreaType.ScoreGuestDouble:

                    yield return ClipType.ScoreGuest1;
                    yield return ClipType.ScoreGuest2;
                    break;

                case AreaType.ScoreGuest1:

                    yield return ClipType.ScoreGuest1;
                    break;

                case AreaType.ScoreGuest2:

                    yield return ClipType.ScoreGuest2;
                    break;

                case AreaType.ScoreGuest3:

                    yield return ClipType.ScoreGuest3;
                    break;
            }
        }

        #endregion Public Methods
    }
}