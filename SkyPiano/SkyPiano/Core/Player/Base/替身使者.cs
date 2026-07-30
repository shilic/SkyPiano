using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPiano.Core.Player.Base {
    /// <summary>
    /// 替身使者; <br></br>
    /// 1.咋瓦鲁多：暂停播放。 <br></br>
    /// 2.男人领域：上一首。 <br></br>
    /// 3.败者食尘：快退。 <br></br>
    /// 4.天堂制造：快进。 <br></br>
    /// 5.墓志铭：下一首。 <br></br>
    /// 6.恶行易施：切换播放列表。 <br></br>
    /// </summary>
    public interface 替身使者 {
        /// <summary>  1.咋瓦鲁多：暂停播放。  </summary>
        void 咋瓦鲁多();
        /// <summary>  2.男人领域：上一首。  </summary>
        void 男人领域();
        /// <summary>  3.败者食尘：快退。  </summary>
        void 败者食尘();
        /// <summary>  4.天堂制造：快进。  </summary>
        void 天堂制造();
        /// <summary>  5.墓志铭：下一首。  </summary>  
        void 墓志铭();
        /// <summary>  6.恶行易施：切换播放列表。  </summary>
        void 恶行易施(string name);
    }
}
