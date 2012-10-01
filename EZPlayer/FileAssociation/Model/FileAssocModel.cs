using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using EZPlayer.Common;

namespace EZPlayer.FileAssociation.Model
{
    public class FileAssocModel
    {
        private static readonly string DEFAULT_EXT_LIST_FILE = "DefaultExtensionList.xml";
        private static readonly string DEFAULT_EXT_LIST_PATH =
            Path.Combine(AppDataDir.PROCESS_DIR,
            "DefaultConfig",
            DEFAULT_EXT_LIST_FILE);
        private static readonly string EXT_LIST_FILE = "ExtensionList.xml";
        public static readonly string EXT_LIST_FILE_PATH = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, EXT_LIST_FILE);
        private List<ExtensionItem> m_list;
        public List<ExtensionItem> ExtensionList
        {
            get { return m_list; }
        }

        public static FileAssocModel Instance = new FileAssocModel();
        protected FileAssocModel()
        {
        }
        public void Save()
        {
            var path = AppDataDir.PROCESS_PATH;
            var name = Path.GetFileNameWithoutExtension(path);
            new AssociationUtil(name, path, m_list);
            using (var stream = File.Open(EXT_LIST_FILE_PATH, FileMode.Create, FileAccess.ReadWrite))
            {
                new XmlSerializer(typeof(List<ExtensionItem>)).Serialize(stream, m_list);
            }
        }

        public void Load()
        {
            if (File.Exists(FileAssocModel.EXT_LIST_FILE_PATH))
            {
                Load(FileAssocModel.EXT_LIST_FILE_PATH);
            }
            else
            {
                Load(DEFAULT_EXT_LIST_PATH);
            }
        }

        private void Load(string configPath)
        {
            using (var stream = File.Open(configPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                m_list = new XmlSerializer(typeof(List<ExtensionItem>)).Deserialize(stream) as List<ExtensionItem>;
            }
        }
    }
}
