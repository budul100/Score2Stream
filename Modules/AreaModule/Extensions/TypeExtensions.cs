using System.Collections.Generic;
using Score2Stream.Commons.Enums;

namespace Score2Stream.AreaModule.Extensions
{
    internal static class TypeExtensions
    {
        #region Public Methods

        public static IEnumerable<AreaType> GetAreaTypes(this int size)
        {
            switch (size)
            {
                case 1:

                    yield return AreaType.Period;

                    yield return AreaType.ClockGameMin1;
                    yield return AreaType.ClockGameMin2;

                    yield return AreaType.ClockGameSec1;
                    yield return AreaType.ClockGameSec2;

                    yield return AreaType.ClockGameSplit;

                    yield return AreaType.ClockShot1;
                    yield return AreaType.ClockShot2;

                    yield return AreaType.ScoreHome1;
                    yield return AreaType.ScoreHome2;
                    yield return AreaType.ScoreHome3;

                    yield return AreaType.ScoreGuest1;
                    yield return AreaType.ScoreGuest2;
                    yield return AreaType.ScoreGuest3;

                    yield return AreaType.FoulHome;
                    yield return AreaType.FoulGuest;

                    break;

                case 2:

                    yield return AreaType.ClockGameMins;
                    yield return AreaType.ClockGameSecs;

                    yield return AreaType.ClockShot;

                    yield return AreaType.ScoreHomeDouble;
                    yield return AreaType.ScoreGuestDouble;

                    break;

                case 3:

                    yield return AreaType.ScoreHomeTriple;
                    yield return AreaType.ScoreGuestTriple;

                    break;
            }
        }

        #endregion Public Methods
    }
}