using MedicalOfficeClient.Services;
using MedicalOfficeClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MedicalOfficeClient.Views
{
  /// <summary>
  /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
  /// </summary>
  public sealed partial class PersonsView : Page
  {
    PersonsViewModel ViewModel { get; set; }
    public ObservableCollection<string> Suggestions { get; private set; }

    public PersonsView()
    {
      this.Suggestions = new ObservableCollection<string>();
      this.InitializeComponent();
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);
      ViewModel = new PersonsViewModel();
      await ViewModel.InitializeAsync();
    }

    private async void PersonList_ItemClick(object sender, ItemClickEventArgs e)
    {
      ShowDetailPersonView();
      var viewModel = e.ClickedItem as PersonViewModel;
      await viewModel.LoadPreviewAsync();
      ViewModel.PersonToViewDetails = viewModel;
    }

    private void AddPerson_Click(object sender, RoutedEventArgs e)
    {
      ViewModel.PersonToCreateOrEdit = new PersonViewModel();
      ShowCreatePersonView();
    }

    private void EditPerson_Click(object sender, RoutedEventArgs e)
    {
      ViewModel.PersonToCreateOrEdit = ViewModel.PersonToViewDetails;
      ShowCreatePersonView();
    }

    private void CancleCreatePerson_Click(object sender, RoutedEventArgs e)
    {
      ShowDetailPersonView();
    }

    private void ShowDetailPersonView()
    {
      CreateContentPresenter.Visibility = Visibility.Collapsed;
      DetailContentPresenter.Visibility = Visibility.Visible;
    }

    private void ShowCreatePersonView()
    {
      DetailContentPresenter.Visibility = Visibility.Collapsed;
      CreateContentPresenter.Visibility = Visibility.Visible;
    }

    private async void SaveCreatePerson_Click(object sender, RoutedEventArgs e)
    {
      await ViewModel.SaveCreatedPerson();
      ViewModel.PersonToViewDetails = ViewModel.PersonToCreateOrEdit;
      ShowDetailPersonView();
    }

    private void CasesListView_ItemClick(object sender, ItemClickEventArgs e)
    {
      if (e.ClickedItem is MedicalCaseViewModel)
      {
        var item = e.ClickedItem as MedicalCaseViewModel;
        Frame.Navigate(typeof(MedicalCaseView), item);
      }
    }

    // Search Box

    private void SearchPersonBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
      if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
      {
        var suggestions = SearchPersons(sender.Text);
        if (0 < suggestions.Count)
        {
          sender.ItemsSource = suggestions;
        }
        else
        {
          sender.ItemsSource = new string[] { "Keine Ergebnisse" };
        }
      }
    }

    private void SearchPersonBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
      if (args.ChosenSuggestion != null && args.ChosenSuggestion is PersonGroup)
      {
        //User selected an item, take an action
        SelectPerson(args.ChosenSuggestion as Person);
      }
      else if (!string.IsNullOrEmpty(args.QueryText))
      {
        //Do a fuzzy search based on the text
        var suggestions = SearchPersons(sender.Text);
        if (0 < suggestions.Count)
        {
          SelectPerson(suggestions.FirstOrDefault());
        }
      }
    }

    private void SearchPersonBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
      var person = args.SelectedItem as Person;

      //Don't autocomplete the TextBox when we are showing "no results"
      if (person != null)
      {
        sender.Text = person.FormName;
      }
    }

    private void SelectPerson(Person person)
    {
      if (person != null)
      {

      }
    }

    private List<Person> SearchPersons(string query)
    {
      var groups = ViewModel.GroupedPersons;
      var suggestions = new List<Person>();

      foreach (var group in groups)
      {
        var matchingItems = group.Where(p => p.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0);
        foreach (var item in matchingItems)
        {
          suggestions.Add(item);
        }
      }

      return suggestions
        .OrderByDescending(i => i.FormName.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        .ThenBy(i => i.FormName)
        .ToList();
    }
  }
}
