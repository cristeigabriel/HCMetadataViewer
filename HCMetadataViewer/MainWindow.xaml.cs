using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static HCMetadataViewer.MetadataParser;

namespace HCMetadataViewer
{
  public static class Context
  {
    public static List<Title> titles = new List<Title>();
  }

  public class ResultsBinding
  {
    public ListCollectionView ResultsView { get; set; } =
      (ListCollectionView)CollectionViewSource.GetDefaultView(Context.titles);
  }

  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      var parser = new MetadataParser(global::HCMetadataViewer.Properties.Resources.list_final8);
      Context.titles = parser.titles;
      InitializeComponent();
      this.Title = "HCMetadataViewer";
    }

    private void titles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      var selectedItem = titles.SelectedItem as Title;
      if (selectedItem != null)
      {
        var window = new DataWindow(selectedItem);
        window.Show();
      }
    }

    private static bool MatchesWildcard(string lhs, string rhs)
    {
      int lhsIndex = 0, rhsIndex = 0;
      int starIndex = -1, afterStarIndex = -1;

      while (rhsIndex < rhs.Length)
      {
        if (lhsIndex < lhs.Length && (lhs[lhsIndex] == '*' || lhs[lhsIndex] == rhs[rhsIndex]))
        {
          if (lhs[lhsIndex] == '*')
          {
            starIndex = lhsIndex;
            afterStarIndex = rhsIndex;
            lhsIndex++;
            continue;
          }
          lhsIndex++;
          rhsIndex++;
        }
        else if (starIndex != -1)
        {
          lhsIndex = starIndex + 1;
          rhsIndex = afterStarIndex + 1;
          afterStarIndex++;
        }
        else
        {
          return false;
        }
      }

      while (lhsIndex < lhs.Length && lhs[lhsIndex] == '*')
      {
        lhsIndex++;
      }

      return lhsIndex == lhs.Length;
    }

    private void titlesFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (titles != null)
      {
        ListCollectionView resultsView = (ListCollectionView)titles.ItemsSource;
        resultsView.Filter = obj =>
        {
          if (string.IsNullOrEmpty(titlesFilter.Text) || string.IsNullOrWhiteSpace(titlesFilter.Text))
          {
            return true;
          }

          Title title = (Title)obj;
          var titleName = title.name.ToLower();
          var filter = titlesFilter.Text.ToLower();

          return MatchesWildcard(filter, titleName);
        };
      }
    }
  }
}