using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using EZPlayer.Common;
using EZPlayer.FileAssociation.Model;
using EZPlayer.History;
using EZPlayer.Model;
using EZPlayer.PlayList;
using EZPlayer.Power;
using EZPlayer.Subtitle;
using Microsoft.Win32;

namespace EZPlayer.ViewModel
{
    public class MainWndViewModel : ViewModelBase
    {
        private static readonly string VOLUME_INFO_FILE = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "volume.xml");
        private static readonly string LAST_PLAY_INFO_FILE_PATH = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "lastplay.xml");
        private static readonly string HISTORY_INFO_FILE_PATH = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "history.xml");
        
        private HistoryModel m_historyModel = new HistoryModel(LAST_PLAY_INFO_FILE_PATH, HISTORY_INFO_FILE_PATH);
        private SleepBarricade m_sleepBarricade;
        private bool m_isPlaying = false;
        private MainWndModel m_model = new MainWndModel();
        
        public MainWndViewModel()
        {
            m_model.EvtTimeChanged += OnTimeChanged;
            m_model.EvtPositionChanged += () => OnPropertyChanged("Position");
        }

        public void Init()
        {
            m_model.Init();

            SetupFileAssoc();
            
            LoadLastVolume();

            SetupSleepBarricade();
        }        

        #region Commands
        public ICommand PlayPauseCommand
        {
            get
            {
                return new RelayCommand(param => PlayOrPause(),
                    param => true);
            }
        }

        public ICommand StopCommand
        {
            get
            {
                return new RelayCommand(param => Stop(),
                    param => IsPlaying);
            }
        }
        public ICommand OpenCommand
        {
            get
            {
                return new RelayCommand(param => Open(),
                    param => true);
            }
        }

        public ICommand PreviousCommand
        {
            get
            {
                return new RelayCommand(param => Previous(),
                    param => true);
            }
        }

        public ICommand NextCommand
        {
            get
            {
                return new RelayCommand(param => Next(),
                    param => true);
            }
        }

        public ICommand ForwardCommand
        {
            get
            {
                return new RelayCommand(param => Forward(),
                    param => IsPlaying);
            }
        }

        public ICommand RewindCommand
        {
            get
            {
                return new RelayCommand(param => Rewind(),
                    param => IsPlaying);
            }
        }

        #endregion

        #region Properties
        public bool IsPlaying
        {
            get
            {
                return m_isPlaying;
            }
            set
            {
                m_isPlaying = value;
                OnPropertyChanged("IsPlaying");
            }
        }
        public string SelectedPath
        {
            get;
            set;
        }

        public float Position
        {
            get
            {
                return m_model.Position;
            }
            set
            {
                m_model.Position = value;
                OnPropertyChanged("Position");
            }
        }

        public FrameworkElement PlayWnd
        {
            get 
            {
                return m_model.PlayWnd;
            }
        }

        public double Volume
        {
            get
            {
                return m_model.Volume;
            }
            set
            {
                m_model.Volume = value;        
                OnPropertyChanged("Volume");
            }
        }

        public string Title
        {
            get
            {
                var filePath = m_model.CurrentFilePath;
                if (filePath == null)
                {
                    return Process.GetCurrentProcess().MainModule.ModuleName;
                }
                else
                {
                    return Path.GetFileNameWithoutExtension(filePath);
                }
            }
        }

        public string TimeIndicator
        {
            get
            {
                return m_model.TimeIndicator;
            }
        }
        #endregion        

        public void SaveVolumeInfo()
        {
            using (var stream = File.Open(VOLUME_INFO_FILE, FileMode.Create))
            {
                new XmlSerializer(typeof(double)).Serialize(stream, Volume);
            }
        }
        
        public void SaveLastPlayInfo()
        {
            if (m_model.CurrentFilePath != null)
            {
                var item = new HistoryItem()
                {
                    Position = m_model.Position,
                    FilePath = m_model.CurrentFilePath,
                    Volume = m_model.Volume,
                    PlayedDate = DateTime.Now
                };
                m_historyModel.LastPlayedFile = item;
                m_historyModel.Save();
            }
        }

        public List<string> GenerateFileList(List<string> fileList)
        {
            if (fileList.Count == 1)
            {
                SelectedPath = fileList[0];
                return PlayListUtil.GetPlayList(SelectedPath, DirectorySearcher.Instance);
            }
            else if (fileList.Count > 1)
            {
                var sortedFileList = fileList.Cast<string>().OrderBy(s => s).ToList();
                SelectedPath = sortedFileList[0];
                return sortedFileList;
            }
            else
            {
                throw new ApplicationException("No file in the list");
            }
        }        

        public void PlayAListOfFiles(List<string> playList)
        {
            SubtitleUtil.PrepareSubtitle(SelectedPath);
            PrepareVLCMediaList(playList);
            StartPlay();
        }

        private void SetupSleepBarricade()
        {
            m_sleepBarricade = new SleepBarricade(() => IsPlaying);
        }

        private void SetupFileAssoc()
        {
            FileAssocModel.Instance.Load();
            FileAssocModel.Instance.Save();
        }

        private bool TryLoadLastPlayedFile()
        {
            if (m_historyModel.LastPlayedFile != null)
            {
                SelectedPath = m_historyModel.LastPlayedFile.FilePath;
                PlayAListOfFiles(PlayListUtil.GetPlayList(SelectedPath, DirectorySearcher.Instance));
                return true;
            }
            return false;
        }

        private void Open()
        {
            bool isPlaying = m_model.IsPlaying;
            if (m_model.CurrentFilePath != null && isPlaying)
            {
                m_model.Pause();
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
                if (m_model.CurrentFilePath != null && isPlaying)
                {
                    m_model.Play();
                }
                return;
            }

            SelectedPath = openFileDialog.FileName;
            var playList = PlayListUtil.GetPlayList(SelectedPath, DirectorySearcher.Instance);
            PlayAListOfFiles(playList);
        }

        private void Pause()
        {
            if (m_model.IsPlaying)
            {
                m_model.Pause();
                IsPlaying = false;
            }
        }

        private void Previous()
        {
            if (m_model.CurrentFilePath == null)
            {
                Open();
                return;
            }
            m_model.Previous();
            UpdateTitle();
        }

        private void Next()
        {
            if (m_model.CurrentFilePath == null)
            {
                Open();
                return;
            }
            
            m_model.Next();
            UpdateTitle();
        }
        
        private void Forward()
        {
            var newValue = Position + 0.001f;
            Position = newValue;
        }

        private void Rewind()
        {
            var newValue = Position - 0.001f;
            Position = newValue;
        }

        private void StartPlay()
        {
            m_model.Play();
            IsPlaying = true;
            RestoreLastPlayStatus();
            UpdateTitle();
            UpdateFileAssoc();
        }

        private void RestoreLastPlayStatus()
        {
            var history = m_historyModel.GetHistoryInfo(SelectedPath);
            if (history != null)
            {
                Position = history.Position;
                Volume = history.Volume;
            }
        }

        private void Stop()
        {
            IsPlaying = false;
            m_model.Stop();
            Volume = 0;
        }

        private void PlayOrPause()
        {
            if (m_model.CurrentFilePath == null)
            {
                if (TryLoadLastPlayedFile())
                {
                    return;
                }
                Open();
                return;
            }

            if (m_model.IsPlaying)
            {
                m_model.Pause();
                IsPlaying = false;
            }
            else
            {
                m_model.Play();
                IsPlaying = true;
            }
        }
        
        private void UpdateFileAssoc()
        {
            FileAssocModel.Instance.AddNewExt(Path.GetExtension(SelectedPath));
        }

        private void UpdateTitle()
        {
            OnPropertyChanged("Title");
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

        private void PrepareVLCMediaList(List<string> playList)
        {
            m_model.SetMedia(playList[0]);
            playList.RemoveAt(0);
            playList.ForEach(f => m_model.AddMedia(f));
        }

        private void OnTimeChanged()
        {
            OnPropertyChanged("TimeIndicator");

            SyncPlayStatusWithModel();
        }

        private void SyncPlayStatusWithModel()
        {
            if (IsPlaying != m_model.IsPlaying)
            {
                IsPlaying = m_model.IsPlaying;
            }
        }
    }
}
