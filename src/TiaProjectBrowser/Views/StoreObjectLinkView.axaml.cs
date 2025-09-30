using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.LogicalTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using TiaFileFormat.Database.Business;
using TiaFileFormat.Database.StorageTypes;
using TiaProjectBrowser;

namespace TiaAvaloniaProjectBrowser.Views;

public partial class StoreObjectLinkView : UserControl, IDisposable
{
    public class LinkWrapper
    {
        public StorageBusinessObject StorageBusinessObject { get; set; }

        public IEnumerable<LinkWrapper> Children
        {
            get
            {
                return LinksTo.OrderBy(x => x.StorageBusinessObject.Header.StoreObjectId.InstId).Union(LinkedFrom.OrderBy(x => x.StorageBusinessObject.Header.StoreObjectId.InstId));
            }
        }

        public string Direction { get; set; }

        public List<LinkWrapper> LinksTo
        {
            get
            {
                return StorageBusinessObject.GetAllRelationsResolved().Where(x => x != null).Select(r => new LinkWrapper() { StorageBusinessObject = r, Direction = "To ->" }).ToList();
            }
        }

        public List<LinkWrapper> LinkedFrom
        {
            get
            {

                return StorageBusinessObject.Database.AllStorageObjects
                    .OfType<StorageBusinessObject>()
                    .Where(x => x.GetAllRelationsResolved().Where(x => x != null).Contains(this.StorageBusinessObject))
                    .Select(r => new LinkWrapper() { StorageBusinessObject = r, Direction = "From <-" })
                    .ToList();
            }
        }
    }

    static StoreObjectLinkView()
    {
    }

    public StoreObjectLinkView()
    {
        InitializeComponent();
        DataContextChanged += StoreObjectView_DataContextChanged;
    }

    string lastFile;

    EventHandler<WebViewCore.Events.WebViewMessageReceivedEventArgs> webViewEvtHandler;

    private void StoreObjectView_DataContextChanged(object? sender, System.EventArgs e)
    {
        try
        {
            if (lastFile != null)
                File.Delete(lastFile);
            lastFile = null;
        }
        catch { }

        try
        {
            var sb = this.DataContext as StorageBusinessObject;
            if (sb != null)
            {
                var src = new HierarchicalTreeDataGridSource<LinkWrapper>(new LinkWrapper() { Direction = "Initial", StorageBusinessObject=sb })
                {
                    Columns =
                {
                    new HierarchicalExpanderColumn<LinkWrapper>(
                        new TextColumn<LinkWrapper, string>("Name", x => x.StorageBusinessObject.Name), x => x.Children),
                    new TextColumn<LinkWrapper, string>("Direction", x => x.Direction),
                    new TextColumn<LinkWrapper, long>("Id", x => x.StorageBusinessObject.Header.StoreObjectId.InstId)
                }
                };
                tv.Source = src;
            }
            else { tv.Source = null; }
        }
        catch { }
    }

    private void detailsExpander_Expanded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var expander = sender as Expander;
        var dc = expander.DataContext as BaseBusinessObject;
        var lb = expander.GetLogicalChildren().OfType<DataGrid>().First();

        if (dc is BaseExpando expando)
        {
            lb.ItemsSource = expando.Data.Select(x => new DetailsInfo() { Name = x.Key, Value = x.Value });
        }
        else if (dc is BaseRelationList rl)
        {
            lb.ItemsSource = rl.Relations;
        }
        else if (dc != null)
        {
            var lst = new List<DetailsInfo>();
            var prp = dc.GetType().GetProperties();
            foreach (var p in prp)
            {
                if (p.Name == "StorageObjectContainer" || p.Name.StartsWith("_"))
                    continue;
                var val = p.GetValue(dc);
                lst.Add(new DetailsInfo() { Name = p.Name, Value = val });
            }
            lb.ItemsSource = lst;
        }
    }

    public class DetailsInfo
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    private void DataGrid_AutoGeneratingColumn(object? sender, Avalonia.Controls.DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName == "StoreObjectId")
            e.Cancel = true;
    }

    private void Label_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        try
        {
            var tb = sender as TextBlock;
            var rel = tb.DataContext as Relation;
            var storeObj = rel.StoreObjectId.StorageObject;
            OpenStoreObject(storeObj);
        }
        catch (Exception) { }
    }

    private void TextBlock_PointerPressed_1(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        OpenStoreObject(this.DataContext as StorageObject);
    }

    public static void OpenStoreObject(StorageObject obj)
    {
        try
        {
            var sov = new StoreObjectInternalsView();
            sov.DataContext = obj;
            var wnd = new Window();
            wnd.Content = sov;
            wnd.Padding = new Avalonia.Thickness(10);
            var ttn = obj is StorageBusinessObject sbo ? sbo.TiaTypeName : "";
            wnd.Title = "StoreObject: " + obj.Header.StoreObjectId + " (" + ttn + ")";
            wnd.Show();
        }
        catch (Exception) { }
    }

    private void Button_CopyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var sb = this.DataContext as StorageObject;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            clipboard.SetTextAsync(sb.Header.StoreObjectId.ToString());
        }
        catch (Exception) { }
    }

    public void Dispose()
    {
        try
        {
            if (lastFile != null)
                File.Delete(lastFile);
        }
        catch { }
    }

    private void ButtonCopyContent(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var wrp = ((Button)sender).DataContext as TreeItemWrapper;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            clipboard.SetTextAsync(wrp.StringContent());
        }
        catch (Exception) { }
    }

    private void ButtonCopyType(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var wrp = ((Button)sender).DataContext as TreeItemWrapper;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
             clipboard.SetTextAsync(wrp.Name);
        }
        catch (Exception) { }
    }

    private void ButtonHex(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var wrp = ((Button)sender).DataContext as TreeItemWrapper;
            var bytes = wrp.ByteContent();
            if (bytes != null)
            {
                var ipSel = new HexViewer(bytes);
                var wnd = new Window();
                wnd.Content = ipSel;
                wnd.Padding = new Avalonia.Thickness(10);
                wnd.Title = "Byte view of " + wrp.Name;
                wnd.CanResize = true;
                wnd.Width = 1250;
                wnd.Height = 550;
                wnd.Show();
            }
        }
        catch (Exception) { }
    }

}