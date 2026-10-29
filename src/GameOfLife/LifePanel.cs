using System.Drawing;
using System.Windows.Forms;

namespace GameOfLife;

/// <summary>A double-buffered panel that repaints when resized, so the
/// grid does not flicker while the simulation runs.</summary>
internal sealed class LifePanel : Panel
{
    public LifePanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = SystemColors.Window;
    }
}
