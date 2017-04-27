using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MedicalOfficeClient.Converters
{
  public class DateFormatConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value is DateTime)
      {
        var dateTime = (DateTime)value;
        var formatString = parameter as string;
        return dateTime.ToString(formatString ?? "d");
      }

      return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      if (value is string)
      {
        DateTime date;
        if (DateTime.TryParse(value as string, out date))
        {
          return date;
          //return date.ToString((parameter as string) ?? "f");
        }
      }

      return new DateTime();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
