using System.Runtime.InteropServices;
using SkyPiano.Core.MusicTheory;
using SkyPiano.Core.Performer.Base;

namespace SkyPiano.Core.Performer.Imp;

/// <summary>
/// 鼠标点击模拟器，实现 <see cref="IPerformer"/> 接口。
/// 将 21 键音符映射到屏幕坐标，通过 Win32 API 移动鼠标并模拟左键点击。
///
/// <para>钢琴布局：3 行 × 7 列（高音 + 中音 + 低音），在目标区域中均匀分布。</para>
/// </summary>
/// <remarks>
/// 构造 MouseSimulator。
/// </remarks>
/// <param name="areaX">游戏区域内钢琴左上角 X 坐标。</param>
/// <param name="areaY">游戏区域内钢琴左上角 Y 坐标。</param>
/// <param name="areaWidth">钢琴区域总宽度。</param>
/// <param name="areaHeight">钢琴区域总高度。</param>
public class MouseSimulator(int areaX, int areaY, int areaWidth, int areaHeight) : IPerformer {
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    /// <summary>21 键 → 屏幕坐标的缓存。</summary>
    private readonly Dictionary<MyNote, (int x, int y)> _positions = BuildPositions(areaX, areaY, areaWidth, areaHeight);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    /// <summary>当前处于按下状态的键集合（用于避免重复移动）。</summary>
    private readonly HashSet<MyNote> _pressed = [];

    /// <summary>
    /// 根据目标区域计算 21 键的屏幕坐标。
    /// 3 行（上中下）× 7 列（C~B），每个键居中放置。
    /// </summary>
    private static Dictionary<MyNote, (int, int)> BuildPositions(int x, int y, int w, int h) {
        const int cols = 7;
        const int rows = 3;
        float keyW = (float)w / cols;
        float keyH = (float)h / rows;

        var map = new Dictionary<MyNote, (int, int)>();
        // 按枚举顺序排列：低音 → 中音 → 高音，每行 7 个
        var notes = (MyNote[])Enum.GetValues(typeof(MyNote));

        for (int i = 0; i < 21; i++) {
            int row = 2 - i / cols;    // 0=低音行(底部), 1=中音行, 2=高音行(顶部)
            int col = i % cols;
            int cx = x + (int)((col + 0.5f) * keyW);   // 键中心 X
            int cy = y + (int)((row + 0.5f) * keyH);   // 键中心 Y
            map[notes[i]] = (cx, cy);
        }
        return map;
    }

    /// <inheritdoc />
    public void KeyPress(MyNote note) {
        if (_pressed.Add(note)) {
            var (cx, cy) = _positions[note];
            SetCursorPos(cx, cy);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        }
    }

    /// <inheritdoc />
    public void KeyRelease(MyNote note) {
        if (_pressed.Remove(note)) {
            var (cx, cy) = _positions[note];
            SetCursorPos(cx, cy);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }
}
