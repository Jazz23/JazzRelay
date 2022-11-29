using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay
{
    public class MultiPanel
    {
        public bool HasExalt { get => ExaltHandle != default; }
        public IntPtr ExaltHandle { get; set; } = default;
        public IntPtr ParentHandle { get; set; } = default;
        public Panel Panel { get; set; }

        public MultiPanel(Panel panel) => Panel = panel;
    }
}
