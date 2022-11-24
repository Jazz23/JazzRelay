using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
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
            //DockExistingExalts();
        }

        public void SetPanel(Panel panel, IntPtr handle)
        {
            MultiPanel? mpanel;
            if (!Panels.TryGetValue(panel, out mpanel)) return;
            mpanel.ExaltHandle = handle;
            mpanel.HasExalt = true;
            ShowWindow(handle, 5);
            mpanel.ParentHandle = SetParent(handle, panel.Handle);
            MoveWindow(handle, -7, -31, panel.Width + 15, panel.Height + 39, true);
        }

        void DockExistingExalts()
        {
            var procs = Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains("exalt")).ToArray();
            int length = Panels.Count() < procs.Count() ? Panels.Count() : procs.Count();
            for (int i = 0; i < length; i++)
            {
                SetPanel(Panels.ElementAt(Panels.Count() - i-1).Key, procs[i].MainWindowHandle);
            }
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

        ~Form1() //idfk
        {
            DetachExalts();
        }

        void DetachExalts()
        {
            int offset = 0;
            foreach (var panel in Panels.Values)
            {
                if (panel.ParentHandle == default) continue;
                SetParent(panel.ExaltHandle, panel.ParentHandle);
                MoveWindow(panel.ExaltHandle, offset, offset, 800, 600, true);
                offset += 15;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DetachExalts();
        }
    }
}
