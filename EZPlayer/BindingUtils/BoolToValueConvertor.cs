using System;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows;

namespace EZPlayer.BindingUtils
{
    public class BoolToValueBinding : Binding
    {
        public BoolToValueBinding()
        {
            Initialize();
        }

        public BoolToValueBinding(string path)
            : base(path)
        {
            Initialize();
        }

        public BoolToValueBinding(string path, object valueIfTrue, object valueIfFalse)
            : base(path)
        {
            Initialize();
            this.ValueIfTrue = valueIfTrue;
            this.ValueIfFalse = valueIfFalse;
        }

        private void Initialize()
        {
            this.ValueIfTrue = Binding.DoNothing;
            this.ValueIfFalse = Binding.DoNothing;
            this.Converter = new BoolToValueConverter(this);
        }

        [ConstructorArgument("valueIfTrue")]
        public object ValueIfTrue { get; set; }

        [ConstructorArgument("valueIfFalse")]
        public object ValueIfFalse { get; set; }

        private class BoolToValueConverter : IValueConverter
        {
            public BoolToValueConverter(BoolToValueBinding boolToValueBinding)
            {
                m_boolToValueBinding = boolToValueBinding;
            }

            private BoolToValueBinding m_boolToValueBinding;

            #region IValueConverter Members

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                try
                {
                    bool b = System.Convert.ToBoolean(value);
                    return b ? m_boolToValueBinding.ValueIfTrue : m_boolToValueBinding.ValueIfFalse;
                }
                catch
                {
                    return DependencyProperty.UnsetValue;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return Binding.DoNothing;
            }

            #endregion
        }

    }
}
