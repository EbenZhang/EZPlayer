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

namespace EZPlayer.ViewModel
{
    public class MainWndViewModel : ViewModelBase
    {
        private static readonly string VOLUME_INFO_FILE = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "volume.xml");

        private SleepBarricade m_sleepBarricade;
        private bool m_isPlaying = false;
        private MainWndModel m_model = new MainWndModel();
        
        public MainWndViewModel()
        {
            m_model.EvtTimeChanged += OnTimeChanged;
            m_model.EvtTimeChanged += () => NotifyPropertyChange(() => Position);
            m_model.EvtMediaParsed += () => UpdateTitle();
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
                    param => CurrentFilePath != null);
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

        public ICommand PreviousCommand
        {
            get
            {
                return new RelayCommand(param => Previous(),
                    param => m_model.CurrentFilePath != null);
            }
        }

        public ICommand NextCommand
        {
            get
            {
                return new RelayCommand(param => Next(),
                    param => m_model.CurrentFilePath != null);
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

        public ICommand PauseCommand
        {
            get
            {
                return new RelayCommand(param => Pause(),
                    param => IsPlaying);
            }
        }

        #endregion

        #region Properties
        public string CurrentFilePath
        {
            get { return m_model.CurrentFilePath; }
        }
        public bool IsPlaying
        {
            get
            {
                return m_isPlaying;
            }
            set
            {
                m_isPlaying = value;
                NotifyPropertyChange(() => IsPlaying);
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
                NotifyPropertyChange(() => Position);
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
                NotifyPropertyChange(() => Volume);
            }
        }

        public int MaxVolume
        {
            get { return m_model.MaxVolume; }
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
                HistoryModel.Instance.LastPlayedFile = item;
                HistoryModel.Instance.Save();
            }
        }

        public List<string> GenerateFileList(List<string> fileList)
        {
            if (fileList.Count == 1)
            {
                var file = fileList[0];
                if (!File.Exists(file))
                {
                    return new List<string>();
                }
                SelectedPath = fileList[0];
                return PlayListUtil.GetPlayList(SelectedPath, DirectorySearcher.Instance);
            }
            else if (fileList.Count > 1)
            {
                var sortedFileList = fileList
                    .Where(f => File.Exists(f)).OrderBy(s => s).ToList();
                if (sortedFileList.Count == 0)
                {
                    return sortedFileList;
                }
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
            if (playList.Count == 0)
            {
                return;
            }
            SubtitleUtil.PrepareSubtitle(SelectedPath);
            SaveLastPlayInfo();
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

        public bool TryLoadLastPlayedFile()
        {
            if (HistoryModel.Instance.LastPlayedFile != null)
            {
                SelectedPath = HistoryModel.Instance.LastPlayedFile.FilePath;
                PlayAListOfFiles(PlayListUtil.GetPlayList(SelectedPath, DirectorySearcher.Instance));
                return true;
            }
            return false;
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
            m_model.Previous();
            UpdateTitle();
        }

        private void Next()
        {
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
            var history = HistoryModel.Instance.GetHistoryInfo(SelectedPath);
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
            NotifyPropertyChange(() => Title);
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
            NotifyPropertyChange(() => TimeIndicator);

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
