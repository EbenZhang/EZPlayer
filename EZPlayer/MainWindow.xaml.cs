using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;
using EZPlayer.Common;
using EZPlayer.PlayList;
using EZPlayer.Power;
using EZPlayer.Subtitle;
using log4net;
using Microsoft.Win32;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Medias;
using Vlc.DotNet.Wpf;
using EZPlayer.View;
using EZPlayer.FileAssociation.Model;
using EZPlayer.History;

namespace EZPlayer
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Used to indicate that the user is currently changing the position (and the position bar shall not be updated). 
        /// </summary>
        private bool m_posUpdateFromVlc;

        private string m_selectedFilePath = null;

        private DispatcherTimer m_activityTimer;

        private DispatcherTimer m_delaySingleClickTimer;

        private SleepBarricade m_sleepBarricade;

        private readonly static string APP_START_PATH = Process.GetCurrentProcess().MainModule.FileName;
        private readonly static string APP_START_DIR = Path.GetDirectoryName(APP_START_PATH);
        private static readonly string VOLUME_INFO_FILE = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "volume.xml");

        private HistoryModel m_historyModel = new HistoryModel();

        public static DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool),
            typeof(MainWindow), new FrameworkPropertyMetadata(true,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private bool IsPlaying
        {
            get
            {
                return (bool)GetValue(IsPlayingProperty);
            }
            set
            {
                this.Topmost = value;
                if (value)
                {
                    this.m_gridConsole.Opacity = 0.4;
                }
                else
                {
                    this.m_gridConsole.Opacity = 1;
                    Mouse.OverrideCursor = null;
                }

                SetValue(IsPlayingProperty, value);
            }
        }

        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof(double),
            typeof(MainWindow),
            new PropertyMetadata(0.0));

        private double Volume
        {
            get
            {
                return (double)GetValue(VolumeProperty);
            }
            set
            {
                m_vlcControl.AudioProperties.Volume = (int)value;
                SetValue(VolumeProperty, value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VlcPlayer"/> class.
        /// </summary>
        public MainWindow()
        {
            InitVlcContext();

            InitializeComponent();

            SetupUserDataDir();

            HookSpaceKeyInput();

            LoadLastVolume();

            this.MouseWheel += new MouseWheelEventHandler(OnMouseWheel);

            IsPlaying = false;

            InitVlcControl();

            this.Closing += OnClosing;
            this.MouseLeftButtonDown += OnMouseClick;

            InitAutoHideConsoleTimer();

            InitDelaySingleClickTimer();

            m_sleepBarricade = new SleepBarricade(() => IsPlaying);

            var args = Environment.GetCommandLineArgs();
            if (args.Count() >= 2)
            {
                /// it seems vlc requires some time to init.
                new DelayTask(TimeSpan.FromMilliseconds(500),
                    () => { GenFileListAndPlay(args.Skip(1).ToList()); }
                    );
            }

            FileAssocModel.Instance.Load();
            FileAssocModel.Instance.Save();
        }

        private void LoadLastVolume()
        {
            if (File.Exists(VOLUME_INFO_FILE))
            {
                using (var stream = File.Open(VOLUME_INFO_FILE, FileMode.Open))
                {
                    Volume = (double)new XmlSerializer(typeof(double)).Deserialize(stream);
                }
            }
            else
            {
                Volume = 100;
            }
        }

        private void InitDelaySingleClickTimer()
        {
            m_delaySingleClickTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(500),
                IsEnabled = true
            };
            m_delaySingleClickTimer.Tick += new EventHandler(OnDelayedSingleClickTimer);
        }

        private void InitAutoHideConsoleTimer()
        {
            m_activityTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1.5),
                IsEnabled = true
            };
            m_activityTimer.Tick += OnNoInputs;
            m_activityTimer.Start();
        }

        private void InitVlcControl()
        {
            m_vlcControl.VideoProperties.Scale = 2;
            m_vlcControl.PositionChanged += VlcControlOnPositionChanged;
            m_vlcControl.TimeChanged += VlcControlOnTimeChanged;
        }

        private void HookSpaceKeyInput()
        {
            InputManager.Current.PreNotifyInput += new NotifyInputEventHandler(PreNotifyInput);
        }

        private static void SetupUserDataDir()
        {
            if (!Directory.Exists(AppDataDir.EZPLAYER_DATA_DIR))
            {
                Directory.CreateDirectory(AppDataDir.EZPLAYER_DATA_DIR);
            }
        }

        private static void InitVlcContext()
        {
            // Set libvlc.dll and libvlccore.dll directory path
            VlcContext.LibVlcDllsPath = Path.Combine(APP_START_DIR, "VLC");

            // Set the vlc plugins directory path
            VlcContext.LibVlcPluginsPath = Path.Combine(VlcContext.LibVlcDllsPath, "plugins");

            /* Setting up the configuration of the VLC instance.
             * You can use any available command-line option using the AddOption function (see last two options). 
             * A list of options is available at 
             *     http://wiki.videolan.org/VLC_command-line_help
             * for example. */

            // Ignore the VLC configuration file
            VlcContext.StartupOptions.IgnoreConfig = true;

            VlcContext.StartupOptions.LogOptions.LogInFile = true;
#if DEBUG
            VlcContext.StartupOptions.LogOptions.Verbosity = VlcLogVerbosities.Debug;
            VlcContext.StartupOptions.LogOptions.ShowLoggerConsole = true;
#else
            //Set the startup options
            VlcContext.StartupOptions.LogOptions.ShowLoggerConsole = false;
            VlcContext.StartupOptions.LogOptions.Verbosity = VlcLogVerbosities.None;
#endif

            // Disable showing the movie file name as an overlay
            VlcContext.StartupOptions.AddOption("--no-video-title-show");

            // The only supporting Chinese font
            VlcContext.StartupOptions.AddOption("--freetype-font=DFKai-SB");

            // Pauses the playback of a movie on the last frame
            //VlcContext.StartupOptions.AddOption("--play-and-pause");

            // Initialize the VlcContext
            VlcContext.Initialize();
        }

        void PreNotifyInput(object sender, NotifyInputEventArgs e)
        {
            if (e.StagingItem.Input.RoutedEvent != Keyboard.KeyDownEvent)
                return;

            var args = e.StagingItem.Input as KeyEventArgs;
            if (args == null || args.Key != Key.Space)
            {
                return;
            }
            args.Handled = true;
            OnBtnPauseClick(null, null);
        }

        void OnDelayedSingleClickTimer(object sender, EventArgs e)
        {
            m_delaySingleClickTimer.Stop();
            OnBtnPauseClick(null, null);
        }

        void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var volume = Volume + e.Delta / 10;
            Volume = MathUtil.Clamp(volume, 0d, 100d);
        }

        /// <summary>
        /// Main window closing event
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            SaveLastPlayInfo();
            SaveVolumeInfo();

            // Close the context. 
            VlcContext.CloseAll();
        }

        private void SaveVolumeInfo()
        {
            using (var stream = File.Open(VOLUME_INFO_FILE, FileMode.Create))
            {
                new XmlSerializer(typeof(double)).Serialize(stream, m_sliderVolume.Value);
            }
        }

        private void SaveLastPlayInfo()
        {
            if (m_vlcControl.Media != null)
            {
                var item = new HistoryItem()
                {
                    Position = m_vlcControl.Position,
                    FilePath = new Uri(m_vlcControl.Media.MRL).LocalPath,
                    Volume = m_sliderVolume.Value,
                    PlayedDate = DateTime.Now
                };
                m_historyModel.LastPlayedFile = item;
                m_historyModel.Save();
            }
        }

        /// <summary>
        /// Called if the Play button is clicked; starts the VLC playback. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void OnBtnPlayClick(object sender, RoutedEventArgs e)
        {
            if (m_vlcControl.Media == null)
            {
                if (TryLoadLastPlayedFile())
                {
                    return;
                }
                this.OnBtnOpenClick(sender, e);
                return;
            }
            else
            {
                DoPlay();
            }
        }

        private bool TryLoadLastPlayedFile()
        {
            if (m_historyModel.LastPlayedFile != null)
            {
                m_selectedFilePath = m_historyModel.LastPlayedFile.FilePath;
                m_sliderVolume.Value = m_historyModel.LastPlayedFile.Volume;
                Play(PlayListUtil.GetPlayList(m_selectedFilePath, DirectorySearcher.Instance));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called if the Pause button is clicked; pauses the VLC playback. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void OnBtnPauseClick(object sender, RoutedEventArgs e)
        {
            if (m_vlcControl.Media == null)
            {
                return;
            }
            if (m_vlcControl.IsPlaying)
            {
                m_vlcControl.Pause();
            }
            else
            {
                m_vlcControl.Play();
            }
            IsPlaying = !IsPlaying;
        }

        /// <summary>
        /// Called if the Stop button is clicked; stops the VLC playback. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void OnBtnStopClick(object sender, RoutedEventArgs e)
        {
            IsPlaying = false;
            m_vlcControl.Stop();
            m_sliderPosition.Value = 0;
        }

        /// <summary>
        /// Called if the Open button is clicked; shows the open file dialog to select a media file to play. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void OnBtnOpenClick(object sender, RoutedEventArgs e)
        {
            if (m_vlcControl.Media != null)
            {
                m_vlcControl.Pause();
                m_vlcControl.Media.ParsedChanged -= OnMediaParsed;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "Open media file for playback",
                FileName = "Media File",
                Filter = "All files |*.*"
            };

            // Process open file dialog box results
            if (openFileDialog.ShowDialog() != true)
            {
                if (m_vlcControl.Media != null)
                {
                    m_vlcControl.Play();
                }
                return;
            }

            if (m_vlcControl.Media != null)
            {
                m_vlcControl.Media.ParsedChanged -= OnMediaParsed;
            }

            m_selectedFilePath = openFileDialog.FileName;
            var playList = PlayListUtil.GetPlayList(m_selectedFilePath, DirectorySearcher.Instance);
            Play(playList);
        }

        private void Play(List<string> playList)
        {
            SubtitleUtil.PrepareSubtitle(m_selectedFilePath);
            PrepareVLCMediaList(playList);
            m_vlcControl.Media.ParsedChanged += OnMediaParsed;
            DoPlay();
        }

        private void DoPlay()
        {
            m_vlcControl.Play();
            var history = m_historyModel.GetHistoryInfo(m_selectedFilePath);
            if (history != null)
            {
                UpdatePosition(history.Position);
            }
            this.IsPlaying = true;
            UpdateTitle();
            FileAssocModel.Instance.AddNewExt(Path.GetExtension(m_selectedFilePath));
        }

        private void PrepareVLCMediaList(List<string> playList)
        {
            m_vlcControl.Media = new PathMedia(playList[0]);
            playList.RemoveAt(0);
            playList.ForEach(f => m_vlcControl.Medias.Add(new PathMedia(f)));
        }

        /// <summary>
        /// Volume value changed by the user. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void SliderVolumeValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = Convert.ToInt32(m_sliderVolume.Value);
        }

        /// <summary>
        /// Called by <see cref="VlcControl.Media"/> when the media information was parsed. 
        /// </summary>
        /// <param name="sender">Event sending media. </param>
        /// <param name="e">VLC event arguments. </param>
        private void OnMediaParsed(MediaBase sender, VlcEventArgs<int> e)
        {
            m_timeIndicator.Text = string.Format(
                "{0:00}:{1:00}:{2:00}",
                m_vlcControl.Media.Duration.Hours,
                m_vlcControl.Media.Duration.Minutes,
                m_vlcControl.Media.Duration.Seconds);

            Volume = m_vlcControl.AudioProperties.Volume;
        }

        /// <summary>
        /// Called by the <see cref="VlcControl"/> when the media position changed during playback.
        /// </summary>
        /// <param name="sender">Event sennding control. </param>
        /// <param name="e">VLC event arguments. </param>
        private void VlcControlOnPositionChanged(VlcControl sender, VlcEventArgs<float> e)
        {
            m_posUpdateFromVlc = true;
            m_sliderPosition.Value = e.Data;
        }

        private void VlcControlOnTimeChanged(VlcControl sender, VlcEventArgs<TimeSpan> e)
        {
            if (m_vlcControl.Media == null)
                return;
            var duration = m_vlcControl.Media.Duration;
            m_timeIndicator.Text = string.Format(
                "{0:00}:{1:00}:{2:00} / {3:00}:{4:00}:{5:00}",
                e.Data.Hours,
                e.Data.Minutes,
                e.Data.Seconds,
                duration.Hours,
                duration.Minutes,
                duration.Seconds);

            if (IsPlaying != m_vlcControl.IsPlaying)
            {
                IsPlaying = m_vlcControl.IsPlaying;
            }
        }

        /// <summary>
        /// Stop position changing, re-enables updates for the slider by the player. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void SliderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(m_sliderPosition).X;
            var sliderRange = m_sliderPosition.Maximum - m_sliderPosition.Minimum;

            float value = ((float)mousePos / (float)m_sliderPosition.Width) * (float)sliderRange;

            UpdatePosition(value);
        }

        private void UpdatePosition(float value)
        {
            value = MathUtil.Clamp(value, 0.0f, 1.0f);
            m_sliderPosition.Value = value;
        }

        /// <summary>
        /// Change position when the slider value is updated. 
        /// </summary>
        /// <param name="sender">Event sender. </param>
        /// <param name="e">Event arguments. </param>
        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (m_posUpdateFromVlc)
            {
                m_posUpdateFromVlc = false;
                //Update the current position text when it is in pause
                var duration = m_vlcControl.Media == null ? TimeSpan.Zero : m_vlcControl.Media.Duration;
                var time = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * m_vlcControl.Position);
                m_timeIndicator.Text = string.Format(
                    "{0:00}:{1:00}:{2:00} / {3:00}:{4:00}:{5:00}",
                    time.Hours,
                    time.Minutes,
                    time.Seconds,
                    duration.Hours,
                    duration.Minutes,
                    duration.Seconds);
                return;
            }
            else
            {
                m_vlcControl.Position = (float)e.NewValue;
            }
        }

        private void OnBtnPreviousClick(object sender, RoutedEventArgs e)
        {
            var cur = m_vlcControl.Media;
            if (cur == null)
            {
                OnBtnOpenClick(sender, e);
                return;
            }
            int index = m_vlcControl.Medias.IndexOf(cur);
            if (index > 0)
            {
                m_vlcControl.Previous();
                UpdateTitle();
            }
        }

        private void OnBtnNextClick(object sender, RoutedEventArgs e)
        {
            var cur = m_vlcControl.Media;
            if (cur == null)
            {
                OnBtnOpenClick(sender, e);
                return;
            }
            int index = m_vlcControl.Medias.IndexOf(cur);
            if (index < m_vlcControl.Medias.Count - 1)
            {
                m_vlcControl.Next();
                UpdateTitle();
            }
        }

        private void UpdateTitle()
        {
            Uri uri = new Uri(m_vlcControl.Media.MRL);
            this.Title = Path.GetFileNameWithoutExtension(uri.LocalPath);
        }

        private void OnNoInputs(object sender, EventArgs e)
        {
            if (m_vlcControl.IsPlaying)
            {
                this.m_gridConsole.Visibility = Visibility.Hidden;
                Mouse.OverrideCursor = Cursors.None;
            }
        }

        private void RestartInputMonitorTimer()
        {
            m_activityTimer.Stop();
            m_activityTimer.Start();
        }
        
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            RestartInputMonitorTimer();
            if (Mouse.OverrideCursor == Cursors.None)
            {
                Mouse.OverrideCursor = null;
            }
            m_gridConsole.Visibility = Visibility.Visible;
        }

        #region FullScreen
        void OnMouseClick(object sender, MouseButtonEventArgs e)
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

            // workaround to hide taskbar when swith from maximised to fullscreen
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
        #endregion

        private void OnDropFile(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {
                var fileList = (e.Data as DataObject).GetFileDropList();
                GenFileListAndPlay(fileList.Cast<string>().ToList());
            }
        }

        private void GenFileListAndPlay(List<string> fileList)
        {
            if (fileList.Count == 1)
            {
                m_selectedFilePath = fileList[0];
                Play(PlayListUtil.GetPlayList(m_selectedFilePath, DirectorySearcher.Instance));
            }
            else if (fileList.Count > 1)
            {
                var sortedFileList = fileList.Cast<string>().OrderBy(s => s).ToList();
                m_selectedFilePath = sortedFileList[0];
                Play(sortedFileList);
            }
        }

        private void OnBtnForwardClick(object sender, RoutedEventArgs e)
        {
            var newValue = m_vlcControl.Position + 0.001f;
            UpdatePosition(newValue);
        }

        private void OnBtnRewindClick(object sender, RoutedEventArgs e)
        {
            var newValue = m_vlcControl.Position - 0.001f;
            UpdatePosition(newValue);
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
                OnBtnRewindClick(null, null);
            }
            if (ShortKeys.IsForwardShortKey(e))
            {
                OnBtnForwardClick(null, null);
            }

            if (ShortKeys.IsDecreaseVolumeShortKey(e))
            {
                var volume = Volume - 12;
                Volume = MathUtil.Clamp(volume, 0d, 100d);
            }
            if (ShortKeys.IsIncreaseVolumeShortKey(e))
            {
                var volume = Volume + 12;
                Volume = MathUtil.Clamp(volume, 0d, 100d);
            }

            if (ShortKeys.IsFullScreenShortKey(e))
            {
                ToggleFullScreenMode();
            }

            if (ShortKeys.IsPauseShortKey(e))
            {
                OnBtnPauseClick(null, null);
            }
        }

        private void OnBtnSettingsClick(object sender, RoutedEventArgs e)
        {
            var v = new FileAssociationView();
            var isPlaying = IsPlaying;
            if (IsPlaying)
            {
                m_vlcControl.Pause();
                IsPlaying = false;
            }
            v.Owner = this;
            v.ShowDialog();
            if (isPlaying)
            {
                m_vlcControl.Play();
                IsPlaying = true;
            }
        }
    }
}