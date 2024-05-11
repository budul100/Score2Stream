using System.Collections.Generic;
using Score2Stream.Commons.Enums;

namespace Score2Stream.ScoreboardService.Extensions
{
    internal static class TypeExtensions
    {
        #region Public Methods

        public static IEnumerable<SegmentType> GetClipTypes(this AreaType areaType)
        {
            switch (areaType)
            {
                case AreaType.ClockGameMins:

                    yield return SegmentType.ClockGameMin1;
                    yield return SegmentType.ClockGameMin2;
                    break;

                case AreaType.ClockGameMin1:

                    yield return SegmentType.ClockGameMin1;
                    break;

                case AreaType.ClockGameMin2:

                    yield return SegmentType.ClockGameMin2;
                    break;

                case AreaType.ClockGameSecs:

                    yield return SegmentType.ClockGameSec1;
                    yield return SegmentType.ClockGameSec2;
                    break;

                case AreaType.ClockGameSec1:

                    yield return SegmentType.ClockGameSec1;
                    break;

                case AreaType.ClockGameSec2:

                    yield return SegmentType.ClockGameSec2;
                    break;

                case AreaType.ClockGameSplit:

                    yield return SegmentType.ClockGameSplit;
                    break;

                case AreaType.ClockShot:

                    yield return SegmentType.ClockShot1;
                    yield return SegmentType.ClockShot2;
                    break;

                case AreaType.ClockShot1:

                    yield return SegmentType.ClockShot1;
                    break;

                case AreaType.ClockShot2:

                    yield return SegmentType.ClockShot2;
                    break;

                case AreaType.FoulHome:

                    yield return SegmentType.FoulHome;
                    break;

                case AreaType.FoulGuest:

                    yield return SegmentType.FoulGuest;
                    break;

                case AreaType.Period:

                    yield return SegmentType.Period;
                    break;

                case AreaType.ScoreHomeTriple:

                    yield return SegmentType.ScoreHome1;
                    yield return SegmentType.ScoreHome2;
                    yield return SegmentType.ScoreHome3;
                    break;

                case AreaType.ScoreHomeDouble:

                    yield return SegmentType.ScoreHome1;
                    yield return SegmentType.ScoreHome2;
                    break;

                case AreaType.ScoreHome1:

                    yield return SegmentType.ScoreHome1;
                    break;

                case AreaType.ScoreHome2:

                    yield return SegmentType.ScoreHome2;
                    break;

                case AreaType.ScoreHome3:

                    yield return SegmentType.ScoreHome3;
                    break;

                case AreaType.ScoreGuestTriple:

                    yield return SegmentType.ScoreGuest1;
                    yield return SegmentType.ScoreGuest2;
                    yield return SegmentType.ScoreGuest3;
                    break;

                case AreaType.ScoreGuestDouble:

                    yield return SegmentType.ScoreGuest1;
                    yield return SegmentType.ScoreGuest2;
                    break;

                case AreaType.ScoreGuest1:

                    yield return SegmentType.ScoreGuest1;
                    break;

                case AreaType.ScoreGuest2:

                    yield return SegmentType.ScoreGuest2;
                    break;

                case AreaType.ScoreGuest3:

                    yield return SegmentType.ScoreGuest3;
                    break;
            }
        }

        #endregion Public Methods
    }
}