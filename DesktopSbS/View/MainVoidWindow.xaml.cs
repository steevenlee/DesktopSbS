using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using DesktopSbS.Hook;
using DesktopSbS.Interop;
using DesktopSbS.Model;

namespace DesktopSbS.View
{
    /// <summary>
    /// Logique d'interaction pour MainVoidWindow.xaml
    /// </summary>
    public partial class MainVoidWindow : Window
    {
        public MainVoidWindow()
        {
            InitializeComponent();
            this.init();
        }
        private const int renderPassTimeMs = 20;
        private GlobalKeyboardHook keyboardHook;

        private List<WinSbS> windows = new List<WinSbS>();
        private List<WinSbS> tmpWindows = new List<WinSbS>();
 
        private bool MaximizedWindowFound;
        private bool TaskbarWindowFound;

        private bool hasToUpdate = false;

        private bool isAboutOpened = false;

        private CursorSbS cursorSbS;

        private BackgroundWindow backgroundWindow;

        private bool requestAbort = false;


        private bool is3DActive = false;
        public bool Is3DActive
        {
            get
            {
                return this.is3DActive;
            }
            set
            {
                if (this.is3DActive != value)
                {
                    this.is3DActive = value;
                    this.cursorSbS.Is3DActive = value;
                    if (this.is3DActive)
                    {
                        this.backgroundWindow.Show();
                    }
                    else
                    {
                        this.backgroundWindow.Hide();
                    }
                }
            }
        }

        private Thread threadUpdateWindows;

        private void init()
        {
            if (Options.CheckUpdateAtStartup)
            {
                AppUpdater.CheckForUpdates(true, dra =>
                 {
                     bool is3DRunning = this.Is3DActive;
                     if (is3DRunning)
                     {
                         this.Dispatcher.Invoke(() =>
                         {
                             this.Is3DActive = false;
                             this.isAboutOpened = true;
                         });
                     }
                     dra?.Invoke();
                     if (is3DRunning)
                     {
                         this.Dispatcher.Invoke(() =>
                         {
                             this.isAboutOpened = false;
                             if (App.Current != null) this.Is3DActive = true;
                         });
                     }
                 });
            }

            NrealAir.Start();

            if (!Options.HideAboutOnStartup)
            {
                AboutWindow.Instance.ShowDialog();
                if (!AboutWindow.Continue)
                    Application.Current.Shutdown();
            }

            this.keyboardHook = new GlobalKeyboardHook();
            this.keyboardHook.KeyDown += KeyboardHook_KeyDown;

            this.cursorSbS = new CursorSbS();

            this.backgroundWindow = new BackgroundWindow();

            this.Is3DActive = true;

            this.threadUpdateWindows = new Thread(asyncUpdateWindows);
            this.threadUpdateWindows.IsBackground = true;
            this.threadUpdateWindows.Start();

        }

        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ShortcutCommands? shortcutFound = Options.KeyboardShortcuts?.
                FirstOrDefault(t =>
                e.KeyboardDevice.Modifiers == t.Item1
                && key == t.Item2)?.Item3;

            if (shortcutFound.HasValue)
            {
                switch (shortcutFound.Value)
                {
                    case ShortcutCommands.About:
                        if (!this.isAboutOpened)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                AboutWindow.Instance.hideNextTime.IsChecked = false;
                                this.Is3DActive = false;
                                this.isAboutOpened = true;
                                AboutWindow.Instance.ShowDialog();
                                this.isAboutOpened = false;
                                if (App.Current != null) this.Is3DActive = true;
                            });
                        }
                        break;
                    case ShortcutCommands.DecreaseParallax:
                        Options.ParallaxEffect--;
                        this.hasToUpdate = true;
                        break;
                    case ShortcutCommands.IncreaseParallax:
                        Options.ParallaxEffect++;
                        this.hasToUpdate = true;
                        break;
                    case ShortcutCommands.ShutDown:
                        this.requestAbort = true;
                        break;
                    case ShortcutCommands.SwitchSbSMode:
                        Options.ModeSbS = !Options.ModeSbS;
                        this.hasToUpdate = true;
                        break;
                    case ShortcutCommands.Pause3D:
                        this.Is3DActive = !this.Is3DActive;
                        break;
                    case ShortcutCommands.HideDestCursor:
                        Options.HideDestCursor = !Options.HideDestCursor;
                        this.cursorSbS.Is3DActive = this.Is3DActive;
                        break;
                    case ShortcutCommands.KeepRatio:
                        Options.KeepRatio = !Options.KeepRatio;
                        this.hasToUpdate = true;
                        break;
                    case ShortcutCommands.ResetView:
                        NrealAir.Reset();
                        this.hasToUpdate = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void asyncUpdateWindows()
        {
            while (!this.requestAbort)
            {
                /* Head view */
                Euler euler = NrealAir.Euler;
                if (Math.Abs(euler.x - Options.Euler.x) > 5)
                {
                    this.hasToUpdate = true;
                    Options.Euler = euler;
                    Options.ViewRatio = -euler.x / 90.0;

                    // src view
                    int offsetX = (int)(Options.ViewRatio * Options.ScreenSrcBounds.Width);
                    Rectangle rect = Options.ScreenSrcBounds;
                    rect.Offset(offsetX, 0);
                    rect.Intersect(Options.ScreenSrcBounds);
                    Options.ScreenSrcView = rect;
                }

                DateTime start = DateTime.Now;
                this.updateWindows();
                DateTime end = DateTime.Now;

                int elapsedMS = (int)(end - start).TotalMilliseconds;
                // this.Dispatcher.Invoke(()=> DebugWindow.Instance.UpdateMessage($"Elapsed ms: {elapsedMS}"));
                Thread.Sleep(Math.Max(0, renderPassTimeMs - elapsedMS));
            }

            this.Dispatcher.Invoke(App.Current.Shutdown);
        }


