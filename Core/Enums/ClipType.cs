using System.ComponentModel;

namespace Score2Stream.Core.Enums
{
    public enum ClipType
    {
        [Description("[Clip]")]
        None,

        [Description("Period")]
        Period,

        [Description("Clock min X_")]
        ClockGameMin1,

        [Description("Clock min _X")]
        ClockGameMin2,

        [Description("Clock sec X_")]
        ClockGameSec1,

        [Description("Clock sec _X")]
        ClockGameSec2,

        [Description("Clock splitter")]
        ClockGameSplit,

        [Description("Shot 1")]
        ClockShot1,

        [Description("Shot 2")]
        ClockShot2,

        [Description("Score home X__")]
        ScoreHome1,

        [Description("Score home _X_")]
        ScoreHome2,

        [Description("Score home __X")]
        ScoreHome3,

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