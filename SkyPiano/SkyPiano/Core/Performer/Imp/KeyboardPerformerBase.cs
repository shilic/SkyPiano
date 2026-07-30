using SkyPiano.SkyPiano.Core.Performer.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Devices;

namespace SkyPiano.SkyPiano.Core.Performer.Imp {
    public abstract class KeyboardPerformerBase : IPerformer {
        public abstract string performerName {  get; }
        /// <summary>
        /// 键盘按下
        /// </summary>
        /// <param name="key"></param>
        protected abstract void KeyPress(string key);
        /// <summary>
        /// 键盘抬起
        /// </summary>
        /// <param name="key"></param>
        protected abstract void KeyRelease(string key);
        /// <summary>
        /// 键盘点击
        /// </summary>
        /// <param name="key"></param>
        protected virtual void KeyClick(string key) {
            KeyPress(key);
            KeyRelease(key);
        }

        /// <summary>  哆  </summary>
        public virtual void duo() {
            KeyClick("A");
        }
        /// <summary>  来  </summary>
        public virtual void re() {
            KeyClick("S");
        }
        /// <summary>  咪  </summary>
        public virtual void mi() {
            KeyClick("D");
        }
        /// <summary>  发  </summary>
        public virtual void fa() {
            KeyClick("F");
        }
        /// <summary>  嗦  </summary>
        public virtual void so() {
            KeyClick("G");
        }
        /// <summary>  啦  </summary>
        public virtual void la() {
            KeyClick("H");
        }
        /// <summary>  吸  </summary>
        public virtual void xi() {
            KeyClick("J");
        }
        /// <summary>  哆Up  </summary>
        public virtual void duoUp() {
            KeyRelease("Q");
        }
        /// <summary>  来Up  </summary>
        public virtual void reUp() {
            KeyRelease("W");
        }
        /// <summary>  咪Up  </summary>
        public virtual void miUp() {
            KeyRelease("E");
        }
        /// <summary>  发Up  </summary>
        public virtual void faUp() {
            KeyRelease("R");
        }
        /// <summary>  嗦Up  </summary>
        public virtual void soUp() {
            KeyRelease("T");
        }
        /// <summary>  啦Up  </summary>
        public virtual void laUp() {
            KeyRelease("Y");
        }
        /// <summary>  吸Up  </summary>
        public virtual void xiUp() {
            KeyRelease("U");
        }
        /// <summary>  哆Down  </summary>
        public virtual void duoDown() {
            KeyPress("Z");
        }
        /// <summary>  来Down  </summary>
        public virtual void reDown() {
            KeyPress("X");
        }
        /// <summary>  咪Down  </summary>
        public virtual void miDown() {
            KeyPress("C");
        }
        /// <summary>  发Down  </summary>
        public virtual void faDown() {
            KeyRelease("V");
        }
        /// <summary>  嗦Down  </summary>
        public virtual void soDown() {
            KeyRelease("B");
        }
        /// <summary>  啦Down  </summary>
        public virtual void laDown() {
            KeyRelease("N");
        }
        /// <summary>  吸Down  </summary>
        public virtual void xiDown() {
            KeyRelease("M");
        }
    }
}