        private void updateWindows()
        {
            this.tmpWindows = new List<WinSbS>();
            this.TaskbarWindowFound = false;
            this.MaximizedWindowFound = false;

            if (this.Is3DActive)
            {
               User32.EnumWindows(windowFound, 0);
            }

            int updateAllIndex = -1;

            int offsetLevel = 0;

            WinSbS taskBarWindow = null;

            WinSbS tmpWindow = this.tmpWindows.FirstOrDefault(w => w.Maximized);
            try
            {
                if (tmpWindow != null &&
                    Options.ExcludedApplications?.Contains(Path.GetFileName(User32.GetFilePath(tmpWindow.Handle))) == true)
                {
                    this.tmpWindows.Clear();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e}");
            }


            for (int i = this.tmpWindows.Count - 1; i >= 0; --i)
            {
                tmpWindow = this.tmpWindows[i];

                if (i < this.tmpWindows.Count - 1)
                {
                    tmpWindow.Owner = this.tmpWindows[i + 1];
                }
                else
                {
                    tmpWindow.Owner = null;
                }

                if (tmpWindow.Maximized || tmpWindow.Taskbar)
                {
                    offsetLevel = 0;
                }
                else
                {
                    offsetLevel++;
                }
                tmpWindow.OffsetLevel = offsetLevel;

                int oldIndex = this.windows.FindIndex(w => w.Handle == tmpWindow.Handle);
                if (oldIndex < 0) // New window
                {
                    App.Current.Dispatcher.Invoke(tmpWindow.RegisterThumbs);
                }
                else
                {
                    tmpWindow.CopyThumbInstances(this.windows[oldIndex]);

                    if (updateAllIndex < 0 && this.windows[oldIndex].Owner?.Handle != tmpWindow.Owner?.Handle)
                    {
                        updateAllIndex = i; // Back window changed
                    }
                    else if (!this.windows[oldIndex].SourceRect.Equals(tmpWindow.SourceRect))
                    {
                        tmpWindow.UpdateThumbs(); // Window size changed
                    }
                    this.windows.RemoveAt(oldIndex);

                }

                if (tmpWindow.Taskbar)
                    taskBarWindow = tmpWindow;
            }
            for (int i = 0; i < this.windows.Count; ++i)
            {
                App.Current.Dispatcher.Invoke(this.windows[i].UnRegisterThumbs);
            }

            this.windows = this.tmpWindows;

            if (this.isAboutOpened)
            {
                return;
            }


            if (this.hasToUpdate)
            {
                Options.ComputedVariables.UpdateVariables();
                updateAllIndex = this.windows.Count - 1;
                this.hasToUpdate = false;
            }

            if (updateAllIndex > -1)
            {
                for (int i = updateAllIndex; i >= 0; --i)
                {
                    if (this.windows[i] != taskBarWindow) this.windows[i].UpdateThumbs();
                }
                taskBarWindow?.UpdateThumbs(true);
            }

            this.cursorSbS.UpdateThumbs((this.windows.Any() ? this.windows.Max(w => w.OffsetLevel) : 0) + 1);
        }

        private bool windowFound(IntPtr hwnd, int lParam)
        {
            WS winStyle = (WS)User32.GetWindowLongA(hwnd, User32.GWL_STYLE);
            if ((winStyle & WS.WS_VISIBLE) != WS.WS_VISIBLE
                || (winStyle & WS.WS_ICONIC) != 0
                || (winStyle & WS.WS_DISABLED) != 0)
                return true;

            RECT rect = new RECT();
            User32.GetWindowRect(hwnd, ref rect);
            if (rect.IsSize0())
                return true;
            // Skip windows that not on source view
            Rectangle rectangle = rect.toRectangle();
            if (!Options.ScreenSrcView.IntersectsWith(rectangle))
                return true;

            // Detection of cloaked win10 windows => not rendered 
            int cloaked = 0;
            DwmApi.DwmGetWindowAttribute(hwnd, DwmApi.DwmWindowAttribute.DWMWA_CLOAKED, out cloaked, sizeof(int));
            if (cloaked != 0)
                return true;

            StringBuilder sb = new StringBuilder(100);
            User32.GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString();
            // don't consider our own windows 
            if (title == "ThumbWindows")
                return true;

            bool isTaskbar = rectangle.Equals(Options.ScreenSrcTaskbar);
            if (!this.MaximizedWindowFound || isTaskbar)
            {
                WinSbS win = new WinSbS(hwnd);
                win.SourceRect = rectangle;
                win.Title = title;
                win.Taskbar = isTaskbar;
                win.Maximized = (winStyle & WS.WS_MAXIMIZE) > 0;
                if (win.Maximized)
                    this.MaximizedWindowFound = true;
                if (isTaskbar)
                    this.TaskbarWindowFound = true;
                this.tmpWindows.Add(win);
            }

            if (this.MaximizedWindowFound && this.TaskbarWindowFound)
                // Stop enumeration
                return false;

            return true; //continue enumeration
        }

        protected override void OnClosed(EventArgs e)
        {

            base.OnClosed(e);
            Options.Save();
            this.Is3DActive = false;

        }
    }

}
