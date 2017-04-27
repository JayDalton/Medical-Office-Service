using MedicalOfficeClient.Services;
using MedicalOfficeClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MedicalOfficeClient.Views
{
  public class PersonViewModel : NotificationBase<Person>
  {
    public PersonViewModel(Person person = null) : base(person)
    {
      _cases = new ObservableCollection<MedicalCaseViewModel>();
    }

    public Guid PersonId
    {
      get { return This.PersonId; }
    }

    public String Title
    {
      get { return This.Title; }
      set { SetProperty(This.Title, value, () => This.Title = value); }
    }

    public String Name
    {
      get { return This.FormName; }
    }

    public String LastName
    {
      get { return This.LastName; }
      set { SetProperty(This.LastName, value, () => This.LastName = value); }
    }

    private string _firstName;
    public string FirstName
    {
      get { return _firstName ?? This.FirstName; }
      set { if (SetProperty(ref _firstName, value)) { This.FirstName = value; } }
    }

    public DateTime Birthday
    {
      get { return This.Birthday; }
      set { SetProperty(This.Birthday, value, () => This.Birthday = value); }
    }

    private ObservableCollection<MedicalCaseViewModel> _cases;
    public ObservableCollection<MedicalCaseViewModel> Cases
    {
      get { return _cases; }
      set { SetProperty(ref _cases, value); }
    }

    public async Task LoadPreviewAsync()
    {
      using (var db = new Database())
      {
        Cases.Clear();
        This = await db.Persons.FindAsync(This.PersonId);
        if (This != null)
        {
          await db.Entry(This).Collection(p => p.Cases).LoadAsync();
          foreach (var Case in This.Cases)
          {
            Cases.Add(new MedicalCaseViewModel(Case));
          }
        }
      }
    }

    public async Task AddNewCaseAsync(object sender, RoutedEventArgs e)
    {
      if (sender is Button)
      {
        using (var db = new Database())
        {
          var btn = sender as Button;
          var type = (MedicalCaseType)btn.Tag;
          var person = await db.Persons.FindAsync(This.PersonId);
          if (person != null)
          {
            var mediCase = new MedicalCase { Type = type };
            switch (mediCase.Type)
            {
              case MedicalCaseType.Logo:
                mediCase.Label = string.Format("Logopädie {0:d}", DateTime.Now);
                break;
              case MedicalCaseType.Ergo:
                mediCase.Label = string.Format("Ergotherapie {0:d}", DateTime.Now);
                break;
              case MedicalCaseType.Physio:
                mediCase.Label = string.Format("Physiotherapie {0:d}", DateTime.Now);
                break;
              case MedicalCaseType.Satellite:
                mediCase.Label = string.Format("Satellitenfall {0:d}", DateTime.Now);
                break;
            }

            await db.Entry(person).Collection(c => c.Cases).LoadAsync();
            if (person.Cases == null)
            {
              person.Cases = new List<MedicalCase>() { mediCase };
            }
            else
            {
              person.Cases.Add(mediCase);
            }
            await db.SaveChangesAsync();
            Cases.Add(new MedicalCaseViewModel(mediCase));
          }
        }
      }
    }

  }
}
