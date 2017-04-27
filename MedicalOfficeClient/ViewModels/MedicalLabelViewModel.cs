using MedicalOfficeClient.Interfaces;
using MedicalOfficeClient.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MedicalOfficeClient.ViewModels
{
  public class MedicalLabelViewModel : NotificationBase<MedicalItem>, IMedicalItemViewModel
  {
    #region Fields

    //RichTextBlock _block;

    #endregion Fields

    public MedicalLabelViewModel(MedicalItem item, RichTextBlock block) : base(item)
    {
      //_block = block;
      if (This.Type != MedicalItemType.Label)
      {
        This.Type = MedicalItemType.Label;
        This.DateCreated = DateTime.Now;
        This.Content = null;
      }

      //Run myRun1 = new Run();
      //myRun1.Text = "Hello World!";

      //Paragraph myParagraph = new Paragraph();
      //myParagraph.Inlines.Add(myRun1);

      //_block.Blocks.Add(myParagraph);
    }

    #region Properties

    public MedicalItem Item
    {
      get { return This; }
    }

    public DateTime? Created
    {
      get { return This.DateCreated; }
    }

    public DateTime? Changed
    {
      get { return This.DateChanged; }
    }

    public MedicalItemType Type
    {
      get { return This.Type; }
    }

    public Symbol Symbol
    {
      get { return This.Symbol; }
    }

    public SolidColorBrush Color
    {
      get { return This.ColorBrush; }
    }

    public byte[] Content
    {
      get { return This.Content ?? new byte[0]; }
      set { SetProperty(This.Content, value, () => This.Content = value); }
    }

    private BitmapImage _preview;
    public BitmapImage Preview
    {
      get { return _preview ?? (_preview = new BitmapImage()); }
      set { SetProperty(ref _preview, value); }
    }

    #endregion Properties

    public async Task LoadPreviewAsync()
    {
      if (This.Preview != null)
      {
        using (var stream = new InMemoryRandomAccessStream())
        {
          await stream.WriteAsync(This.Preview.AsBuffer());
          stream.Seek(0);
          await Preview.SetSourceAsync(stream);
        }
      }
    }

    private async Task savePreviewAsync(int maxWidth = 256, int maxHeight = 256)
    {
      if (This.Content != null)
      {
        // a4 pixel size 96 dpi = 794 x 1123
        CanvasDevice device = CanvasDevice.GetSharedDevice(true);
        CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, 794, 1123, 96);

        var mergedStream = new InMemoryRandomAccessStream();
        using (var ds = renderTarget.CreateDrawingSession())
        {
          ds.Clear(Colors.White);
          var textContent = Encoding.UTF8.GetString(This.Content);
          //CanvasSolidColorBrush textBrush = new CanvasSolidColorBrush(device, Colors.Red);
          CanvasTextFormat textFormat = new CanvasTextFormat();
          CanvasTextLayout textLayout = new CanvasTextLayout(device, textContent, textFormat, 600, 900);

          ds.DrawTextLayout(textLayout, new Vector2(100, 100), Colors.Black);
        }
        await renderTarget.SaveAsync(mergedStream, CanvasBitmapFileFormat.Jpeg);

        //StorageFile between = await DownloadsFolder.CreateFileAsync("between.jpg", CreationCollisionOption.GenerateUniqueName);
        //using (var fileStream = await between.OpenAsync(FileAccessMode.ReadWrite))
        //{
        //  await RandomAccessStream.CopyAsync(mergedStream, fileStream);
        //}

        // scaling result
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(mergedStream);
        using (var previewStream = new InMemoryRandomAccessStream())
        {
          BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(previewStream, decoder);
          var ratioWidth = (double)maxWidth / decoder.PixelWidth;
          var ratioHeight = (double)maxHeight / decoder.PixelHeight;
          var ratioScale = Math.Min(ratioWidth, ratioHeight);

          var aspectHeight = (uint)(ratioScale * decoder.PixelHeight);
          var aspectWidth = (uint)(ratioScale * decoder.PixelWidth);

          //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
          encoder.BitmapTransform.ScaledHeight = aspectHeight;
          encoder.BitmapTransform.ScaledWidth = aspectWidth;
          await encoder.FlushAsync();

          // write result to preview
          using (var reader = new DataReader(previewStream.GetInputStreamAt(0)))
          {
            This.Preview = new byte[previewStream.Size];
            await reader.LoadAsync((uint)previewStream.Size);
            reader.ReadBytes(This.Preview);
          }

          //StorageFile previewFile = await DownloadsFolder.CreateFileAsync("preview.jpg", CreationCollisionOption.GenerateUniqueName);
          //await FileIO.WriteBytesAsync(previewFile, This.Preview);
        }
      }
    }

    public async Task<bool> SaveMedicalItemToDatabaseAsync()
    {
      await savePreviewAsync();
      await LoadPreviewAsync();
      using (var db = new Database())
      {
        if (This.MedicalItemId == Guid.Empty && This.MedicalCaseId != Guid.Empty)
        {
          var mediCase = await db.MedicalCases.FindAsync(This.MedicalCaseId);
          if (mediCase != null)
          {
            await db.Entry(mediCase).Collection(c => c.Items).LoadAsync();
            mediCase.Items.Add(This);
            await db.SaveChangesAsync();
            return true;
          }
        }
        else
        {
          var mediItem = await db.MedicalItems.FindAsync(This.MedicalItemId);
          if (mediItem != null)
          {
            mediItem.Content = This.Content;
            mediItem.Overlay = This.Overlay;
            mediItem.Preview = This.Preview;
            //db.MedicalItems.Update(This);
            await db.SaveChangesAsync();
            return true;
          }
        }
      }
      return false;
    }

  }
}
