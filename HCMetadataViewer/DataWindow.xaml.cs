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
  public partial class DataWindow : Window
  {
    public DataWindow(Title title)
    {
      InitializeComponent();
      this.Title = title.name;

      this.parsedTitle = new TitleParser(title).ParseTitle();
      FunctionNameTextBlock.Text = parsedTitle.FunctionName ?? "None";
      FunctionTypeTextBlock.Text = parsedTitle.GenerateFunctionPrototype();
      var friendlyArguments = parsedTitle.Arguments.Select(x =>
      {
        x.CArgument = x.CArgument.Split(' ').Last();
        return x;
      });
      ArgumentsItemsControl.ItemsSource = friendlyArguments;
      DllTextBlock.Text = parsedTitle.Dll ?? "Unknown";
      LibTextBlock.Text = parsedTitle.Library ?? "Unknown";
      HeaderTextBlock.Text = parsedTitle.Header ?? "Unknown";
      MinConsumerVerTextBlock.Text = parsedTitle.MinVersionConsumer ?? "Unknown";
      MinServerVerTextBlock.Text = parsedTitle.MinVersionServer ?? "Unknown";
    }

    public ParsedTitle parsedTitle;
  }
}