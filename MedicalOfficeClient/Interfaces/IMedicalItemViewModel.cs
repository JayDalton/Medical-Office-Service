using MedicalOfficeClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MedicalOfficeClient.Interfaces
{
  public interface IMedicalItemViewModel
  {
    MedicalItem Item { get; }
    MedicalItemType Type { get; }
    DateTime? Created { get; }
    DateTime? Changed { get; }
    Symbol Symbol { get; }
    BitmapImage Preview { get; }
    SolidColorBrush Color { get; }
    Task LoadPreviewAsync();
  }

}
