using System;
using System.Collections.Generic;
using System.ComponentModel;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPiano.SkyPiano.Core.MusicTheory.Base{
    /* 为什么不使用键值对，或者直接使用字符串来表示音符？
     * 拜托，如果使用键值对和字符串，程序可移植性和可读性将会大幅下滑，难以维护。
     * 最好的办法是使用枚举将钢琴全部按键抽象出来，但是原神只有21个按键，所以只需要抽象21的按键。
     */
    /// <summary>
    /// 音符 <br></br>
    /// 音符的实际整形值，应当按照MIDI里边的规则进行填写，但是我现在不清楚，先空着。 <br></br>
    /// </summary>
    public enum Note : Int32 {
        /// <summary>  哆  </summary>
        [Description("A")]
        C4 = 60,
        Duo,
        [Description("S")]
        Re,
        [Description("D")]
        Mi,
        [Description("F")]
        Fa,
        [Description("G")]
        So,
        [Description("H")]
        La,
        [Description("J")]
        Xi,

        [Description("Q")]
        DuoUp,
        [Description("W")]
        ReUp,
        [Description("E")]
        MiUp,
        [Description("R")]
        FaUp,
        [Description("T")]
        SoUp,
        [Description("Y")]
        LaUp,
        [Description("U")]
        XiUp,

        [Description("Z")]
        DuoDown,
        [Description("X")]
        ReDown,
        [Description("C")]
        MiDown,
        [Description("V")]
        FaDown,
        [Description("B")]
        SoDown,
        [Description("N")]
        LaDown,
        [Description("M")]
        XiDown,
    }
}