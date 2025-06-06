using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DotNetSiemensPLCToolBoxLibrary.DataTypes;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.Projectfiles;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using Siemens.Simatic.Hmi.Utah.Globalization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TiaFileFormat;
using TiaFileFormat.Database;
using TiaFileFormat.Database.Business;
using TiaFileFormat.Database.Business.BaseTypes;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.Project;

namespace TiaAvaloniaProjectBrowser.Views;

public partial class MainView : UserControl
{
    public static MainView? Instance;

    internal ObservableCollection<object> rootList = new ObservableCollection<object>();

    public MainView()
    {
        InitializeComponent();

        Instance = this;

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);

        var args = Environment.GetCommandLineArgs();
        if (args != null && args.Length > 1)
        {
            var _ = Load(args[1]);
        }

        this.Unloaded += MainView_Unloaded;

        tv.ItemsSource = this.rootList;
    }

    private void MainView_Unloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    public void SetActionText(string text)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            action.Text = text;
        });
    }

    void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    async void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            await Load(e.Data.GetFiles().OfType<IStorageFile>().ToList());
        }
    }

    public class SimpleTreeItem : INotifyPropertyChanged
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public IEnumerable<object> ProjectTreeChildrenSorted { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class Step5_7TreeItem
    {
        public string Name { get; set; }

        public ProjectBlockInfo ProjectBlockInfo { get; set; }

        public IEnumerable<object> ProjectTreeChildrenSorted { get; set; }
    }

    ObservableCollection<object> items;
    private async void cmdOpen_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var tiaFileType = new FilePickerFileType("Tia Portal Files")
        {
            Patterns = new[] { "*.plf", "*.ap20", "*.ap19", "*.ap18", "*.ap17", "*.ap16", "*.ap15_1", "*.ap15", "*.ap14", "*.ap13", "*.ap12", "*.ap11", "*.ap10", "*.zap20", "*.zal20", "*.zap19", "*.zal19", "*.zap18", "*.zal18", "*.zap17", "*.zal17", "*.zap16", "*.zal16", "*.zap15_1", "*.zal15_1", "*.zap15", "*.zal15", "*.zap14", "*.zal14", "*.zap13", "*.zal13", "*.zap12", "*.zal12", "*.zap11", "*.zal11", "*.zip", "*.s7p", "*.s7l", "*.s5d" },
            AppleUniformTypeIdentifiers = new[] { "org.tia.portal" },
            MimeTypes = new[] { "application/tia" }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open",
            AllowMultiple = false,
            FileTypeFilter = new[] { tiaFileType }
        });

        await Load(files);
    }

    private async void cmdPassword_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var mb = MessageBoxManager.GetMessageBoxCustom(
             new MessageBoxCustomParams
             {
                 ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Ok", },
                    new ButtonDefinition { Name = "Cancel", }
                },
                 ContentTitle = "Password",
                 ContentMessage = "Password for encrypted blocks",
                 Icon = MsBox.Avalonia.Enums.Icon.Question,
                 WindowStartupLocation = WindowStartupLocation.CenterOwner,
                 CanResize = false,
                 MaxWidth = 500,
                 MaxHeight = 800,
                 SizeToContent = SizeToContent.WidthAndHeight,
                 ShowInCenter = true,
                 InputParams = new InputParams() { },
                 Topmost = false,

             });
        var res = await mb.ShowAsync();
        if (res == "Ok")
        {
            StoreObjectView._convertOptions.EncryptionOptions.EncryptionPasswords = new List<string>() { mb.InputValue };
        }
    }

    private async Task Load(IReadOnlyList<IStorageFile> files)
    {
        if (files.Count >= 1)
        {
            await Load(files[0].Path.LocalPath);
        }
    }

    Dictionary<TiaDatabaseFile, ObservableCollection<object>> databaseTreeItemCollectionDict = new Dictionary<TiaDatabaseFile, ObservableCollection<object>>();

    private async Task Load(string file)
    {
        var fileTvItem = new SimpleTreeItem() { Name = file + "  [ loading... ]" };
        items = new ObservableCollection<object>();
        fileTvItem.ProjectTreeChildrenSorted = items;
        rootList.Add(fileTvItem);

        await Task.Factory.StartNew(() =>
        {
            var fti = fileTvItem;
            var container = items;

            if (!LoadStep7Project(file, fileTvItem, container))
            {
                LoadTiaProject(file, fileTvItem, container);
            }

        }).ConfigureAwait(false);
    }

    private bool LoadStep7Project(string file, SimpleTreeItem fileTvItem, ObservableCollection<object> container)
    {
        Project prj = Projects.LoadProject(file, showDeleted: false); //, chkShowDeleted.Checked, credentials
        if (prj != null)
        {
            //SimpleTreeItem
            //prj.ProjectStructure
            container.Add(ToSimpleTreeItem(prj.ProjectStructure));
            return true;
        }
        return false;
    }

    #region S5/7 support

    private SimpleTreeItem ToSimpleTreeItem(ProjectFolder projectFolder)
    {
        var fld = new SimpleTreeItem() { Name = projectFolder.Name, ProjectTreeChildrenSorted = projectFolder.SubItems?.Select(x => ToSimpleTreeItem(x)) };
        if (projectFolder is BlocksOfflineFolder blkOfflineFld)
        {
            fld.ProjectTreeChildrenSorted = blkOfflineFld.BlockInfos.Select(x => new Step5_7TreeItem() { ProjectBlockInfo = x, Name = ((S7ProjectBlockInfo)x).BlockName + (x.Name == null ? "" : " (" + x.Name + ")") });
        }
        else if (projectFolder is Step5BlocksFolder step5BlocksFolder)
        {
            fld.ProjectTreeChildrenSorted = step5BlocksFolder.BlockInfos.Select(x => new Step5_7TreeItem() { ProjectBlockInfo = x, Name = ((S5ProjectBlockInfo)x).BlockName + (x.Name == null ? "" : " (" + x.Name + ")") });
        }
        return fld;
    }

    private void LoadTiaProject(string file, SimpleTreeItem fileTvItem, ObservableCollection<object> container)
    {
        var tfp = TiaFileProvider.CreateFromSingleFile(file);
        if (tfp.TiaFileProviderType != TiaFileProviderType.Unkown)
        {
            var project = TiaProject.Load(tfp);
            var database = project.Database;
            databaseTreeItemCollectionDict.Add(database, items);

            var umacItems = new List<object>();
            if (database.RootObject.StoreObjectIds.ContainsKey("Project"))
            {
                var prjObj = (StorageBusinessObject)database.RootObject.StoreObjectIds["Project"].StorageObject;
                Dispatcher.UIThread.Invoke(() =>
                {
                    fileTvItem.Name = file + "  [ parseing... ]";
                    container.Add(prjObj);
                });
            }
            if (database.RootObject.StoreObjectIds.ContainsKey("Library"))
            {
                var libObj = (StorageBusinessObject)database.RootObject.StoreObjectIds["Library"].StorageObject;
                Dispatcher.UIThread.Invoke(() =>
                {
                    fileTvItem.Name = file + "  [ parseing... ]";
                    container.Add(libObj);
                });
            }

            _ = Task.Factory.StartNew(() =>
            {
                var lst = container;
                database.ParseAllObjects();

                var umacItems = new List<StorageBusinessObject>();
                if (database.RootObject.StoreObjectIds.ContainsKey("Project"))
                {
                    var prjObj = (StorageBusinessObject)database.RootObject.StoreObjectIds["Project"].StorageObject;
                    umacItems.AddRange(prjObj.GetRelationsWithNameResolved("Siemens.Automation.Umac.Model.Root.UmacRootData.UmacRoot"));
                }
                if (database.RootObject.StoreObjectIds.ContainsKey("Library"))
                {
                    var libObj = (StorageBusinessObject)database.RootObject.StoreObjectIds["Library"].StorageObject;
                    umacItems.AddRange(libObj.GetRelationsWithNameResolved("Siemens.Automation.Umac.Model.Root.UmacRootData.UmacRoot"));
                }
                umacItems = umacItems.Distinct().ToList();
                if (umacItems.Count > 0)
                {
                    var umac = umacItems[0];
                    var users = umac.GetRelationsWithNameResolved("Siemens.Automation.Umac.Model.Root.UmacRootData.AllUsers");
                    if (users.Count() > 0)
                    {
                        var userItem = new SimpleTreeItem() { Name = "Users", ProjectTreeChildrenSorted = users };
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            lst.Add(new SimpleTreeItem() { Name = "Security", ProjectTreeChildrenSorted = [userItem] });
                        });
                    }
                }
                var imgs = database.FindStorageBusinessObjectsWithChildType<HmiInternalImageAttributes>();
                if (imgs.Count() > 0)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        lst.Add(new SimpleTreeItem() { Name = "Images in Project", ProjectTreeChildrenSorted = imgs });
                    });
                }
                Dispatcher.UIThread.Invoke(() =>
                {
                    fileTvItem.Name = file;
                });

                //var a = database.AllStorageObjects.OfType<StorageBusinessObject>().Where(x => x.GetChild<ProjectUserAttributes>() != null || x.GetChild<UmcUserAttributes>() != null).ToList();
            }).ConfigureAwait(false);
        }
    }

    #endregion

    bool FindInObject(object o, string search)
    {
        if (o == null)
            return false;
        if (o is bool)
            return false;
        if (o is string s)
            return s.Contains(search, StringComparison.OrdinalIgnoreCase);
        if (o is sbyte sb)
        {
            if (sbyte.TryParse(search, out var search_i))
            {
                return sb == search_i;
            }
            return false;
        }
        if (o is byte b)
        {
            if (byte.TryParse(search, out var search_i))
            {
                return b == search_i;
            }
            return false;
        }
        if (o is int i)
        {
            if (int.TryParse(search, out var search_i))
            {
                return i == search_i;
            }
            return false;
        }
        if (o is uint ui)
        {
            if (uint.TryParse(search, out var search_i))
            {
                return ui == search_i;
            }
            return false;
        }
        if (o is long l)
        {
            if (long.TryParse(search, out var search_i))
            {
                return l == search_i;
            }
            return false;
        }
        if (o is ulong ul)
        {
            if (ulong.TryParse(search, out var search_i))
            {
                return ul == search_i;
            }
            return false;
        }
        if (o is short ss)
        {
            if (short.TryParse(search, out var search_i))
            {
                return ss == search_i;
            }
            return false;
        }
        if (o is ushort us)
        {
            if (ushort.TryParse(search, out var search_i))
            {
                return us == search_i;
            }
            return false;
        }
        if (o is double dd)
        {
            if (double.TryParse(search, out var search_i))
            {
                return dd == search_i;
            }
            return false;
        }
        if (o is float ff)
        {
            if (float.TryParse(search, out var search_i))
            {
                return ff == search_i;
            }
            return false;
        }
        if (o is decimal dec)
        {
            if (decimal.TryParse(search, out var search_i))
            {
                return dec == search_i;
            }
            return false;
        }
        if (o is DateTime)
        {
            return false;
        }
        if (o is CoreBlob cb)
        {
            return cb.DataAsString.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        if (o is CoreTextAttribute cta)
        {
            return cta.Texts.Any(x => x.Value.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        if (o.GetType().IsEnum)
        {
            return o.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        if (o is Guid g)
        {
            return g.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        if (o is char c)
        {
            return c.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        if (o is BaseStructure bs)
        {
            foreach (var p in bs.GetSetPropertiesDictionary())
            {
                if (FindInObject(p.Value, search))
                    return true;
            }
            return false;
        }
        if (o is ICoreArray ca)
        {
            foreach (var p in ca.DataUntyped)
            {
                if (FindInObject(p, search))
                    return true;
            }
            return false;
        }
        if (o is CoreMap cm)
        {
            foreach (var p in cm.Values)
            {
                if (FindInObject(p, search))
                    return true;
            }
            return false;
        }
        if (o is ExpandoLink)
        {
            return false;
        }
        if (o.GetType().FullName == "System.Drawing.Size" || o.GetType().FullName == "System.Drawing.Color")
        {
            return false;
        }
        if (o.GetType().IsClass)
        {
            throw new Exception("abc");
        }

        return false;
    }

    private void TreeView_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var sb = (e.Source as Control)?.DataContext as StorageBusinessObject;
        if (sb != null)
        {
            try
            {
                var sov = new StoreObjectView();
                sov.DataContext = sb;
                var wnd = new Window();
                wnd.Content = sov;
                wnd.Padding = new Avalonia.Thickness(10);
                wnd.Title = sb.Path;
                wnd.Show();
            }
            catch (Exception) { }
        }
    }

    internal static bool showAllFolders;

    private void CheckBox_Checked_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        showAllFolders = ((CheckBox)sender).IsChecked.Value;
        var items = tv.ItemsSource;
        tv.ItemsSource = null;
        tv.ItemsSource = items;
    }

    //internal static S7CommPlusConnection s7CommPlusConnection;
    //internal static Dictionary<string, VarInfo> s7CommPlusVars;

    private void Connect_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var sb = tv.SelectedItem as StorageBusinessObject;
        var ipSel = new IpSelector(sb);
        var wnd = new Window();
        wnd.Content = ipSel;
        wnd.Padding = new Avalonia.Thickness(10);
        wnd.Title = "Connect to S7-1500";
        wnd.CanResize = true;
        wnd.Width = 1250;
        wnd.Height = 550;
        wnd.Show();
    }

    private void TreeView_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            if (tv.SelectedItem is StorageBusinessObject sb)
            {
                var mnuI = new MenuItem { Header = "Search", };
                mnuI.Click += async (s, e) =>
                {
                    var mb = MessageBoxManager.GetMessageBoxCustom(
                        new MessageBoxCustomParams
                        {
                            ButtonDefinitions = new List<ButtonDefinition>
                        {
                            new ButtonDefinition { Name = "Ok", },
                            new ButtonDefinition { Name = "Cancel", }
                        },
                            ContentTitle = "Search",
                            ContentMessage = "Search for :",
                            Icon = MsBox.Avalonia.Enums.Icon.Question,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            MaxWidth = 500,
                            MaxHeight = 800,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            ShowInCenter = true,
                            InputParams = new InputParams() { },
                            Topmost = false,

                        });
                    var res = await mb.ShowAsync();
                    if (res == "Ok")
                    {
                        search(sb, mb.InputValue);
                        //StoreObjectView._convertOptions.EncryptionOptions.EncryptionPasswords = new List<string>() { mb.InputValue };
                    }
                };
                var opn = new MenuItem { Header = "Open by Id", };
                opn.Click += async (s, e) =>
                {
                    var mb = MessageBoxManager.GetMessageBoxCustom(
                        new MessageBoxCustomParams
                        {
                            ButtonDefinitions = new List<ButtonDefinition>
                        {
                            new ButtonDefinition { Name = "Ok", },
                            new ButtonDefinition { Name = "Cancel", }
                        },
                            ContentTitle = "Open by Id",
                            ContentMessage = "Id :",
                            Icon = MsBox.Avalonia.Enums.Icon.Question,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            MaxWidth = 500,
                            MaxHeight = 800,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            ShowInCenter = true,
                            InputParams = new InputParams() { },
                            Topmost = false,

                        });
                    var res = await mb.ShowAsync();
                    if (res == "Ok")
                    {
                        if (long.TryParse(mb.InputValue, out var nr))
                        {
                            if (sb.Database.StorageObjectDictionary.TryGetValue(nr, out var found))
                            {
                                var sov = new StoreObjectView();
                                sov.DataContext = found;
                                var wnd = new Window();
                                wnd.Content = sov;
                                wnd.Padding = new Avalonia.Thickness(10);
                                wnd.Title = sb.Path;
                                wnd.Show();
                            }
                        }
                    }
                };
                var contextMenu = new ContextMenu
                {
                    Items =
                {
                    mnuI,
                    opn
                }
                };

                if (sender is Control control)
                {
                    contextMenu.PlacementTarget = control;
                    contextMenu.Open(control);
                }
            }
        }
    }

    private void search(StorageBusinessObject sb, string text)
    {
        try
        {
            var database = sb.Database;

            var txt = text;
            //if (txt.Contains("-"))
            //{
            //    txt = txt.Split("-")[1];
            //}
            //if (Int64.TryParse(txt, out var id) && this.database.StorageObjectDictionary.ContainsKey(id))
            //{
            //    var obj = this.database.StorageObjectDictionary[id];
            //    StoreObjectView.OpenStoreObject(obj);
            //}
            //else
            //{

            progress.IsIndeterminate = true;
            var search = txt.ToLower();
            action.Text = "searching for \"" + search + "\"";
            var objs = new ObservableCollection<StorageBusinessObject>();

            databaseTreeItemCollectionDict[sb.Database].Add(new SimpleTreeItem() { Name = "Search \"" + search + "\"", ProjectTreeChildrenSorted = objs });

            Task.Run(() =>
                {
                    foreach (var sb in database.StorageObjects.OfType<StorageBusinessObject>())
                    {
                        if (sb.Name != null && sb.Name.ToLower().Contains(search))
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                if (!objs.Contains(sb))
                                    objs.Add(sb);
                            });
                        }
                    }
                    try
                    {
                        foreach (var sb in database.StorageObjects.OfType<StorageBusinessObject>())
                        {
                            foreach (var o in sb.Children)
                            {
                                if (o is BaseRelationList)
                                { }
                                else if (o is BaseExpando exp)
                                {
                                    foreach (var p in exp.Values)
                                    {
                                        if (FindInObject(p, search))
                                        {
                                            Dispatcher.UIThread.Invoke(() =>
                                            {
                                                if (!objs.Contains(sb))
                                                    objs.Add(sb);
                                            });
                                            goto EndFE;
                                        }
                                    }
                                }
                                else if (o is TiaFileFormat.Database.Business.CoreText ct)
                                {
                                    if (ct != null && ct.Texts.Any(x => x.Value != null && x.Value.Contains(search, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        Dispatcher.UIThread.Invoke(() =>
                                        {
                                            if (!objs.Contains(sb))
                                                objs.Add(sb);
                                        });
                                        goto EndFE;
                                    }
                                }
                                else if (o is BaseBusinessObject bbo)
                                {
                                    var dc = bbo.GetSetPropertiesDictionary();
                                    foreach (var p in dc.Values)
                                    {
                                        if (FindInObject(p, search))
                                        {
                                            Dispatcher.UIThread.Invoke(() =>
                                            {
                                                if (!objs.Contains(sb))
                                                    objs.Add(sb);
                                            });
                                            goto EndFE;
                                        }
                                    }
                                }
                            }
                        EndFE:;
                        }
                    }
                    catch (Exception)
                    { }
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        progress.IsIndeterminate = false;
                    });
                });
        }
        catch { }
    }
}