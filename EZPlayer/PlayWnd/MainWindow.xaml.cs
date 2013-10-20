using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EZPlayer.Common;
using EZPlayer.View;
using EZPlayer.ViewModel;
using Vlc.DotNet.Core;
using Microsoft.Win32;
using EZPlayer.History;
using System.Diagnostics;
using Org.Mentalis.Utilities;

namespace EZPlayer
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer m_activityTimer;

        /// <summary>
        /// Delay the single click so as to solve the issue
        /// that a double click will trigger two single clicks.
        /// </summary>
        private DispatcherTimer m_delaySingleClickTimer;

        private MainWndViewModel m_viewModel = null;

        public MainWindow()
        {
            InitializeComponent();            

            SetupUserDataDir();

            SetupViewModel();

            SetupPlayWndVideoSource();

            SetupHookSpaceKeyInput();

            SetupMouseWheelActions();

            SetupWindowClosingActions();

            SetupMouseLeftClickActions();

            SetupMouseMoveActions();

            SetupAutoHideConsoleTimer();

            SetupDelaySingleClickTimer();

            ProcessCommandLineArgs();
        }

        private void ProcessCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Count() >= 2)
            {
                var playList = m_viewModel.GenerateFileList(args.Skip(1).ToList());
                if (playList.Count == 0)
                {
                    return;
                }
                /// it seems vlc requires some time to init.
                new DelayTask(TimeSpan.FromMilliseconds(500),
                    () => { m_viewModel.PlayAListOfFiles(playList); }
                    );
            }
        }

        private void SetupUserDataDir()
        {
            if (!Directory.Exists(AppDataDir.EZPLAYER_DATA_DIR))
            {
                Directory.CreateDirectory(AppDataDir.EZPLAYER_DATA_DIR);
            }
        }

        private void SetupViewModel()
        {
            m_viewModel = this.DataContext as MainWndViewModel;

            m_viewModel.Init();
        }

        private void SetupMouseWheelActions()
        {
            this.MouseWheel += new MouseWheelEventHandler(AdjustVolume4MouseWheel);
        }

        private void SetupWindowClosingActions()
        {
            this.Closing += (sender, arg) => SaveLastPlayInfo();
            this.Closing += (sender, arg) => VlcContext.CloseAll();
            SystemEvents.SessionEnding += (sender, arg) => SaveLastPlayInfo();
        }

        private void SetupMouseLeftClickActions()
        {
            this.m_gridPlayWnd.MouseLeftButtonDown += OnMouseClick;
        }

        private void SetupMouseMoveActions()
        {
            this.MouseMove += (sender, arg) => RestartInputMonitorTimer();
            this.MouseMove += (sender, arg) => ShowMouseCursor();
            this.MouseMove += (sender, arg) => ShowConsole();
        }                

        private void SetupPlayWndVideoSource()
        {
            Image img = new Image();
            Binding bindingImgSrcToVlc = new Binding();
            bindingImgSrcToVlc.Source = m_viewModel.PlayWnd;
            bindingImgSrcToVlc.Path = new PropertyPath("VideoSource");
            bindingImgSrcToVlc.Mode = BindingMode.OneWay;
            img.SetBinding(Image.SourceProperty, bindingImgSrcToVlc);
            var visual = new VisualBrush(img);
            visual.Stretch = Stretch.Uniform;
            m_gridPlayWnd.Background = visual;
        }

        private void SetupDelaySingleClickTimer()
        {
            m_delaySingleClickTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(500),
                IsEnabled = true
            };
            m_delaySingleClickTimer.Stop();
            m_delaySingleClickTimer.Tick += PlayOrPause4DelayedLeftMouseClick;
        }

        private void SetupAutoHideConsoleTimer()
        {
            m_activityTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1.5),
                IsEnabled = true
            };
            m_activityTimer.Tick += HideConsoleWhenNoInputs;
            m_activityTimer.Tick += HideMouseWhenNoInputs;
            m_activityTimer.Start();
        }

        private void SetupHookSpaceKeyInput()
        {
            InputManager.Current.PreNotifyInput += new NotifyInputEventHandler(PlayOrPause4SpaceKey);
        }        

        private void PlayOrPause4SpaceKey(object sender, NotifyInputEventArgs e)
        {
            if (this.OwnedWindows.Count != 0)
            {
                return;
            }
            if (e.StagingItem.Input.RoutedEvent != Keyboard.KeyDownEvent)
                return;

            var args = e.StagingItem.Input as KeyEventArgs;
            if (args == null || args.Key != Key.Space)
            {
                return;
            }
            args.Handled = true;
            PlayOrPauseOrOpen();
        }

        private void PlayOrPauseOrOpen()
        {
            if (m_viewModel.CurrentFilePath == null)
            {
                if (m_viewModel.TryLoadLastPlayedFile())
                {
                    return;
                }
                this.Open();
                return;
            }
            else
            {
                ExecuteCommand.Execute(m_viewModel.PlayPauseCommand);
            }
        }

        private void PlayOrPause4DelayedLeftMouseClick(object sender, EventArgs e)
        {
            m_delaySingleClickTimer.Stop();
            PlayOrPauseOrOpen();
            m_gridConsole.IsEnabled = true;
        }

        private void AdjustVolume4MouseWheel(object sender, MouseWheelEventArgs e)
        {
            m_viewModel.Volume += e.Delta / 10;
        }

        private void SaveLastPlayInfo()
        {
            m_viewModel.SaveLastPlayInfo();
            m_viewModel.SaveVolumeInfo();
        }

        private void HideConsoleWhenNoInputs(object sender, EventArgs e)
        {
            if (m_viewModel.IsPlaying)
            {
                this.m_gridConsole.Visibility = Visibility.Hidden;
            }
        }

        private void HideMouseWhenNoInputs(object sender, EventArgs e)
        {
            if (m_viewModel.IsPlaying)
            {
                Mouse.OverrideCursor = Cursors.None;
            }
        }

        private void RestartInputMonitorTimer()
        {
            m_activityTimer.Stop();
            m_activityTimer.Start();
        }
        
        private void ShowConsole()
        {
            m_gridConsole.Visibility = Visibility.Visible;
        }

        private void ShowMouseCursor()
        {
            if (Mouse.OverrideCursor == Cursors.None)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                m_gridConsole.IsEnabled = true;
                m_delaySingleClickTimer.Stop();
                ToggleFullScreenMode();
            }
            else if (e.ClickCount == 1)
            {
                m_gridConsole.IsEnabled = false;
                m_delaySingleClickTimer.Start();
            }
        }

        private void ToggleFullScreenMode()
        {
            if (IsInFullScreenMode())
            {
                SwitchToNormalMode();
            }
            else
            {
                SwitchToFullScreenMode();
            }
        }

        private void SwitchToFullScreenMode()
        {
            this.WindowStyle = WindowStyle.None;

            // workaround to hide taskbar when switch from maximised to fullscreen
            this.WindowState = WindowState.Normal;

            this.WindowState = WindowState.Maximized;
        }

        private void SwitchToNormalMode()
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.WindowState = WindowState.Normal;
        }

        private bool IsInFullScreenMode()
        {
            return this.WindowState == WindowState.Maximized &&
                                this.WindowStyle == WindowStyle.None;
        }

        private void OnDropFile(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {
                var fileList = (e.Data as DataObject).GetFileDropList();
                var playList = m_viewModel.GenerateFileList(fileList.Cast<string>().ToList());
                if (playList.Count != 0)
                {
                    m_viewModel.PlayAListOfFiles(playList);
                }
            }
        }

        private void OnBtnFullScreenClick(object sender, RoutedEventArgs e)
        {
            ToggleFullScreenMode();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            RestartInputMonitorTimer();
            if (ShortKeys.IsRewindShortKey(e))
            {
                ExecuteCommand.Execute(m_viewModel.RewindCommand);
            }
            if (ShortKeys.IsForwardShortKey(e))
            {
                ExecuteCommand.Execute(m_viewModel.ForwardCommand);
            }

            if (ShortKeys.IsDecreaseVolumeShortKey(e))
            {
                m_viewModel.Volume -= 12;
            }
            if (ShortKeys.IsIncreaseVolumeShortKey(e))
            {
                m_viewModel.Volume += 12;
            }

            if (ShortKeys.IsFullScreenShortKey(e))
            {
                ToggleFullScreenMode();
            }

            if (ShortKeys.IsPauseShortKey(e))
            {
                PlayOrPauseOrOpen();
            }
        }

        private void OnBtnSettingsClick(object sender, RoutedEventArgs e)
        {
            var v = new SettingsView();
            v.Owner = this;
            ExecuteCommand.Execute(m_viewModel.PauseCommand);
            v.ShowDialog();
        }

        private void OnBtnRewindClick(object sender, RoutedEventArgs e)
        {
            ExecuteCommand.Execute(m_viewModel.RewindCommand);
        }

        private void OnBtnForwardClick(object sender, RoutedEventArgs e)
        {
            ExecuteCommand.Execute(m_viewModel.ForwardCommand);
        }

        private void OnBtnOpenClick(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void OnBtnPlayClick(object sender, RoutedEventArgs e)
        {
            PlayOrPauseOrOpen();
        }

        private void Open()
        {
            var historyView = new HistoryView();
            ExecuteCommand.Execute(m_viewModel.PauseCommand);
            historyView.Owner = this;

            HistoryModel historyModel = HistoryModel.Instance;
            if (historyModel.HistoryItems.Count == 0)
            {
                historyView.BrowseFiles();
            }
            else
            {
                historyView.ShowDialog();
            }

            if (historyView.FileList.Count == 0)
            {
                return;
            }
            var playList = m_viewModel.GenerateFileList(historyView.FileList);
            if (playList.Count != 0)
            {
                m_viewModel.PlayAListOfFiles(playList);
            }
        }
    }
}