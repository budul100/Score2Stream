using System.ComponentModel;

namespace Score2Stream.Core.Enums
{
    public enum ClipType
    {
        [Description("[Clip]")]
        None,

        [Description("Period")]
        Period,

        [Description("Clock min1")]
        ClockGameMin1,

        [Description("Clock min2")]
        ClockGameMin2,

        [Description("Clock sec1")]
        ClockGameSec1,

        [Description("Clock sec2")]
        ClockGameSec2,

        [Description("Clock splitter")]
        ClockGameSplit,

        [Description("Shot1")]
        ClockShot1,

        [Description("Shot2")]
        ClockShot2,

        [Description("Shot splitter")]
        ClockShotSplit,

        [Description("Score home")]
        ScoreHome,

        [Description("Score guest")]
        ScoreGuest,

        [Description("Fouls home")]
        FoulHome,

        [Description("Fouls guest")]
        FoulGuest
    }
}