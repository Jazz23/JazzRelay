using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JazzRelay
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        public Dictionary<Panel, MultiPanel> Panels { get; set; } = new(); //Last panel is main panel
        Dictionary<Panel, (float, float)> _sizeMap = new();
        Dictionary<Panel, (float, float)> _locMap = new();

        public Form1()
        {
            InitializeComponent();
            LoadPanels();
        }

        void LoadPanels()
        {
            foreach (var control in Controls)
            {
                if (control.GetType() == typeof(Panel))
                {
                    var panel = (Panel)control;
                    _sizeMap.Add(panel, ((float)panel.Width / ClientSize.Width, (float)panel.Height / ClientSize.Height));
                    _locMap.Add(panel, ((float)panel.Location.X / ClientSize.Width, (float)panel.Location.Y / ClientSize.Height));

                    Panels.Add(panel, new MultiPanel());
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false; //Bite me
        }

        private void Panel_Resize(object sender, EventArgs e)
        {
            IntPtr handle = Panels[(Panel)sender].ExaltHandle;
            if (handle != default)
                MoveWindow(handle, 0, 0, ((Panel)sender).Width, panel1.Height, true);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            foreach (var panel in _sizeMap)
            {
                panel.Key.Width = (int)(panel.Value.Item1 * ClientSize.Width);
                panel.Key.Height = (int)(panel.Value.Item2 * ClientSize.Height);
            }
            foreach (var loc in _locMap)
            {
                loc.Key.Location = new Point((int)(loc.Value.Item1 * ClientSize.Width), (int)(loc.Value.Item2 * ClientSize.Height));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var meme = ClientSize;
        }
    }
}
