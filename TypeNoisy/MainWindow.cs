using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TypeNoisy
{
    public partial class MainWindow : Form
    {
        const string MCI_CHANNEL = "TYPE_NOISY_";

        const string TEXT_RESUME = "R&esume";
        const string TEXT_PAUSE = "P&ause";
        const string TEXT_EXIT = "E&xit";


        Dictionary<uint, bool> keyStatus;
        IntPtr hookId;
        ContextMenu menu;
        MenuItem pauseMenuItem;

        int mciOrder = 0;
        bool paused = false;

        public MainWindow()
        {
            InitializeComponent();

            Application.ApplicationExit += (s, o) =>
            {
                InterceptKeys.DeleteHook(hookId);
            };

            keyStatus = new Dictionary<uint, bool>();
            hookId = InterceptKeys.CreateHook(KeyDownCallback, KeyUpCallback);

            InitWindow();
        }

        void InitWindow()
        {
            InitMenu();

            notifyIcon1.ContextMenu = menu;
            notifyIcon1.DoubleClick += Pause;
            notifyIcon1.Visible = true;

            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        void InitMenu()
        {
            menu = new ContextMenu();

            pauseMenuItem = new MenuItem
            {
                Index = 1,
                Text = TEXT_PAUSE
            };

            var exitItem = new MenuItem
            {
                Index = 2,
                Text = TEXT_EXIT
            };


            pauseMenuItem.Click += new System.EventHandler(Pause);
            exitItem.Click += new EventHandler(Exit);

            menu.MenuItems.AddRange(new MenuItem[] { pauseMenuItem, exitItem });
        }

        void Exit(object sender, object args)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        void Pause(object sender, object args)
        {
            paused = !paused;

            if (paused)
            {
                keyStatus.Clear();
                pauseMenuItem.Text = TEXT_RESUME;
            }
            else
            {
                pauseMenuItem.Text = TEXT_PAUSE;
            }
        }   

        string GetSoundName(uint code)
        {
            var sb = new StringBuilder(512);

            var file = "other";

            if (code == (int)Keys.Enter) file = "return";
            else if (code == (int)Keys.Space) file = "space";
            else if (code == (int)Keys.Left ||
                     code == (int)Keys.Right ||
                     code == (int)Keys.Up ||
                     code == (int)Keys.Down) file = "arrow";
            else if (code == (int)Keys.Back) file = "backspce";
            else if (code == (int)Keys.Tab) file = "tab";

            //Gets short name to make it work with long filepaths
            NativeMethods.GetShortPathName(Path.Combine(Environment.CurrentDirectory, "sounds", $"{file}.wav"), sb, sb.Capacity);

            return sb.ToString();
        }

        void KeyDownCallback(uint code)
        {
            if (!paused)
            {
                keyStatus.TryGetValue(code, out bool status);

                if (!status)
                {
                    var name = MCI_CHANNEL + (++mciOrder);
                    keyStatus[code] = true;

                    new Task(() =>
                    {
                        SendMciCommand($"open {GetSoundName(code)} alias {name}");
                        SendMciCommand($"play {name} wait");
                        SendMciCommand($"close applause  ${name}");
                    }).Start();
                }
            }
        }

        public void SendMciCommand(string command)
        {
            NativeMethods.mciSendString(command, null, 0, IntPtr.Zero);
        }

        void KeyUpCallback(uint code)
        {
            if (!paused)
            {
                keyStatus[code] = false;
            }
        }
    }
}
