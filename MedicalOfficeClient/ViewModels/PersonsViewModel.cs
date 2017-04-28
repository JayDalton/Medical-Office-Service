using MedicalOfficeClient.Services;
using MedicalOfficeClient.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace MedicalOfficeClient.ViewModels
{
  public class PersonsViewModel : NotificationBase
  {
    #region Fields

    private static Random random = new Random(DateTime.Now.Millisecond);

    #endregion Fields

    public PersonsViewModel()
    {
    }

    #region Properties

    private IEnumerable<PersonGroup> _groupedPerson;
    public IEnumerable<PersonGroup> GroupedPersons
    {
      get { return _groupedPerson ?? new ObservableCollection<PersonGroup>(); }
      private set { SetProperty(ref _groupedPerson, value); }
    }

    private PersonViewModel _createPerson;
    public PersonViewModel PersonToCreateOrEdit
    {
      get { return _createPerson ?? default(PersonViewModel); }
      set { SetProperty(ref _createPerson, value); }
    }

    private PersonViewModel _detailPerson;
    public PersonViewModel PersonToViewDetails
    {
      get { return _detailPerson ?? default(PersonViewModel); }
      set { SetProperty(ref _detailPerson, value); }
    }

    #endregion Properties

    public async Task InitializeAsync()
    {
      await InitializeContextAsync();
      await InitializeContentAsync();
      //await SaveContextToJsonAsync();
    }

    public async Task InitializeContextAsync()
    {
      // Wenn keine Einträge -> DB füllen
      using (var db = new Database())
      {
        if (0 == await db.Persons.CountAsync())
        {
          var persons = await getRandomPersons(5);
          foreach (var person in persons)
          {
            db.Persons.Add(person);
            db.SaveChanges();
          }
        }
      }
    }

    public async Task InitializeContentAsync()
    {
      using (var db = new Database())
      {
        // Personen laden
        var persons = await db.Persons.ToListAsync();

        // Gruppen bilden
        GroupedPersons = persons
          .Select(p => new PersonViewModel(p))
          .OrderBy(p => p.LastName)
          .GroupBy(p => p.LastName[0].ToString(), (key, list) => new PersonGroup(key, list))
        ;

        //var vm = GroupedPersons.FirstOrDefault().FirstOrDefault();
        //await vm.LoadPreviewAsync();
        //PersonToViewDetails = vm;
      }
    }

    public async Task SaveContextToJsonAsync()
    {
      using (var db = new Database())
      {
        StorageFolder folder = ApplicationData.Current.LocalFolder;
        StorageFile file = await folder.CreateFileAsync("persons.json", CreationCollisionOption.ReplaceExisting);

        var jsonArray = new JsonArray();
        var jsonObject = new JsonObject();

        //string content = await FileIO.ReadTextAsync(file);
        //JsonObject.TryParse(content, out jsonObject);
        //if (jsonObject.ContainsKey("FakePersons"))
        //{
        //  // Inhalt lesen
        //  foreach (var person in jsonObject.GetNamedArray("FakePersons"))
        //  {
        //    jsonArray.Add(person);
        //  }
        //}

        var persons = db.Persons
          .Include(person => person.Cases)
          .ThenInclude(Cases => Cases.Items)
          .ToList();

        // Inhalte mergen
        foreach (var person in persons)
        {
          //var entry = person.ToJsonObject();
          //if (!jsonArray.Contains(entry))
          //{
          //  jsonArray.Add(entry);
          //}
        }

        // Inhalte schreiben
        jsonObject["FakePersons"] = jsonArray;
        await FileIO.WriteTextAsync(file, jsonObject.Stringify());
      }
    }

    public async Task SaveCreatedPerson()
    {
      if (!string.IsNullOrEmpty(PersonToCreateOrEdit.LastName) || !string.IsNullOrEmpty(PersonToCreateOrEdit.FirstName))
      {
        using (var db = new Database())
        {
          var person = await db.Persons.FindAsync(PersonToCreateOrEdit.PersonId);
          if (person != null)
          {
            person.LastName = PersonToCreateOrEdit.LastName;
            person.FirstName = PersonToCreateOrEdit.FirstName;
            person.Birthday = PersonToCreateOrEdit.Birthday;
          }
          else
          {
            await db.Persons.AddAsync(PersonToCreateOrEdit);
          }

          await db.SaveChangesAsync();
          await InitializeContentAsync();
        }
      }
    }

    private async Task<IEnumerable<Person>> getRandomPersons(int number = 50)
    {
      var result = new List<Person>();
      Random random = new Random(DateTime.Now.Millisecond);

      var lastNames = new List<string>() {
        "Vogel", "Grüner", "Meissner", "Hofmeyer", "Miller", "Müller", "Schmidt", "Schneider",
        "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann", "Schäfer", "Bauer",
        "Koch", "Richter", "Schröder", "Neumann", "Krause",
      };

      var firstNames = new List<string>() {
        "Dennis", "Melanie", "Christian", "Daniela", "Martin", "Maria", "Peter", "Monika", "Wolfgang",
        "Ursula", "Michael", "Petra", "Werner", "Elisabeth", "Klaus", "Sabine", "Thomas", "Renate",
        "Manfred", "Helga", "Helmut", "Karin", "Jürgen", "Brigitte", "Heinz", "Ingrid", "Gerhard",
        "Erika", "Andreas", "Andrea", "Hans", "Gisela", "Josef", "Claudia", "Günther", "Susanne",
        "Dieter", "Christine",
      };

      for (int i = 0; i < number; i++)
      {
        result.Add(new Person
        {
          LastName = lastNames[random.Next(lastNames.Count)],
          FirstName = firstNames[random.Next(firstNames.Count)],
          Birthday = new DateTime(
            random.Next(1995, 2010),
            random.Next(1, 12),
            random.Next(1, 27)
          ),
          Cases = await getRandomMedicalCases(random)
        });
      }
      return result;
    }

    private async Task<List<MedicalCase>> getRandomMedicalCases(Random random, int number = 2)
    {
      var result = new List<MedicalCase>();
      var types = Enum.GetValues(typeof(MedicalCaseType));
      for (int i = 0; i < number; i++)
      {
        result.Add(new MedicalCase
        {
          Type = (MedicalCaseType)types.GetValue(random.Next(types.Length)),
          Label = string.Format("Default Medical Case {0}", i),
          Items = await getRandomMedicalItems(random)
        });
      }
      return result;
    }

    private async Task<List<MedicalItem>> getRandomMedicalItems(Random random, int number = 5)
    {
      var result = new List<MedicalItem>();
      var types = Enum.GetValues(typeof(MedicalItemType));
      for (int i = 0; i < number; i++)
      {
        //var itemModel = new MedicalItem { Type = MedicalItemType.Label };
        var itemModel = new MedicalItem { Type = (MedicalItemType)types.GetValue(random.Next(types.Length)) };
        switch (itemModel.Type)
        {
          case MedicalItemType.Label:
            itemModel.Content = Encoding.UTF8.GetBytes(GetBiography());
            break;
          case MedicalItemType.Image:
            itemModel.Content = await getRandomBitmapAsync(random);
            break;
          case MedicalItemType.Document:
            itemModel.Content = await getRandomDocumentAsync(random);
            break;
        }
        result.Add(itemModel);
      }
      return result;
    }

    private string getRandomText(Random random)
    {
      StringBuilder builder = new StringBuilder();
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
      var wordCount = random.Next(15, 25);
      for (int i = 0; i < wordCount; i++)
      {
        var wordLength = random.Next(3, 8);
        builder.Append(new string(Enumerable.Repeat(chars, wordLength)
        .Select(s => s[random.Next(s.Length)]).ToArray())).Append(" ");
      }

      return builder.ToString();
    }

    private async Task<byte[]> getRandomBitmapArray(Random random, int width = 400, int height = 400)
    {
      // random bytes
      var randomBytes = new byte[width * height * 4]; // BGRA
      random.NextBytes(randomBytes);

      // create bitmap
      WriteableBitmap writeableBitmap = new WriteableBitmap(width, height);
      var bufferStream = writeableBitmap.PixelBuffer.AsStream();
      await bufferStream.WriteAsync(randomBytes, 0, randomBytes.Length);

      // neues Bild generieren?
      SoftwareBitmap outputBitmap = SoftwareBitmap.CreateCopyFromBuffer(
          writeableBitmap.PixelBuffer,
          BitmapPixelFormat.Bgra8,
          writeableBitmap.PixelWidth,
          writeableBitmap.PixelHeight,
          BitmapAlphaMode.Premultiplied
      );

      // darstellung
      //var bitmapSource = new SoftwareBitmapSource();
      //await bitmapSource.SetBitmapAsync(outputBitmap);
      //RandomImage = bitmapSource;

      // save image to file
      //SaveSoftwareBitmapToFile(outputBitmap);

      // read byte array and return
      using (var stream = new InMemoryRandomAccessStream())
      {
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
        encoder.SetSoftwareBitmap(outputBitmap);
        await encoder.FlushAsync();

        byte[] bitmapBytes = new byte[stream.Size];
        using (DataReader reader = new DataReader(stream.GetInputStreamAt(0)))
        {
          await reader.LoadAsync((uint)stream.Size);
          reader.ReadBytes(bitmapBytes);
        }
        return bitmapBytes;
      }
    }

    private async Task<byte[]> getRandomBitmapAsync(Random random)
    {
      StorageFolder appInstalledFolder = Package.Current.InstalledLocation;
      StorageFolder assets = await appInstalledFolder.GetFolderAsync("Assets\\Bitmaps");
      var files = await assets.GetFilesAsync();

      var input = files[random.Next(files.Count)];
      using (var fileStream = await input.OpenReadAsync())
      {
        var bytes = new byte[fileStream.Size];
        using (var reader = new DataReader(fileStream.GetInputStreamAt(0)))
        {
          await reader.LoadAsync((uint)fileStream.Size);
          reader.ReadBytes(bytes);
          return bytes;
        }
      }
    }

    private async Task<byte[]> getRandomDocumentAsync(Random random)
    {
      // hinterlegtes Bild öffnen
      StorageFolder appInstalledFolder = Package.Current.InstalledLocation;
      StorageFolder assets = await appInstalledFolder.GetFolderAsync("Assets\\Documents");
      var files = await assets.GetFilesAsync();
      //Uri[] files = {
      //  new Uri(@"ms-appx:///Assets/Logo/pdf_example01.pdf"),
      //  new Uri(@"ms-appx:///Assets/Logo/pdf_example02.pdf"),
      //  new Uri(@"ms-appx:///Assets/Logo/pdf_example03.pdf"),
      //  new Uri(@"ms-appx:///Assets/Logo/pdf_example04.pdf"),
      //  new Uri(@"ms-appx:///Assets/Logo/pdf_example05.pdf")
      //};

      var input = files[random.Next(files.Count)];
      //var input = await StorageFile.GetFileFromApplicationUriAsync(file);
      using (var fileStream = await input.OpenReadAsync())
      {
        var bytes = new byte[fileStream.Size];
        using (var reader = new DataReader(fileStream.GetInputStreamAt(0)))
        {
          await reader.LoadAsync((uint)fileStream.Size);
          reader.ReadBytes(bytes);
          return bytes;
        }
      }
    }

    #region Helpers
    private static string GeneratePosition()
    {
      List<string> positions = new List<string>() { "Program Manager", "Developer", "Product Manager", "Evangelist" };
      return positions[random.Next(0, positions.Count)];
    }
    private static string GetBiography()
    {
      List<string> biographies = new List<string>()
            {
                @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer id facilisis lectus. Cras nec convallis ante, quis pulvinar tellus. Integer dictum accumsan pulvinar. Pellentesque eget enim sodales sapien vestibulum consequat.",
                @"Maecenas eu sapien ac urna aliquam dictum.",
                @"Nullam eget mattis metus. Donec pharetra, tellus in mattis tincidunt, magna ipsum gravida nibh, vitae lobortis ante odio vel quam.",
                @"Quisque accumsan pretium ligula in faucibus. Mauris sollicitudin augue vitae lorem cursus condimentum quis ac mauris. Pellentesque quis turpis non nunc pretium sagittis. Nulla facilisi. Maecenas eu lectus ante. Proin eleifend vel lectus non tincidunt. Fusce condimentum luctus nisi, in elementum ante tincidunt nec.",
                @"Aenean in nisl at elit venenatis blandit ut vitae lectus. Praesent in sollicitudin nunc. Pellentesque justo augue, pretium at sem lacinia, scelerisque semper erat. Ut cursus tortor at metus lacinia dapibus.",
                @"Ut consequat magna luctus justo egestas vehicula. Integer pharetra risus libero, et posuere justo mattis et.",
                @"Proin malesuada, libero vitae aliquam venenatis, diam est faucibus felis, vitae efficitur erat nunc non mauris. Suspendisse at sodales erat.",
                @"Aenean vulputate, turpis non tincidunt ornare, metus est sagittis erat, id lobortis orci odio eget quam. Suspendisse ex purus, lobortis quis suscipit a, volutpat vitae turpis.",
                @"Duis facilisis, quam ut laoreet commodo, elit ex aliquet massa, non varius tellus lectus et nunc. Donec vitae risus ut ante pretium semper. Phasellus consectetur volutpat orci, eu dapibus turpis. Fusce varius sapien eu mattis pharetra.",
                @"Nam vulputate eu erat ornare blandit. Proin eget lacinia erat. Praesent nisl lectus, pretium eget leo et, dapibus dapibus velit. Integer at bibendum mi, et fringilla sem."
            };
      return biographies[random.Next(0, biographies.Count)];
    }

    private static string GeneratePhoneNumber()
    {
      return string.Format("{0:(###)} {1:###}-{2:####}", random.Next(100, 999), random.Next(100, 999), random.Next(1000, 9999));
    }
    private static string GenerateFirstName()
    {
      List<string> names = new List<string>() { "Lilly", "Mukhtar", "Sophie", "Femke", "Abdul-Rafi'", "Chirag-ud-D...", "Mariana", "Aarif", "Sara", "Ibadah", "Fakhr", "Ilene", "Sardar", "Hanna", "Julie", "Iain", "Natalia", "Henrik", "Rasa", "Quentin", "Gadi", "Pernille", "Ishtar", "Jimme", "Justina", "Lale", "Elize", "Rand", "Roshanara", "Rajab", "Bijou", "Marcus", "Marcus", "Alima", "Francisco", "Thaqib", "Andreas", "Mariana", "Amalie", "Rodney", "Dena", "Fadl", "Ammar", "Anna", "Nasreen", "Reem", "Tomas", "Filipa", "Frank", "Bari'ah", "Parvaiz", "Jibran", "Tomas", "Elli", "Carlos", "Diego", "Henrik", "Aruna", "Vahid", "Eliana", "Roxane", "Amanda", "Ingrid", "Wander", "Malika", "Basim", "Eisa", "Alina", "Andreas", "Deeba", "Diya", "Parveen", "Bakr", "Celine", "Bakr", "Marcus", "Daniel", "Mathea", "Edmee", "Hedda", "Maria", "Maja", "Alhasan", "Alina", "Hedda", "Victor", "Aaftab", "Guilherme", "Maria", "Kai", "Sabien", "Abdel", "Fadl", "Bahaar", "Vasco", "Jibran", "Parsa", "Catalina", "Fouad", "Colette" };
      return names[random.Next(0, names.Count)];
    }
    private static string GenerateLastName()
    {
      List<string> lastnames = new List<string>() { "Carlson", "Attia", "Quint", "Hollenberg", "Khoury", "Araujo", "Hakimi", "Seegers", "Abadi", "al", "Krommenhoek", "Siavashi", "Kvistad", "Sjo", "Vanderslik", "Fernandes", "Dehli", "Sheibani", "Laamers", "Batlouni", "Lyngvær", "Oveisi", "Veenhuizen", "Gardenier", "Siavashi", "Mutlu", "Karzai", "Mousavi", "Natsheh", "Seegers", "Nevland", "Lægreid", "Bishara", "Cunha", "Hotaki", "Kyvik", "Cardoso", "Pilskog", "Pennekamp", "Nuijten", "Bettar", "Borsboom", "Skistad", "Asef", "Sayegh", "Sousa", "Medeiros", "Kregel", "Shamoun", "Behzadi", "Kuzbari", "Ferreira", "Van", "Barros", "Fernandes", "Formo", "Nolet", "Shahrestaani", "Correla", "Amiri", "Sousa", "Fretheim", "Van", "Hamade", "Baba", "Mustafa", "Bishara", "Formo", "Hemmati", "Nader", "Hatami", "Natsheh", "Langen", "Maloof", "Berger", "Ostrem", "Bardsen", "Kramer", "Bekken", "Salcedo", "Holter", "Nader", "Bettar", "Georgsen", "Cunha", "Zardooz", "Araujo", "Batalha", "Antunes", "Vanderhoorn", "Nader", "Abadi", "Siavashi", "Montes", "Sherzai", "Vanderschans", "Neves", "Sarraf", "Kuiters" };
      return lastnames[random.Next(0, lastnames.Count)];
    }
    #endregion

    private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap)
    {
      FileSavePicker fileSavePicker = new FileSavePicker();
      fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
      fileSavePicker.FileTypeChoices.Add("JPEG files", new List<string>() { ".jpg" });
      fileSavePicker.SuggestedFileName = "image";

      var outputFile = await fileSavePicker.PickSaveFileAsync();
      if (outputFile != null)
      {
        using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
        {
          // Create an encoder with the desired format
          BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

          // Set the software bitmap
          encoder.SetSoftwareBitmap(softwareBitmap);

          await encoder.FlushAsync();
        }
      }
    }
  }

  public class PersonGroup : IGrouping<string, PersonViewModel>
  {
    private List<PersonViewModel> _personGroup;

    public PersonGroup(string key, IEnumerable<PersonViewModel> persons)
    {
      Key = key;
      _personGroup = new List<PersonViewModel>(persons);
    }

    public string Key { get; }

    public string Count { get { return string.Format(" ({0})", _personGroup.Count); } }

    public IEnumerator<PersonViewModel> GetEnumerator() => _personGroup.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _personGroup.GetEnumerator();
  }


  public static class NoSynchronizationContextScope
  {
    public static Disposable Enter()
    {
      var context = SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(null);
      return new Disposable(context);
    }

    public struct Disposable : IDisposable
    {
      private readonly SynchronizationContext _synchronizationContext;

      public Disposable(SynchronizationContext synchronizationContext)
      {
        _synchronizationContext = synchronizationContext;
      }

      public void Dispose() =>
          SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
    }
  }
}
