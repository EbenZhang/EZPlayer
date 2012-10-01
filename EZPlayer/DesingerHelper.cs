using System.Windows;

namespace EZPlayer
{
    class DesingerHelper
    {
        public static bool IsDesigner
        {
            get
            {
                bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(
                    new DependencyObject());
                return designTime;
            }
        }
    }
}
