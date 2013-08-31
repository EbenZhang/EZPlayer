using EZPlayer.Common;
using System;
using System.Windows;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Medias;
using Vlc.DotNet.Wpf;

namespace EZPlayer.Model
{
    public class MainWndModel
    {
        public VlcControl m_vlcControl = new VlcControl();

        public delegate void NotifyChange();
        public event NotifyChange EvtTimeChanged;

        /// <summary>
        /// Will be triggered when the first time a media is loaded.
        /// Will be triggered when vlc switches to next media automatically.
        /// </summary>
        public event NotifyChange EvtMediaParsed;
        
        public MainWndModel()
        {
        }
        
        public void Init()
        {
            m_vlcControl.VideoProperties.Scale = 2;
            m_vlcControl.TimeChanged += VlcControlOnTimeChanged;
        }

        public bool IsPlaying
        {
            get
            {
                return m_vlcControl.IsPlaying;
            }
        }

        public float Position
        {
            get
            {
                return m_vlcControl.Position;
            }
            set
            {
                m_vlcControl.Position = MathUtil.Clamp(value, 0.0f, 1.0f);
            }
        }

        public double Volume
        {
            get
            {
                return m_vlcControl.AudioProperties.Volume;
            }
            set
            {
                m_vlcControl.AudioProperties.Volume = (int)MathUtil.Clamp(value, 0d, MaxVolume);
            }
        }

        public int MaxVolume
        {
            get { return 200; }
        }

        public string CurrentFilePath
        {
            get
            {
                if (m_vlcControl.Media != null)
                {
                    return new Uri(m_vlcControl.Media.MRL).LocalPath;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                SetMedia(value);
            }
        }

        public string TimeIndicator
        {
            get;
            private set;
        }

        public FrameworkElement PlayWnd
        {
            get
            {
                return m_vlcControl;
            }
        }

        public void Pause()
        {
            m_vlcControl.Pause();
        }

        public void Stop()
        {
            m_vlcControl.Stop();
        }

        public void Play()
        {
            m_vlcControl.Play();
        }

        public void Previous()
        {
            var cur = m_vlcControl.Media;
            if (cur == null)
            {
                return;
            }
            int index = m_vlcControl.Medias.IndexOf(cur);
            if (index > 0)
            {
                m_vlcControl.Previous();
            }
        }

        public void Next()
        {
            var cur = m_vlcControl.Media;
            if (cur == null)
            {
                return;
            }
            int index = m_vlcControl.Medias.IndexOf(cur);
            if (index < m_vlcControl.Medias.Count - 1)
            {
                m_vlcControl.Next();
            }
        }

        public void SetMedia(string mediaPath)
        {
            m_vlcControl.Media = new PathMedia(mediaPath);
            m_vlcControl.Media.ParsedChanged += OnMediaParsed;
        }

        /// <summary>
        /// Called by <see cref="VlcControl.Media"/> when the media information was parsed. 
        /// </summary>
        /// <param name="sender">Event sending media. </param>
        /// <param name="e">VLC event arguments. </param>
        private void OnMediaParsed(MediaBase sender, VlcEventArgs<int> e)
        {
            if (EvtMediaParsed != null)
            {
                EvtMediaParsed();
            }
        }

        private void VlcControlOnTimeChanged(VlcControl sender, VlcEventArgs<TimeSpan> e)
        {
            if (m_vlcControl.Media == null)
                return;

            TimeIndicator = string.Format(
                "{0:00}:{1:00}:{2:00} / {3:00}:{4:00}:{5:00}",
                e.Data.Hours,
                e.Data.Minutes,
                e.Data.Seconds,
                m_vlcControl.Media.Duration.Hours,
                m_vlcControl.Media.Duration.Minutes,
                m_vlcControl.Media.Duration.Seconds);

            if (EvtTimeChanged != null)
            {
                EvtTimeChanged();
            }
        }

        public void AddMedia(string mediaPath)
        {
            if (m_vlcControl.Media != null)
            {
                m_vlcControl.Media.ParsedChanged -= this.OnMediaParsed;
            }
            var media = new PathMedia(mediaPath);
            media.ParsedChanged += this.OnMediaParsed;
            m_vlcControl.Medias.Add(media);
        }

        public void SetSubtitleFile(string subTitleFilePath)
        {
            m_vlcControl.VideoProperties.SetSubtitleFile(subTitleFilePath);
        }
    }
}
