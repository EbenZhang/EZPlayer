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
                m_viewModel.PlayAListOfFiles(playList);
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
        }

        private void SetupMouseLeftClickActions()
        {
            this.MouseLeftButtonDown += OnMouseClick;
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
            m_delaySingleClickTimer.Tick += new EventHandler(PlayOrPause4DelayedLeftMouseClick);
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
            if (e.StagingItem.Input.RoutedEvent != Keyboard.KeyDownEvent)
                return;

            var args = e.StagingItem.Input as KeyEventArgs;
            if (args == null || args.Key != Key.Space)
            {
                return;
            }
            args.Handled = true;
            m_viewModel.PlayPauseCommand.Execute(null);
        }

        private void PlayOrPause4DelayedLeftMouseClick(object sender, EventArgs e)
        {
            m_delaySingleClickTimer.Stop();
            m_viewModel.PlayPauseCommand.Execute(null);
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
                m_delaySingleClickTimer.Stop();
                ToggleFullScreenMode();
            }
            else if (e.ClickCount == 1)
            {
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
                m_viewModel.PlayAListOfFiles(playList);
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
                m_viewModel.RewindCommand.Execute(null);
            }
            if (ShortKeys.IsForwardShortKey(e))
            {
                m_viewModel.ForwardCommand.Execute(null);
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
                m_viewModel.PlayPauseCommand.Execute(null);
            }
        }

        private void OnBtnSettingsClick(object sender, RoutedEventArgs e)
        {
            var v = new FileAssociationView();
            var isPlaying = m_viewModel.IsPlaying;
            if (isPlaying)
            {
                m_viewModel.PlayPauseCommand.Execute(null);
            }
            v.Owner = this;
            v.ShowDialog();
            if (isPlaying)
            {
                m_viewModel.PlayPauseCommand.Execute(null);
            }
        }

        private void OnBtnRewindClick(object sender, RoutedEventArgs e)
        {
            if (m_viewModel.RewindCommand.CanExecute(null))
            {
                m_viewModel.RewindCommand.Execute(null);
            }
        }

        private void OnBtnForwardClick(object sender, RoutedEventArgs e)
        {
            if (m_viewModel.ForwardCommand.CanExecute(null))
            {
                m_viewModel.ForwardCommand.Execute(null);
            }
        }
    }
}