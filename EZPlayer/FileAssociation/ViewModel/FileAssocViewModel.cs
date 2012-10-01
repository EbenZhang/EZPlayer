using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using EZPlayer.Common;
using EZPlayer.FileAssociation.Model;

namespace EZPlayer.ViewModel
{
    public class FileAssocViewModel : ViewModelBase
    {
        private FileAssocModel m_model = FileAssocModel.Instance;
        public FileAssocViewModel()
        {
            if (DesingerHelper.IsDesigner)
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
                return new RelayCommand(param => m_model.Save(),
                    param => true);
            }
        }
    }
}
