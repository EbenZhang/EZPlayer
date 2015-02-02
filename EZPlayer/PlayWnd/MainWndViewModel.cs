using EZPlayer.Common;
using EZPlayer.FileAssociation.Model;
using EZPlayer.History;
using EZPlayer.Model;
using EZPlayer.PlayList;
using EZPlayer.Power;
using EZPlayer.Subtitle;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Org.Mentalis.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace EZPlayer.ViewModel
{
    // A delegate type for hooking up change notifications.
    public delegate void AllPlayed();

    public class MainWndViewModel : ViewModelBase
    {
        private static readonly string VOLUME_INFO_FILE = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "volume.xml");

        private SleepBarricade m_sleepBarricade;
        private bool m_isPlaying = false;
        private MainWndModel m_model = new MainWndModel();

        public event AllPlayed EvtAllPlayed;
        
        public MainWndViewModel()
        {
            m_model.EvtTimeChanged += OnTimeChanged;
            m_model.EvtMediaParsed += OnMediaChanged;
            PlayingFiles = new ObservableCollection<string>();
        }

        void OnMediaChanged()
        {
            RestoreLastPlayStatus();
            UpdateTitle();
            SearchOnlineSubtitle();
            UpdateFileAssoc();
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
                return new RelayCommand(() => PlayOrPause(),
                    () => CurrentFilePath != null);
            }
        }

        public ICommand StopCommand
        {
            get
            {
                return new RelayCommand(() => Stop(),
                    () => IsPlaying);
            }
        }

        public ICommand PreviousCommand
        {
            get
            {
                return new RelayCommand(() => Previous(),
                    () => m_model.CurrentFilePath != null);
            }
        }

        public ICommand NextCommand
        {
            get
            {
                return new RelayCommand(() => Next(),
                    () => m_model.CurrentFilePath != null);
            }
        }

        public ICommand ForwardCommand
        {
            get
            {
                return new RelayCommand(() => Forward(),
                    () => IsPlaying);
            }
        }

        public ICommand RewindCommand
        {
            get
            {
                return new RelayCommand(() => Rewind(),
                    () => IsPlaying);
            }
        }

        public ICommand PauseCommand
        {
            get
            {
                return new RelayCommand(() => Pause(),
                    () => IsPlaying);
            }
        }

        #endregion

        #region Properties
        public string CurrentFilePath
        {
            get { return m_model.CurrentFilePath; }
            set
            {
                if (m_model.CurrentFilePath != value)
                {
                    if (!string.IsNullOrWhiteSpace(m_model.CurrentFilePath))
                    {
                        SaveLastPlayInfo();
                    }
                    m_model.CurrentFilePath = value;

                    IsPlaying = m_model.CurrentFilePath != null;
                    
                    //OnMediaChanged();

                    RaisePropertyChanged(() => CurrentFilePath);
                }
            }
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
                RaisePropertyChanged(() => IsPlaying);
            }
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
                RaisePropertyChanged(() => Position);
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
                RaisePropertyChanged(() => Volume);
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
                    return "EZPlayer";
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

                if (file.ToUpperInvariant().StartsWith("HTTP"))
                {
                    return fileList;
                }
                if (!File.Exists(file))
                {
                    return new List<string>();
                }
                return PlayListUtil.GetPlayList(fileList[0], DirectorySearcher.Instance);
            }
            else if (fileList.Count > 1)
            {
                var sortedFileList = fileList
                    .Where(f => File.Exists(f)).OrderBy(s => s).ToList();
                if (sortedFileList.Count == 0)
                {
                    return sortedFileList;
                }
                return sortedFileList;
            }
            else
            {
                throw new ApplicationException("No file in the list");
            }
        }

        public ObservableCollection<String> PlayingFiles
        {
            get;
            set;
        }

        public void PlayAListOfFiles(List<string> playList)
        {
            if (playList.Count == 0)
            {   
                return;
            }

            this.CurrentFilePath = null;

            PlayingFiles.Clear();
            playList.ForEach(r => PlayingFiles.Add(r));
            //NotifyPropertyChange(() => PlayingFiles);
            SubtitleUtil.PrepareSubtitle(playList[0]);
            this.CurrentFilePath = playList[0];
        }

        private void SearchOnlineSubtitle()
        {
            Task<string> subtitleTask = new Task<string>(() => new OpenSubtitleSearcher().DownloadSubtitles(CurrentFilePath));
            subtitleTask.ContinueWith((x) =>
            {
                var filePath = x.Result;
                if (!string.IsNullOrEmpty(filePath) && Path.GetDirectoryName(filePath) == Path.GetDirectoryName(CurrentFilePath))
                {
                    SubtitleUtil.PrepareSubtitle(CurrentFilePath);
                    m_model.SetSubtitleFile(filePath);
                }
            }, TaskScheduler.Current);
            subtitleTask.Start();
        }

        private void SetupSleepBarricade()
        {
            m_sleepBarricade = new SleepBarricade(() => IsPlaying);
        }

        private void SetupFileAssoc()
        {
            Task.Factory.StartNew(
                () => FileAssocModel.Instance.Load())
                .ContinueWith((x) => FileAssocModel.Instance.Save());
        }

        public bool TryLoadLastPlayedFile()
        {
            if (HistoryModel.Instance.LastPlayedFile != null)
            {
                PlayAListOfFiles(PlayListUtil.GetPlayList(HistoryModel.Instance.LastPlayedFile.FilePath, DirectorySearcher.Instance));
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
            int pre = PlayingFiles.IndexOf(CurrentFilePath) - 1;
            if (pre >= 0)
            {
                CurrentFilePath = PlayingFiles[pre];
            }
        }

        private void Next()
        {
            int next = GetNextMediaIndex();
            if (next != -1)
            {
                CurrentFilePath = PlayingFiles[next];
            }
        }

        private int GetNextMediaIndex()
        {
            int next = PlayingFiles.IndexOf(CurrentFilePath) + 1;
            return next < PlayingFiles.Count ? next : -1;
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

        private void RestoreLastPlayStatus()
        {
            var history = HistoryModel.Instance.GetHistoryInfo(CurrentFilePath);
            if (history != null)
            {
                Position = history.Position >= 1 ? 0 : history.Position;
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
                Pause();
            }
            else
            {
                m_model.Play();
                IsPlaying = true;
            }
        }
        
        private void UpdateFileAssoc()
        {
            FileAssocModel.Instance.AddNewExt(Path.GetExtension(CurrentFilePath));
        }

        private void UpdateTitle()
        {
            RaisePropertyChanged(() => Title);
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
        
        private void OnTimeChanged()
        {
            RaisePropertyChanged(() => TimeIndicator);

            RaisePropertyChanged(() => Position);

            SyncPlayStatusWithModel();

            if (Position >= 1)
            {
                if (GetNextMediaIndex() != -1)
                {
                    Next();
                }
                else
                {
                    if (EvtAllPlayed != null)
                    {
                        EvtAllPlayed();
                    }
                }
            }
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
