using System;
using System.Globalization;
using System.Windows.Data;

namespace SteelTube.Desktop.Common
{
    /// <summary>Negates a bool -- used to drive the "Length" radio button off the same UseWeightInput flag as "Weight".</summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;
    }
}