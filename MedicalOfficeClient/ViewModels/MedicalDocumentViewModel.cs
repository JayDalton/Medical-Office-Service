using MedicalOfficeClient.Interfaces;
using MedicalOfficeClient.Services;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MedicalOfficeClient.ViewModels
{
  public class MedicalDocumentViewModel : NotificationBase<MedicalItem>, IMedicalItemViewModel
  {
    #region Fields

    private bool _progressing;
    private PdfDocument _pdfDocument;
    private uint _pageIndex, _pageCount;

    const int WrongPassword = unchecked((int)0x8007052b); // HRESULT_FROM_WIN32(ERROR_WRONG_PASSWORD)
    const int GenericFail = unchecked((int)0x80004005);   // E_FAIL

    #endregion Fields

    public MedicalDocumentViewModel(MedicalItem item) : base(item)
    {
      _contentBitmaps = new ObservableCollection<SimpleFlipPage>();
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

    public string Title
    {
      get { return This.Label; }
      set { SetProperty(This.Label, value, () => This.Label = value); }
    }

    public bool Progressing
    {
      get { return _progressing; }
      set { SetProperty(ref _progressing, value); }
    }

    public uint PageCount
    {
      get { return _pageCount; }
      set { SetProperty(ref _pageCount, value); }
    }

    public uint PageIndex
    {
      get { return _pageIndex; }
      set { SetProperty(ref _pageIndex, value); }
    }

    private ObservableCollection<SimpleFlipPage> _contentBitmaps;
    public ObservableCollection<SimpleFlipPage> ContentBitmaps
    {
      get { return _contentBitmaps; }
      set { SetProperty(ref _contentBitmaps, value); }
    }

    private BitmapImage _preview;
    public BitmapImage Preview
    {
      get { return _preview ?? (_preview = new BitmapImage()); }
      set { SetProperty(ref _preview, value); }
    }

    #endregion Properties

    #region Methods

    public PdfPage GetPage(uint index)
    {
      return _pdfDocument.GetPage(index);
    }

    public async Task LoadDocumentAsync()
    {
      await loadDocumentAsync();
      await loadContentAsync();
    }

    private async Task loadDocumentAsync()
    {
      try
      {
        var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(This.Content.AsBuffer());
        _pdfDocument = await PdfDocument.LoadFromStreamAsync(stream);
      }
      catch (Exception ex)
      {
        _pdfDocument = null;
        switch (ex.HResult)
        {
          case WrongPassword:
            Debug.WriteLine("Document is password-protected and password is incorrect.");
            break;

          case GenericFail:
            Debug.WriteLine("Document is not a valid PDF.");
            break;

          default:
            // File I/O errors are reported as exceptions.
            Debug.WriteLine(ex.Message);
            break;
        }
      }
      if (_pdfDocument.IsPasswordProtected)
      {
        Debug.WriteLine("Document is password protected.");
      }
    }

    private async Task loadContentAsync()
    {
      if (_pdfDocument != null)
      {
        ContentBitmaps.Clear();
        PageIndex = default(uint);
        PageCount = _pdfDocument.PageCount;
        var overlays = await LoadOverlayAsync();
        for (uint idx = 0; idx < PageCount; idx++)
        {
          using (var page = _pdfDocument.GetPage(idx))
          {
            var bitmapImage = new BitmapImage();
            using (var bmpStream = new InMemoryRandomAccessStream())
            {
              await page.RenderToStreamAsync(bmpStream);
              await bitmapImage.SetSourceAsync(bmpStream);
            }

            var inkStrokes = new InkStrokeContainer();
            if (overlays.ContainsKey(idx))
            {
              using (var inkStream = new InMemoryRandomAccessStream())
              {
                await inkStream.WriteAsync(overlays[idx].AsBuffer());
                await inkStrokes.LoadAsync(inkStream.GetInputStreamAt(0));
              }
            }

            ContentBitmaps.Add(
              new SimpleFlipPage
              {
                Index = idx,
                Image = bitmapImage,
                InkStrokes = inkStrokes
              }
            );

          }
        }
      }
    }

    public async Task<Dictionary<uint, byte[]>> LoadOverlayAsync()
    {
      var result = new Dictionary<uint, byte[]>();
      if (This.Overlay != null)
      {
        using (var stream = new InMemoryRandomAccessStream())
        {
          await stream.WriteAsync(This.Overlay.AsBuffer());
          using (var reader = new DataReader(stream.GetInputStreamAt(0)))
          {
            await reader.LoadAsync((uint)stream.Size);
            while (0 < reader.UnconsumedBufferLength)
            {
              var index = reader.ReadUInt32();
              var length = reader.ReadUInt32();
              var content = reader.ReadBuffer(length);
              result[index] = content.ToArray();
            }
          }
        }
      }
      return result;
    }

    private async Task saveOverlayAsync()
    {
      using (var overlayStream = new InMemoryRandomAccessStream())
      {
        using (var writer = new DataWriter(overlayStream))
        {
          foreach (var page in _contentBitmaps)
          {
            if (0 < page.InkStrokes.GetStrokes().Count)
            {
              using (var strokeStream = new InMemoryRandomAccessStream())
              {
                writer.WriteUInt32(page.Index);
                await page.InkStrokes.SaveAsync(strokeStream);
                var strokeBytes = new byte[strokeStream.Size];
                using (var reader = new DataReader(strokeStream.GetInputStreamAt(0)))
                {
                  await reader.LoadAsync((uint)strokeStream.Size);
                  reader.ReadBytes(strokeBytes);
                }
                writer.WriteUInt32((uint)strokeBytes.Length);
                writer.WriteBuffer(strokeBytes.AsBuffer());
              }
            }
          }
          await writer.StoreAsync();
          await writer.FlushAsync();
          if (0 < overlayStream.Size)
          {
            This.Overlay = new byte[overlayStream.Size];
            using (var reader = new DataReader(overlayStream.GetInputStreamAt(0)))
            {
              await reader.LoadAsync((uint)overlayStream.Size);
              reader.ReadBytes(This.Overlay);
            }
          }
          else
          {
            This.Overlay = null;
          }
        }
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

    private async Task savePreviewAsync(int maxWidth = 256, int maxHeight = 256)
    {
      if (_pdfDocument != null && 0 < _pdfDocument.PageCount)
      {
        CanvasDevice device = CanvasDevice.GetSharedDevice(true);

        // Content-Bild laden
        var page = _pdfDocument.GetPage(0);
        var contentStream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(contentStream);
        var contentImage = await CanvasBitmap.LoadAsync(device, contentStream);

        CanvasRenderTarget renderTarget = new CanvasRenderTarget(device,
          contentImage.SizeInPixels.Width,
          contentImage.SizeInPixels.Height,
          96
        );

        // merge content + overlay
        var mergedStream = new InMemoryRandomAccessStream();
        using (var ds = renderTarget.CreateDrawingSession())
        {
          ds.DrawImage(contentImage);
          var overlays = await LoadOverlayAsync();
          if (overlays.ContainsKey(0))
          {
            InkCanvas inkCanvas = new InkCanvas();
            var inkStream = new InMemoryRandomAccessStream();
            await inkStream.WriteAsync(overlays[0].AsBuffer());
            await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inkStream.GetInputStreamAt(0));
            ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
          }
        }
        await renderTarget.SaveAsync(mergedStream, CanvasBitmapFileFormat.Jpeg);

        StorageFile between = await DownloadsFolder.CreateFileAsync("between.jpg", CreationCollisionOption.GenerateUniqueName);
        using (var fileStream = await between.OpenAsync(FileAccessMode.ReadWrite))
        {
          await RandomAccessStream.CopyAsync(mergedStream, fileStream);
        }

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

          StorageFile previewFile = await DownloadsFolder.CreateFileAsync("preview.jpg", CreationCollisionOption.GenerateUniqueName);
          await FileIO.WriteBytesAsync(previewFile, This.Preview);
        }
      }
    }

    public async Task<bool> SaveMedicalItemToDatabaseAsync()
    {
      await saveOverlayAsync();
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

    #endregion Methods
  }

  public class SimpleFlipPage
  {
    public uint Index { get; set; }
    public BitmapImage Image { get; set; }
    public InkStrokeContainer InkStrokes { get; set; }
  }
}
