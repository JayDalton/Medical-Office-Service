using MedicalOfficeClient.Interfaces;
using MedicalOfficeClient.Services;
using MedicalOfficeClient.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;

namespace MedicalOfficeClient.ViewModels
{
  public class MedicalCaseViewModel : NotificationBase<MedicalCase>
  {
    #region Fields
    #endregion Fields

    public MedicalCaseViewModel(MedicalCase medical = null) : base(medical)
    {
      _caseItems = new ObservableCollection<IMedicalItemViewModel>();
    }

    #region Properties

    public MedicalCase Case
    {
      get { return This; }
    }

    public string Person
    {
      get { return This.Person?.Name; }
    }

    public DateTime? Created
    {
      get { return This.DateCreated; }
    }

    public DateTime? Changed
    {
      get { return This.DateChanged; }
    }

    public string Label
    {
      get { return This.Label ?? string.Empty; }
      set { SetProperty(This.Label, value, () => This.Label = value); }
    }

    public SolidColorBrush Color
    {
      get { return This.ColorBrush; }
    }

    private ObservableCollection<IMedicalItemViewModel> _caseItems;
    public ObservableCollection<IMedicalItemViewModel> CaseItems
    {
      get { return _caseItems; }
      set { SetProperty(ref _caseItems, value); }
    }

    #endregion Properties

    #region Methods

    public async Task LoadCaseFromDatabaseAsync()
    {
      await LoadCaseFromDatabaseAsync(This.MedicalCaseId);
    }

    public async Task LoadCaseFromDatabaseAsync(Guid guid)
    {
      using (var db = new Database())
      {
        This = await db.MedicalCases.FindAsync(guid);
        if (This != null)
        {
          await db.Entry(This).Collection(c => c.Items).LoadAsync();
          await db.Entry(This).Reference(c => c.Person).LoadAsync();
          RaisePropertyChanged(nameof(Changed));
          RaisePropertyChanged(nameof(Created));
          RaisePropertyChanged(nameof(Person));
          RaisePropertyChanged(nameof(Label));
          RaisePropertyChanged(nameof(Color));
          await InitializeItemsPreviewAsync();
        }
      }
    }

    public async Task RemoveCaseItemFromDatabase(IMedicalItemViewModel vm)
    {
      using (var db = new Database())
      {
        var mediItem = await db.MedicalItems.FindAsync(vm.Item.MedicalItemId);
        if (mediItem != null)
        {
          db.MedicalItems.Remove(mediItem);
          await db.SaveChangesAsync();
          CaseItems.Remove(vm);
        }
      }
    }

    public async Task SaveCaseToDatabase()
    {
      using (var db = new Database())
      {
        //var mediCase = await db.MedicalCases.FindAsync(This.MedicalCaseId);
        //if (mediCase != null)
        //{
        //  mediCase.Label = This.Label;
        //}
        //This.Changed = null;
        db.MedicalCases.Update(This);
        await db.SaveChangesAsync();
      }
    }

    public MedicalItem CreateItemFromType(MedicalItemType type)
    {
      return new MedicalItem
      {
        Type = type,
        Label = string.Format("new {0}", type),
        MedicalCaseId = This.MedicalCaseId
      };
    }

    public async Task<MedicalItem> CreateItemFromFileAsync(StorageFile inputFile)
    {
      var item = new MedicalItem { MedicalCaseId = This.MedicalCaseId };
      using (var fileStream = await inputFile.OpenReadAsync())
      {
        switch (inputFile.ContentType)
        {
          case "image/png":
          case "image/jpeg":
            item.Type = MedicalItemType.Image;
            item.Label = string.Format("Image: {0} | {1}", inputFile.DisplayName, inputFile.DisplayType);
            break;
          case "application/pdf":
            item.Type = MedicalItemType.Document;
            item.Label = string.Format("Document: {0} | {1}", inputFile.DisplayName, inputFile.DisplayType);
            break;
        }

        item.Content = new byte[fileStream.Size];
        using (DataReader reader = new DataReader(fileStream.GetInputStreamAt(0)))
        {
          await reader.LoadAsync((uint)fileStream.Size);
          reader.ReadBytes(item.Content);

          //StorageFile sampleFile = await DownloadsFolder.CreateFileAsync("sample.pdf");
          //await FileIO.WriteBytesAsync(sampleFile, item.Content);

          return item;
        }
      }
    }

    public async Task DeleteAllMedicalItems()
    {
      using (var db = new Database())
      {
        db.MedicalItems.RemoveRange(This.Items);
        await db.SaveChangesAsync();
        await LoadCaseFromDatabaseAsync();
      }
    }

    private async Task InitializeItemsPreviewAsync()
    {
      if (This.Items != null)
      {
        CaseItems.Clear();
        foreach (var item in This.Items)
        {
          IMedicalItemViewModel viewModel = null;
          switch (item.Type)
          {
            case MedicalItemType.Label:
              viewModel = new MedicalLabelViewModel(item, null);
              break;
            case MedicalItemType.Image:
              viewModel = new MedicalImageViewModel(item, null);
              break;
            case MedicalItemType.Document:
              viewModel = new MedicalDocumentViewModel(item);
              break;
          }
          await viewModel.LoadPreviewAsync();
          CaseItems.Add(viewModel);
        }
      }
    }

    #endregion Methods

  }
}