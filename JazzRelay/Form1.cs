using JazzRelay.Enums;
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

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);
        const int GWL_STYLE = (-16);
        const UInt32 WS_VISIBLE = 0x10000000;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;

        Dictionary<Panel, (float, float)> _sizeMap = new();
        Dictionary<Panel, (float, float)> _locMap = new();
        public List<MultiPanel> Panels { get; set; } = new(); //Last panel is main panel
        public MultiPanel MainPanel { get => Panels.Last(); }

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

                    Panels.Add(new MultiPanel(panel));
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false; //Bite me
            //DockExistingExalts();
        }

        public void PressGlobalKey(string key)
        {
            foreach (var panel in Panels)
            {
                PressKey(panel, key);
            }
        }

        public void PressKey(MultiPanel panel, string key)
        {
            if (panel.ExaltHandle != default)
            {
                SetForegroundWindow(panel.ExaltHandle);
                SendKeys.SendWait(key);
                SendKeys.Flush();
            }
        }

        public void SetPanel(MultiPanel mpanel, Panel panel)
        {
            //ShowWindow(mpanel.ExaltHandle, 5);
            var parent = SetParent(mpanel.ExaltHandle, panel.Handle);
            if (mpanel.ParentHandle == default) mpanel.ParentHandle = parent;
            MoveWindow(mpanel.ExaltHandle, -7, -31, panel.Width + 15, panel.Height + 39, true);
        }

        public MultiPanel GrabNewPanel() => Panels.FirstOrDefault(x => x.HasExalt == false) ?? Panels.First();

        public void SwapPanels(MultiPanel panel1, MultiPanel panel2)
        {
            int index1 = Panels.IndexOf(panel1);
            int index2 = Panels.IndexOf(panel2);
            if (index1 == -1 || index2 == -1) throw new Exception("Error swapping panels!");
            Panels[index1] = panel2;
            Panels[index2] = panel1;
            var temp1 = panel1.Panel;
            panel1.Panel = panel2.Panel;
            panel2.Panel = temp1;
            SetPanel(panel1, panel1.Panel);
            SetPanel(panel2, panel2.Panel);
        }

        //void DockExistingExalts()
        //{
        //    var procs = Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains("exalt")).ToArray();
        //    int length = Panels.Count() < procs.Count() ? Panels.Count() : procs.Count();
        //    for (int i = 0; i < length; i++)
        //    {
        //        SetPanel(Panels.ElementAt(Panels.Count() - i-1).Key, procs[i].MainWindowHandle);
        //    }
        //}

        MultiPanel FindMPanel(Panel panel) => Panels.First(x => x.Panel == panel);

        private void Panel_Resize(object sender, EventArgs e)
        {
            IntPtr handle = FindMPanel((Panel)sender).ExaltHandle;
            if (handle != default)
                MoveWindow(handle, 0, 0, ((Panel)sender).Width, panel1.Height, true);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
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

        ~Form1() //idfk
        {
            DetachExalts();
        }

        void DetachExalts()
        {
            int offset = 0;
            foreach (var panel in Panels)
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

        private void Form1_Enter(object sender, EventArgs e)
        {

        }

        public void FocusPanel(MultiPanel panel) => SetForegroundWindow(panel.ExaltHandle);
    }
}
