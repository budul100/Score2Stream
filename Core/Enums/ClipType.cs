using System.ComponentModel;

namespace Core.Enums
{
    public enum ClipType
    {
        [Description("[Clip]")]
        None,

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

        [Description("Game period")]
        Period,

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