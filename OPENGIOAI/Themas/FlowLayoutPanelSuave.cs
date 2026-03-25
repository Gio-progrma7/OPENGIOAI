using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// FlowLayoutPanel con Double Buffering activado para evitar 
    /// el parpadeo al hacer scroll con muchos controles.
    /// </summary>
    public class FlowLayoutPanelSuave : FlowLayoutPanel
    {
        public FlowLayoutPanelSuave()
        {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true
            );
            this.DoubleBuffered = true;
            this.UpdateStyles();
        }
    }

    /// <summary>
    /// Panel con Double Buffering para evitar parpadeo en los paneles de API.
    /// </summary>
    public class PanelSuave : Panel
    {
        public PanelSuave()
        {
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true
            );
            this.DoubleBuffered = true;
            this.UpdateStyles();
        }
    }
}
