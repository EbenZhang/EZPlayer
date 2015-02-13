using System;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Collections.Generic;

namespace EZPlayer.BindingUtils
{
    [ContentProperty("Converters")]
    public class CompositeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (IValueConverter converter in Converters)
            {
                value = converter.Convert(value, targetType, parameter, culture);
                if (value == DependencyProperty.UnsetValue
                    || value == Binding.DoNothing)
                    break;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            for (var index = Converters.Count - 1; index >= 0; index--)
            {
                value = Converters[index].ConvertBack(value, targetType,
                    parameter, culture);
                if (value == DependencyProperty.UnsetValue
                    || value == Binding.DoNothing)
                    break;
            }
            return value;
        }

        public List<IValueConverter> Converters { get; } = new List<IValueConverter>();
    }
}
