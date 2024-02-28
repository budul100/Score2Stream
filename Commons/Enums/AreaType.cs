using System.ComponentModel;

namespace Score2Stream.Commons.Enums
{
    public enum AreaType
    {
        [Description("[Area]")]
        None,

        [Description("Period")]
        Period,

        [Description("Game mins")]
        ClockGameMins,

        [Description("Game mins X_")]
        ClockGameMin1,

        [Description("Game mins _X")]
        ClockGameMin2,

        [Description("Game secs")]
        ClockGameSecs,

        [Description("Game secs X_")]
        ClockGameSec1,

        [Description("Game secs _X")]
        ClockGameSec2,

        [Description("Clock splitter")]
        ClockGameSplit,

        [Description("Shot secs")]
        ClockShot,

        [Description("Shot secs X_")]
        ClockShot1,

        [Description("Shot secs _X")]
        ClockShot2,

        [Description("Score home")]
        ScoreHomeTriple,

        [Description("Score home")]
        ScoreHomeDouble,

        [Description("Score home X__")]
        ScoreHome1,

        [Description("Score home _X_")]
        ScoreHome2,

        [Description("Score home __X")]
        ScoreHome3,

        [Description("Score guest")]
        ScoreGuestTriple,

        [Description("Score guest")]
        ScoreGuestDouble,

        [Description("Score guest X__")]
        ScoreGuest1,

        [Description("Score guest _X_")]
        ScoreGuest2,

        [Description("Score guest __X")]
        ScoreGuest3,

        [Description("Fouls home")]
        FoulHome,

        [Description("Fouls guest")]
        FoulGuest
    }
}