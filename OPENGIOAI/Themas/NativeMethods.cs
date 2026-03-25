using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Themas
{
    public static class NativeMethods
    {
        public const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
    }

}
