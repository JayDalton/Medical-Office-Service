using MedicalOfficeClient.Interfaces;
using MedicalOfficeClient.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MedicalOfficeClient.ViewModels
{
  public class MedicalImageViewModel : NotificationBase<MedicalItem>, IMedicalItemViewModel
  {
    public MedicalImageViewModel(MedicalItem item, InkCanvas inkCanvas = null) : base(item)
    {
      OverlayCanvas = inkCanvas;
      Elements = new ObservableCollection<TextElement>();
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

    private BitmapImage _content;
    public BitmapImage Content
    {
      get { return _content ?? (_content = new BitmapImage()); }
      set { SetProperty(ref _content, value); }
    }

    private BitmapImage _preview;
    public BitmapImage Preview
    {
      get { return _preview ?? (_preview = new BitmapImage()); }
      set { SetProperty(ref _preview, value); }
    }

    private InkCanvas _overlayCanvas;
    public InkCanvas OverlayCanvas
    {
      get { return _overlayCanvas; }
      set { SetProperty(ref _overlayCanvas, value); }
    }

    private ObservableCollection<TextElement> _elements;
    public ObservableCollection<TextElement> Elements
    {
      get { return _elements; }
      set { SetProperty(ref _elements, value); }
    }

    #endregion Properties

    public async Task InitializeAsync()
    {
      await loadContentAsync();
      await loadOverlayAsync();
      await loadElementsAsync();
    }

    private async Task loadContentAsync()
    {
      if (This.Content != null)
      {
        using (var stream = new InMemoryRandomAccessStream())
        {
          await stream.WriteAsync(This.Content.AsBuffer());
          stream.Seek(0);
          await Content.SetSourceAsync(stream);
        }
      }
    }

    // Laden des Overlay von Item nach InkCanvas
    private async Task loadOverlayAsync()
    {
      if (This.Overlay != null && _overlayCanvas?.InkPresenter?.StrokeContainer != null)
      {
        var tempCanvas = _overlayCanvas ?? new InkCanvas();
        using (var stream = new InMemoryRandomAccessStream())
        {
          await stream.WriteAsync(This.Overlay.AsBuffer());
          using (var inputStream = stream.GetInputStreamAt(0))
          {
            tempCanvas.InkPresenter.StrokeContainer.Clear();
            await tempCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
          }
        }
      }
    }

    // Speichert Overlay von InkCanvas nach Item
    private async Task saveOverlayAsync()
    {
      if (_overlayCanvas != null && 0 < _overlayCanvas.InkPresenter.StrokeContainer.GetStrokes().Count)
      {
        using (var stream = new InMemoryRandomAccessStream())
        {
          await _overlayCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
          using (var reader = new DataReader(stream.GetInputStreamAt(0)))
          {
            This.Overlay = new byte[stream.Size];
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(This.Overlay);
          }
        }
      }
      else
      {
        This.Overlay = null;
      }
    }

    private async Task loadElementsAsync()
    {
      Elements.Clear();
      if (This.Element != null)
      {
        using (var stream = new InMemoryRandomAccessStream())
        {
          await stream.WriteAsync(This.Element.AsBuffer());
          using (var reader = new DataReader(stream.GetInputStreamAt(0)))
          {
            await reader.LoadAsync((uint)stream.Size);
            while (reader.UnconsumedBufferLength > 0)
            {
              var bytes = new byte[reader.ReadUInt32()];
              reader.ReadBytes(bytes);
              TextElement element;
              if (TextElement.TryParse(bytes, out element))
              {
                Elements.Add(element);
              }
            }
          }
        }
      }
    }

    private async Task saveElementAsync()
    {
      if (Elements.Count > 0)
      {
        using (var stream = new InMemoryRandomAccessStream())
        {
          using (var writer = new DataWriter(stream))
          {
            foreach (var elem in Elements)
            {
              if (elem.Content != null)
              {
                var e = elem.ToBinary();
                writer.WriteUInt32((uint)e.Length);
                writer.WriteBytes(e);
                await writer.StoreAsync();
              }
            }
            using (var reader = new DataReader(stream.GetInputStreamAt(0)))
            {
              This.Element = new byte[stream.Size];
              await reader.LoadAsync((uint)stream.Size);
              reader.ReadBytes(This.Element);
            }
          }
        }
      }
      else
      {
        This.Element = null;
      }
    }

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

    public async Task<InMemoryRandomAccessStream> CreateContentBitmapAsync()
    {
      var mergedStream = new InMemoryRandomAccessStream();
      if (This.Content != null)
      {
        CanvasDevice device = CanvasDevice.GetSharedDevice(true);

        // Content-Bild laden
        var contentStream = new InMemoryRandomAccessStream();
        await contentStream.WriteAsync(This.Content.AsBuffer());
        var contentImage = await CanvasBitmap.LoadAsync(device, contentStream);
        var renderTarget = new CanvasRenderTarget(device,
          contentImage.SizeInPixels.Width,
          contentImage.SizeInPixels.Height,
          96
        );

        // merge content + overlay + elements
        using (var ds = renderTarget.CreateDrawingSession())
        {
          ds.DrawImage(contentImage);

          // Ink overlay
          if (OverlayCanvas?.InkPresenter?.StrokeContainer?.GetStrokes().Count > 0)
          {
            ds.DrawInk(OverlayCanvas.InkPresenter.StrokeContainer.GetStrokes());
          }

          // TextBox elements
          foreach (var elem in Elements)
          {
            if (elem.Content != null)
            {
              var position = new Rect(
                new Point(elem.Matrix.X, elem.Matrix.Y),
                new Size(elem.Width, elem.Height)
              );

              var format = new CanvasTextFormat()
              {
                FontSize = 15,
                FontFamily = "Verdana",
                FontWeight = FontWeights.Bold,
                WordWrapping = CanvasWordWrapping.Wrap
              };

              ds.DrawText(elem.Content, position, Colors.DarkBlue, format);
            }
          }
        }
        await renderTarget.SaveAsync(mergedStream, CanvasBitmapFileFormat.Jpeg, 1.0f);
      }
      return mergedStream;
    }

    private async Task savePreviewAsync(int maxWidth = 256, int maxHeight = 256)
    {
      using (var mergedStream = await CreateContentBitmapAsync())
      {
        if (mergedStream.Size > 0)
        {
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
            This.Preview = new byte[previewStream.Size];
            using (var reader = new DataReader(previewStream.GetInputStreamAt(0)))
            {
              await reader.LoadAsync((uint)previewStream.Size);
              reader.ReadBytes(This.Preview);
            }
          }
        }
        else
        {
          This.Preview = null;
        }
      }
    }



    public async Task<bool> SaveMedicalItemToDatabaseAsync()
    {
      await saveOverlayAsync();
      await saveElementAsync();
      await savePreviewAsync();
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
            mediItem.Element = This.Element;
            //db.MedicalItems.Update(This);
            await db.SaveChangesAsync();
            return true;
          }
        }
      }
      return false;
    }
  }

  public static class InkCanvasExtensions
  {
    /// <summary>
    /// Captures the ink from an <see cref="InkCanvas"/> control.
    /// </summary>
    /// <param name="canvas">
    /// The <see cref="InkCanvas"/> control.
    /// </param>
    /// <param name="rootRenderElement">
    /// A <see cref="FrameworkElement"/> which wraps the canvas.
    /// </param>
    /// <param name="encoderId">
    /// A <see cref="BitmapEncoder"/> ID to use to render the image.
    /// </param>
    /// <returns>
    /// Returns an awaitable task.
    /// </returns>
    public static async Task<StorageFile> CaptureInkAsImageAsync(
        this InkCanvas canvas,
        FrameworkElement rootRenderElement,
        Guid encoderId)
    {
      var targetFile =
              await
              ApplicationData.Current.TemporaryFolder.CreateFileAsync(
                  $"{Guid.NewGuid()}.png",
                  CreationCollisionOption.ReplaceExisting);

      if (targetFile != null)
      {
        var renderBitmap = new RenderTargetBitmap();
        await renderBitmap.RenderAsync(rootRenderElement);

        var bitmapSizeAt96Dpi = new Size(renderBitmap.PixelWidth, renderBitmap.PixelHeight);

        var pixels = await renderBitmap.GetPixelsAsync();

        var win2DDevice = CanvasDevice.GetSharedDevice();

        using (
            var target = new CanvasRenderTarget(
                win2DDevice,
                (float)rootRenderElement.ActualWidth,
                (float)rootRenderElement.ActualHeight,
                96.0f))
        {
          using (var drawingSession = target.CreateDrawingSession())
          {
            using (
                var canvasBitmap = CanvasBitmap.CreateFromBytes(
                    win2DDevice,
                    pixels,
                    (int)bitmapSizeAt96Dpi.Width,
                    (int)bitmapSizeAt96Dpi.Height,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    96.0f))
            {
              drawingSession.DrawImage(
                  canvasBitmap,
                  new Rect(0, 0, target.SizeInPixels.Width, target.SizeInPixels.Height),
                  new Rect(0, 0, bitmapSizeAt96Dpi.Width, bitmapSizeAt96Dpi.Height));
            }
            drawingSession.Units = CanvasUnits.Pixels;
            drawingSession.DrawInk(canvas.InkPresenter.StrokeContainer.GetStrokes());
          }

          using (var stream = await targetFile.OpenAsync(FileAccessMode.ReadWrite))
          {
            var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                (uint)renderBitmap.PixelWidth,
                (uint)renderBitmap.PixelHeight,
                logicalDpi,
                logicalDpi,
                target.GetPixelBytes());

            await encoder.FlushAsync();
          }
        }
      }

      return targetFile;
    }
  }

  public class TextElement : NotificationBase
  {
    public string Content { get; set; }
    public TranslateTransform Matrix { get; set; }

    private double _width;
    public double Width
    {
      get { return _width; }
      set { SetProperty(ref _width, value); }
    }

    private double _height;
    public double Height
    {
      get { return _height; }
      set { SetProperty(ref _height, value); }
    }

    public byte[] ToBinary()
    {
      using (MemoryStream ms = new MemoryStream())
      {
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
          var bytes = Encoding.UTF8.GetBytes(Content);
          writer.Write(Matrix.X);
          writer.Write(Matrix.Y);
          writer.Write(Width);
          writer.Write(Height);
          writer.Write(bytes.Length);
          writer.Write(bytes);
          return ms.ToArray();
        }
      }
    }

    public static TextElement Parse(byte[] value)
    {
      using (MemoryStream ms = new MemoryStream(value))
      {
        using (BinaryReader reader = new BinaryReader(ms))
        {
          var x = reader.ReadDouble();
          var y = reader.ReadDouble();
          return new TextElement()
          {
            Matrix = new TranslateTransform() { X = x, Y = y },
            Width = reader.ReadDouble(),
            Height = reader.ReadDouble(),
            Content = Encoding.UTF8.GetString(
              reader.ReadBytes(reader.ReadInt32())
            ),
          };
        }
      }
    }

    public static bool TryParse(byte[] value, out TextElement element)
    {
      try
      {
        element = TextElement.Parse(value);
        return true;
      }
      catch (Exception)
      {
        element = new TextElement();
        return false;
      }
    }

  }
}
