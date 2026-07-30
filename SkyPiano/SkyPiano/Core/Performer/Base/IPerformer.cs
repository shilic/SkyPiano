using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPiano.SkyPiano.Core.Performer.Base {
    /// <summary>
    /// 演奏者抽象, 由不同的演奏者实现不同的演奏方式
    /// </summary>
    public interface IPerformer {
        /// <summary>  演奏者名称  </summary>
        string performerName { get; }
        /// <summary>  哆  </summary>
        void duo();
        /// <summary>  来  </summary>
        void re();
        /// <summary>  咪  </summary>
        void mi();
        /// <summary>  发  </summary>
        void fa();
        /// <summary>  嗦  </summary>
        void so();
        /// <summary>  啦  </summary>
        void la();
        /// <summary>  吸  </summary>
        void xi();

        /// <summary>  哆Up  </summary>
        void duoUp();
        /// <summary>  来Up  </summary>
        void reUp();
        /// <summary>  咪Up  </summary>
        void miUp();
        /// <summary>  发Up  </summary>
        void faUp();
        /// <summary>  嗦Up  </summary>
        void soUp();
        /// <summary>  啦Up  </summary>
        void laUp();
        /// <summary>  吸Up  </summary>
        void xiUp();

        /// <summary>  哆Down  </summary>
        void duoDown();
        /// <summary>  来Down  </summary>
        void reDown();
        /// <summary>  咪Down  </summary>
        void miDown();
        /// <summary>  发Down  </summary>
        void faDown();
        /// <summary>  嗦Down  </summary>
        void soDown();
        /// <summary>  啦Down  </summary>
        void laDown();
        /// <summary>  吸Down  </summary>
        void xiDown();
    }
}
