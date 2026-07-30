using SkyPiano.SkyPiano.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPiano.SkyPiano.Core.MusicTheory.Base{
    public static class NoteTool {
        

        /// <summary> 通过MIDI值实例化音符 </summary> 
        public static Note toNote(this Int32 midiValue) {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 通过键盘值，实例化音符
        /// </summary>
        /// <param name="key">键盘值</param>
        /// <returns></returns>
        public static Note toNote(this String key) {
            foreach (Note value in Enum.GetValues<Note>()) {
                string desc = value.getDescription();
                if (string.Equals(desc, key, StringComparison.OrdinalIgnoreCase))
                    return value;
            }
            throw new Exception($"键盘按键: '{key}' 无法转换为音符");
        }
    }
}
