using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using EZPlayer.Common;
using EZPlayer.FileAssociation.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace EZPlayer.ViewModel
{
    public class FileAssocViewModel : ViewModelBase
    {
        private FileAssocModel m_model = FileAssocModel.Instance;
        public FileAssocViewModel()
        {
            if (base.IsInDesignMode)
            {
                return;
            }
            m_model.Load();
        }
        
        public ObservableCollection<ExtensionItem> Extensions
        {
            get
            {
                return new ObservableCollection<ExtensionItem>(m_model.ExtensionList);
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand(() => m_model.Save(),
                    () => true);
            }
        }
    }
}
