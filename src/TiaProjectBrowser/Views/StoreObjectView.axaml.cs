using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.TextMate;
using AvaloniaWebView;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using ImageMagick;
using RtfDomParser;
using Siemens.Automation.DomainModel;
using Siemens.Simatic.Hmi.Utah.Globalization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml;
using TextMateSharp.Grammars;
using TiaAvaloniaProjectBrowser.Classes;
using TiaFileFormat.Database.Business;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.ExtensionMethods;
using TiaFileFormat.Helper;
using TiaFileFormat.S7CommPlus;
using TiaFileFormat.Wrappers;
using TiaFileFormat.Wrappers.CfCharts;
using TiaFileFormat.Wrappers.CfCharts.Converter;
using TiaFileFormat.Wrappers.CodeBlocks;
using TiaFileFormat.Wrappers.CodeBlocks.Interface;
using TiaFileFormat.Wrappers.Controller.Alarms;
using TiaFileFormat.Wrappers.Controller.Opc;
using TiaFileFormat.Wrappers.Controller.Tags;
using TiaFileFormat.Wrappers.Controller.WatchTable;
using TiaFileFormat.Wrappers.Converters.AutomationXml;
using TiaFileFormat.Wrappers.Converters.Code;
using TiaFileFormat.Wrappers.Hmi.Alarms;
using TiaFileFormat.Wrappers.Hmi.Connections;
using TiaFileFormat.Wrappers.Hmi.Cycle;
using TiaFileFormat.Wrappers.Hmi.GraphicLists;
using TiaFileFormat.Wrappers.Hmi.Tags;
using TiaFileFormat.Wrappers.Hmi.Udts;
using TiaFileFormat.Wrappers.Hmi.WinCCAdvanced;
using TiaFileFormat.Wrappers.Hmi.WinCCUnified;
using TiaFileFormat.Wrappers.TextLists;
using static TiaAvaloniaProjectBrowser.Views.OnlineHelper;

namespace TiaAvaloniaProjectBrowser.Views;

public partial class StoreObjectView : UserControl, IDisposable
{
    static StoreObjectView()
    {
        var cv = new ImageToDataUriConverterWithConversion();
        _winCCAdvancedScriptParser = new WinCCAdvancedScriptParser();
        _winCCAdvancedScreenConverter = new WinCCAdvancedScreenConverter(cv, _winCCAdvancedScriptParser);
        _winCCUnifiedScriptParser = new WinCCUnifiedScriptParser();
        _winCCUnifiedScreenConverter = new WinCCUnifiedScreenConverter(cv, _winCCUnifiedScriptParser);
        _codeBlockConverter = new CodeBlockConverter();
        _convertOptions = new ConvertOptions();
        _highLevelObjectConverterWrapper = new HighLevelObjectConverterWrapper();
    }

    static WinCCAdvancedScriptParser _winCCAdvancedScriptParser;
    static WinCCAdvancedScreenConverter _winCCAdvancedScreenConverter;
    static WinCCUnifiedScriptParser _winCCUnifiedScriptParser;
    static WinCCUnifiedScreenConverter _winCCUnifiedScreenConverter;
    static CodeBlockConverter _codeBlockConverter;
    static HighLevelObjectConverterWrapper _highLevelObjectConverterWrapper;
    internal static ConvertOptions _convertOptions;
    internal CancellationTokenSource cancellationTokenSource;

    private WebView webView;
    private string webViewUrl;
    private FoldingManager foldingManager;
    private TextMate.Installation textMateInstallationCode;
    private TextMate.Installation textMateInstallationXml;
    private TextMate.Installation textMateInstallationSpecial;

    public StoreObjectView()
    {
        InitializeComponent();
        DataContextChanged += StoreObjectView_DataContextChanged;

        this.Unloaded += StoreObjectView_Unloaded;
        this.tabControl.SelectionChanged += (s, e) =>
        {
            if (this.tabControl.SelectedItem == tabWebview)
            {
                this.DiplayWebsite();
            }
            else
            {
                this.HideWebsite();
            }
        };

        datagrid.AutoGeneratingColumn += (sender, e) =>
        {
            if (e.Column.Header?.ToString() == "StorageBusinessObject")
            {
                e.Cancel = true; // Spalte unterdrücken
            }
        };
    }

    private void HideWebsite()
    {
        if (webViewEvtHandler != null)
        {
            webView.WebMessageReceived -= webViewEvtHandler;
        }
    }
    private void DiplayWebsite()
    {
        webView = new WebView();
        webView.BorderThickness = new Avalonia.Thickness(1);
        webView.BorderBrush = Avalonia.Media.Brushes.Black;
        webView.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        webView.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        tabWebview.Content = webView;
        if (webViewEvtHandler != null)
        {
            webView.WebMessageReceived += webViewEvtHandler;
        }
        webView.Url = new Uri(webViewUrl);
    }

    private void StoreObjectView_Unloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.cancellationTokenSource?.Cancel();
    }

    string lastFile;

    private async void Open_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BaseBlock cblk;
        if (this.DataContext is OnlineTreeItem oti)
        {
            cblk = OnlineBlockConverter.GetOnlineCodeBlock(oti.Connection, oti.blockRid);
        }
        else
        {
            var sb = this.DataContext as StorageBusinessObject;
            cblk = _highLevelObjectConverterWrapper.Convert(sb, _convertOptions) as BaseBlock;
        }

        if (cblk != null)
        {
            var exe = @"C:\Program Files\Siemens\Automation\SIMATIC Automation Compare Tool\ACTool.exe ";
            var tempPath = Path.Combine(Path.GetTempPath(), "TempTiaBlock");
            Directory.CreateDirectory(tempPath);
            var file = Path.Combine(tempPath, string.Join("_", cblk.Name.Split(Path.GetInvalidFileNameChars())) + ".xml");
            File.WriteAllText(file, cblk.ToAutomationXml());
            //File.WriteAllText(file, xmlEditor.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = exe;
            proc.StartInfo.Arguments = '"' + file + '"';
            //proc.StartInfo.UseShellExecute = true;
            proc.Start();
        }
    }

    private async void Expert_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenStoreObject((StorageObject)this.DataContext);
    }

    private async void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var sb = this.DataContext as StorageBusinessObject;
        if (sb?.GetChild<HmiInternalImageAttributes>() != null)
        {
            var imgDataAttr = sb.GetChild<HmiInternalImageAttributes>();
            var imgData = imgDataAttr.GenuineContent.Data;
            var topLevel = TopLevel.GetTopLevel(this);

            var fileType = new FilePickerFileType("Image File")
            {
                Patterns = new[] { "*" + imgDataAttr.FileExtension },
                AppleUniformTypeIdentifiers = new[] { imgDataAttr.FileExtension + " image file" },
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save",
                FileTypeChoices = new[] { fileType }
            });

            if (file != null)
            {
                var fileNm = file.Path.AbsolutePath;
                if (imgDataAttr.FileExtension == ".svg")
                {
                    File.WriteAllText(fileNm, imgDataAttr.GenuineContent.DataAsString.RemoveBOM());
                }
                else
                {
                    File.WriteAllBytes(fileNm, imgDataAttr.GenuineContent.Data.ToArray());
                }
            }
        }
        else if (sb?.IsOfType("Siemens.Simatic.Hmi.DL.ModernUI.ILScreenData") == true)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var res = _winCCUnifiedScreenConverter.ConvertWinccUnifiedScreenToHTML(sb);
            var fileType = new FilePickerFileType("HTML File")
            {
                Patterns = new[] { "*.html" },
                AppleUniformTypeIdentifiers = new[] { ".html file" },
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save",
                FileTypeChoices = new[] { fileType }
            });

            if (file != null)
            {
                var fileNm = file.Path.AbsolutePath;
                File.WriteAllText(fileNm, res.Html, new UTF8Encoding(false));
            }
        }
        else if (sb?.IsOfType("Siemens.Simatic.Hmi.Utah.GraphX.HmiScreenData") == true ||
                 sb?.IsOfType("Siemens.Simatic.Hmi.Utah.Modules.ScreenModules.HmiScreenModuleVersionData") == true)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var html = _winCCAdvancedScreenConverter.Convert(sb, null) as WinCCScreen;
            var fileType = new FilePickerFileType("HTML File")
            {
                Patterns = new[] { "*.html" },
                AppleUniformTypeIdentifiers = new[] { ".html file" },
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save",
                FileTypeChoices = new[] { fileType }
            });

            if (file != null)
            {
                var fileNm = file.Path.AbsolutePath;
                File.WriteAllText(fileNm, html.Html, new UTF8Encoding(false));
            }
        }
    }

    EventHandler<WebViewCore.Events.WebViewMessageReceivedEventArgs> webViewEvtHandler;

    private void StoreObjectView_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = null;
        }

        try
        {
            if (lastFile != null)
                File.Delete(lastFile);
            lastFile = null;
        }
        catch { }

        try
        {
            textMateInstallationCode?.Dispose();
            textMateInstallationSpecial?.Dispose();
            textMateInstallationXml?.Dispose();

            webViewEvtHandler = null;
            specialEditor.Text = "";
            codeEditor.Text = "";
            xmlEditor.Text = "";
            debugEditor.Text = "";
            webViewUrl = null;
            graphicdatagrid.ItemsSource = null;
            tabWebview.Content = null;
            treedatagrid.Source = null;
            treedatagrid2.Source = null;

            tabSpecialEditor.IsVisible = false;
            tabXmlEditor.IsVisible = false;
            tabImg.IsVisible = false;
            tabSvg.IsVisible = false;
            tabWebview.IsVisible = false;
            tabGrid.IsVisible = false;
            tabGrid2.IsVisible = false;
            tabTreeGrid.IsVisible = false;
            tabTreeGrid2.IsVisible = false;
            tabCodeEditor.IsVisible = false;
            tabGraphicGrid.IsVisible = false;
            tabDebugEditor.IsVisible  = false;

            tabControl.SelectedItem = null;
            buttons.IsVisible = false;

            if (this.DataContext is OnlineTreeItem oti)
            {
                buttons.IsVisible = true;
                var cBlk = OnlineBlockConverter.GetOnlineCodeBlock(oti.Connection, oti.blockRid);
                DisplayCodeBlock(cBlk);
            }

            if (this.DataContext is MainView.Step5_7TreeItem step5_7TreeItem)
            {
                if (step5_7TreeItem.ProjectBlockInfo is S7ProjectBlockInfo s7ProjectBlockInfo)
                {
                    var blk = s7ProjectBlockInfo.GetBlock();

                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("TiaProjectBrowser.Views.STL.xshd"))
                    using (var reader = XmlReader.Create(stream))
                    {
                        codeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                    codeEditor.Text = blk.ToString();
                    tabCodeEditor.IsVisible = true;
                    tabCodeEditor.IsSelected = true;
                }
                else if (step5_7TreeItem.ProjectBlockInfo is S5ProjectBlockInfo s5ProjectBlockInfo)
                {
                    var blk = s5ProjectBlockInfo.GetBlock();

                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("TiaProjectBrowser.Views.STL.xshd"))
                    using (var reader = XmlReader.Create(stream))
                    {
                        codeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                    codeEditor.Text = blk.ToString();
                    tabCodeEditor.IsVisible = true;
                    tabCodeEditor.IsSelected = true;
                }
            }
            else
            {
                var sb = this.DataContext as StorageBusinessObject;

                if (sb != null)
                    buttons.IsVisible = true;

                if (sb?.GetChild<HmiInternalImageAttributes>() != null)
                {
                    var imgDataAttr = sb.GetChild<HmiInternalImageAttributes>();
                    var imgData = imgDataAttr.GenuineContent.Data;

                    var imgExt = imgDataAttr.FileExtension;
                    if (RichTextFormatHelper.IsRtf(imgData.Span))
                    {
                        using var tr = new StringReader(imgDataAttr.GenuineContent.DataAsString);
                        var d = new RTFDomDocument();
                        d.Load(tr);
                        var image = d.Elements.Traverse<RTFDomElement>(x => x.Elements).OfType<RTFDomImage>().FirstOrDefault();
                        if (image.PicType == RTFPicType.Wmetafile)
                        {
                            imgData = image.Data;
                            imgExt = ".wmf";
                        }
                        else if (image.PicType == RTFPicType.Emfblip)
                        {
                            imgData = image.Data;
                            imgExt = ".emf";
                        }
                        else if (image.PicType == RTFPicType.Pngblip)
                        {
                            imgData = image.Data;
                            imgExt = ".png";
                        }
                        else if (image.PicType == RTFPicType.Wbitmap)
                        {
                            imgData = image.Data;
                            imgExt = ".bmp";
                        }
                    }

                    if (imgExt == ".svg")
                    {
                        string fileName = Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".svg");
                        File.WriteAllText(fileName, imgDataAttr.GenuineContent.DataAsString.RemoveBOM(), new UTF8Encoding(false));
                        var scripts = "";
                        tabWebview.IsVisible = true;
                        webViewUrl = fileName;
                        tabWebview.IsSelected = true;
                        lastFile = fileName;
                    }
                    else if (imgExt == ".wmf" || imgExt == ".emf")
                    {
                        using var ms = new MemoryStream();
                        using var image = new MagickImage(imgData.ToArray());
                        //image.Scale(new Percentage(60));
                        image.Write(ms, MagickFormat.Svg);
                        ms.Position = 0;
                        using var sr = new StreamReader(ms);
                        var text = sr.ReadToEnd();
                        svg.Source = text;
                        tabSvg.IsVisible = true;
                        tabSvg.IsSelected = true;
                    }
                    else
                    {
                        using var ms = new MemoryStream(imgData.ToArray());
                        ms.Position = 0;

                        img.Source = new Bitmap(ms);
                        tabImg.IsVisible = true;
                        tabImg.IsSelected = true;
                    }
                }
                else if (sb?.IsOfType("Siemens.Automation.DomainModel.OnlineBackupData") == true)
                {
                    var lst = new List<BackupItemInfo>();
                    foreach (var c in sb.GetRelationsWithNameResolved("Siemens.Automation.DomainModel.OnlineBackupData.Elements"))
                    {
                        var lo = c.GetChild<LoadableObjectS7>();
                        var bin = lo.BinaryData.Data.ToArray();
                        var bid = new BackupItemDataInfo();
                        using var ms = new MemoryStream(bin);
                        using var br = new EndiannessAwareBinaryReader(ms, EndiannessAwareBinaryReader.Endianness.Big);
                        bid.ParseFromBinaryReader(br, TiaFileFormat.Database.File.FileFormat.V14);
                        var item = new BackupItemInfo() { Name = c.Name, BlockNumber = lo.BlockNumber, BlockType = (OamBlockType)lo.BlockType, Data = bin, BackupItemDataInfo = bid };
                        lst.Add(item);
                    }
                    datagrid.ItemsSource = lst;
                    tabGrid.IsVisible = true;
                    tabGrid.IsSelected = true;
                }
                else if (sb?.IsOfType("Siemens.Simatic.Hmi.DL.ModernUI.ILScreenData") == true)
                {
                    var res = _winCCUnifiedScreenConverter.ConvertWinccUnifiedScreenToHTML(sb);
                    string fileName = Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");
                    File.WriteAllText(fileName, res.Html, new UTF8Encoding(false));
                    var scripts = res.GetScriptString();
                    var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                    textMateInstallationCode?.Dispose();
                    textMateInstallationCode = codeEditor.InstallTextMate(_registryOptions);
                    textMateInstallationCode.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".js").Id));
                    codeEditor.Text = scripts;
                    webViewUrl = fileName;
                    tabWebview.IsVisible = true;
                    tabCodeEditor.IsVisible = true;
                    tabWebview.IsSelected = true;
                    lastFile = fileName;
                }
                else if (sb?.IsOfType("Siemens.Simatic.Hmi.Utah.GraphX.HmiScreenData") == true ||
                         sb?.IsOfType("Siemens.Simatic.Hmi.Utah.Modules.ScreenModules.HmiScreenModuleVersionData") == true)
                {
                    var res = _winCCAdvancedScreenConverter.Convert(sb, null) as WinCCScreen;

                    var html = "<meta charset=\"utf-8\"><div>";
                    html += "<script>function handleEvent() {window.chrome.webview.postMessage(window.event.type + \":\" + window.event.target.id);}</script>";
                    html += res.Html;
                    html += "</div>";

                    string fileName = Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");
                    File.WriteAllText(fileName, html, new UTF8Encoding(false));
                    var scripts = res.GetScriptString();
                    var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                    textMateInstallationCode?.Dispose();
                    textMateInstallationCode = codeEditor.InstallTextMate(_registryOptions);
                    textMateInstallationCode.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".js").Id));
                    webViewEvtHandler = (s, e) =>
                    {
                        var @params = e.Message.Split(":");
                        var evt = @params[0];
                        var id = @params[1];
                        List<WinCCAdvancedScriptParser.ScriptCommand> commands = null;
                        if (res.Scripts.TryGetValue(id, out var script))
                        {
                            if (evt == "click")
                                script.TryGetValue("Dyn.Click.Scripting#", out commands);
                            else if (evt == "pointerdown")
                                script.TryGetValue("Dyn.Press.Scripting#", out commands);
                            else if (evt == "pointerup")
                                script.TryGetValue("Dyn.Release.Scripting#", out commands);
                        }
                        if (commands != null)
                        {
                            foreach (var cmd in commands)
                            {
                                switch (cmd.Type)
                                {
                                    case TiaFileFormat.Wrappers.Hmi.WinCCAdvanced.WinCCAdvancedScriptParser.ScriptCommandType.ActivateScreen:
                                        {
                                            var f = sb;
                                            var nm = cmd.Arguments[0] as string;
                                            var screen = sb.Parent.Parent.ProjectTreeChildren.SelectMany(x => x.ProjectTreeChildren).FirstOrDefault(x => x.Name == nm);
                                            if (screen != null)
                                            {
                                                Dispatcher.UIThread.Invoke(() =>
                                                {
                                                    this.DataContext = screen;
                                                });
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                    };
                    codeEditor.Text = scripts;
                    webViewUrl = fileName;
                    tabWebview.IsVisible = true;
                    tabCodeEditor.IsVisible = true;
                    tabWebview.IsSelected = true;
                    lastFile = fileName;
                }
                else
                {
                    if (sb != null)
                    {
                        var highLevelObject = _highLevelObjectConverterWrapper.Convert(sb, _convertOptions);
                        if (highLevelObject != null)
                        {
                            switch (highLevelObject)
                            {
                                case BaseBlock baseBlock:
                                    {
                                        DisplayCodeBlock(baseBlock);
                                        break;
                                    }
                                case WinCCScript winCCScript:
                                    {
                                        var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                                        textMateInstallationCode?.Dispose();
                                        textMateInstallationCode = codeEditor.InstallTextMate(_registryOptions);
                                        if (winCCScript.ScriptLang == TiaFileFormat.Wrappers.Hmi.ScriptLang.VB )
                                            textMateInstallationCode.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".vbs").Id));
                                        else if (winCCScript.ScriptLang == TiaFileFormat.Wrappers.Hmi.ScriptLang.C || winCCScript.ScriptLang == TiaFileFormat.Wrappers.Hmi.ScriptLang.C_Header)
                                            textMateInstallationCode.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".c").Id));
                                        else
                                            textMateInstallationCode.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".js").Id));
                                        codeEditor.Text = winCCScript.Script;
                                        tabCodeEditor.IsVisible = true;
                                        tabCodeEditor.IsSelected = true;
                                        break;
                                    }
                                case PlcTagTable plcTagTable:
                                    {
                                        codeEditor.Text = CsvSerializer.ToCsv(plcTagTable.Tags);
                                        tabCodeEditor.IsVisible = true;
                                        datagrid.ItemsSource = plcTagTable.Tags;
                                        datagrid2.ItemsSource = plcTagTable.UserConstants;
                                        tabGrid.IsVisible = true;
                                        tabGrid2.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case HmiTagTable hmiTagTable:
                                    {
                                        codeEditor.Text = CsvSerializer.ToCsv(hmiTagTable.Tags);
                                        tabCodeEditor.IsVisible = true;
                                        datagrid.ItemsSource = hmiTagTable.Tags;
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case HmiUdt hmiUdt:
                                    {
                                        codeEditor.Text = CsvSerializer.ToCsv(hmiUdt.Children);
                                        tabCodeEditor.IsVisible = true;
                                        datagrid.ItemsSource = hmiUdt.Children;
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case TextList textList:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(textList, new JsonSerializerOptions() { WriteIndented = true });
                                        tabCodeEditor.IsVisible = true;
                                        datagrid.ItemsSource = textList.Ranges;
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case GraphicList graphicList:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(graphicList, new JsonSerializerOptions() { WriteIndented = true });
                                        tabCodeEditor.IsVisible = true;
                                        graphicdatagrid.ItemsSource = graphicList.Ranges;
                                        tabGraphicGrid.IsVisible = true;
                                        tabGraphicGrid.IsSelected = true;
                                        break;
                                    }
                                case CfChart cfChart:
                                    {
                                        var src = new HierarchicalTreeDataGridSource<TiaFileFormat.Wrappers.CfCharts.Interface.Member>(cfChart.Interface)
                                        {
                                            Columns =
                                        {
                                            new HierarchicalExpanderColumn<TiaFileFormat.Wrappers.CfCharts.Interface.Member>(new TextColumn<TiaFileFormat.Wrappers.CfCharts.Interface.Member, string>("Name", x => x.Name), x => x.Children),
                                            new TextColumn<TiaFileFormat.Wrappers.CfCharts.Interface.Member, string>("DataType", x => x.DataType ?? ""),
                                            new TextColumn<TiaFileFormat.Wrappers.CfCharts.Interface.Member, string>("StartValue", x => x.StartValue ?? ""),
                                            new TextColumn<TiaFileFormat.Wrappers.CfCharts.Interface.Member, string>("Comment", x => x.Comment == null ? "" : x.Comment.ToString() ?? ""),
                                        }
                                        };
                                        treedatagrid.Source = src;
                                        //src.ExpandAll();
                                        tabTreeGrid.IsVisible = true;

                                        codeEditor.Text = JsonSerializer.Serialize(cfChart, new JsonSerializerOptions() { WriteIndented = true });
                                        tabCodeEditor.IsVisible = true;

                                        var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                                        textMateInstallationXml?.Dispose();
                                        textMateInstallationXml = xmlEditor.InstallTextMate(_registryOptions);
                                        textMateInstallationXml.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".xml").Id));
                                        xmlEditor.Text = cfChart.ToAutomationXml();

                                        if (foldingManager != null)
                                            FoldingManager.Uninstall(foldingManager);
                                        foldingManager = FoldingManager.Install(xmlEditor.TextArea);
                                        var strategy = new XmlFoldingStrategy();
                                        strategy.UpdateFoldings(foldingManager, xmlEditor.Document);

                                        tabXmlEditor.IsVisible = true;

                                        var imgs = cfChart.ConvertToSvgs();
                                        string fileName = Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".svg");
                                        File.WriteAllText(fileName, imgs.FirstOrDefault(), new UTF8Encoding(false));
                                        tabWebview.IsVisible = true;
                                        webViewUrl = fileName;
                                        tabWebview.IsSelected = true;
                                        lastFile = fileName;
                                        break;
                                    }
                                case WatchTable watchTable:
                                    {
                                        codeEditor.Text = CsvSerializer.ToCsv(watchTable.Items);
                                        tabCodeEditor.IsVisible = true;
                                        var lst = new ObservableCollection<WatchTableEntryWithValue>(watchTable.Items.Select(x => new WatchTableEntryWithValue(x)));
                                        datagrid.ItemsSource = lst;

                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case AlarmList alarmList:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(alarmList, new JsonSerializerOptions() { WriteIndented = true });
                                        tabCodeEditor.IsVisible = true;
                                        datagrid.ItemsSource = alarmList.Alarms;
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case HmiCycleList hmiCycleList:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(hmiCycleList, new JsonSerializerOptions() { WriteIndented = true });
                                        datagrid.ItemsSource = hmiCycleList.HmiCycles;
                                        tabCodeEditor.IsVisible = true;
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case HmiConnectionList hmiConnectionList:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(hmiConnectionList, new JsonSerializerOptions() { WriteIndented = true });
                                        datagrid.ItemsSource = hmiConnectionList.Connections;
                                        tabCodeEditor.IsVisible = true;
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                case OpcServerInterface opcServerInterface:
                                    {
                                        xmlEditor.Text = opcServerInterface.ServerInterfaceFile;
                                        tabXmlEditor.IsVisible = true;
                                        tabXmlEditor.IsSelected = true;
                                        break;
                                    }
                                case HmiAlarmList hmiAlarmList:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(hmiAlarmList, new JsonSerializerOptions() { WriteIndented = true });
                                        tabCodeEditor.IsVisible = true;
                                        if (hmiAlarmList.HmiAlarmListType == HmiAlarmListType.Discrete)
                                            datagrid.ItemsSource = hmiAlarmList.Alarms.OfType<HmiDiscreteAlarm>().ToList();
                                        else if (hmiAlarmList.HmiAlarmListType == HmiAlarmListType.Analog)
                                            datagrid.ItemsSource = hmiAlarmList.Alarms.OfType<HmiAnalogAlarm>().ToList();
                                        else if (hmiAlarmList.HmiAlarmListType == HmiAlarmListType.OpcUa)
                                            datagrid.ItemsSource = hmiAlarmList.Alarms.OfType<HmiOpcUaAlarm>().ToList();
                                        else if (hmiAlarmList.HmiAlarmListType == HmiAlarmListType.System)
                                            datagrid.ItemsSource = hmiAlarmList.Alarms.OfType<HmiSystemAlarm>().ToList();
                                        tabGrid.IsVisible = true;
                                        tabGrid.IsSelected = true;
                                        break;
                                    }
                                default:
                                    {
                                        codeEditor.Text = JsonSerializer.Serialize(highLevelObject, highLevelObject.GetType(), new JsonSerializerOptions() { WriteIndented = true });
                                        tabCodeEditor.IsVisible = true;
                                        tabCodeEditor.IsSelected = true;
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void DisplayCodeBlock(BaseBlock codeBlock)
    {
        if (codeBlock is CodeBlock cblk && cblk.SpecialCodeBlockData != null)
        {
            var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
            textMateInstallationSpecial?.Dispose();
            textMateInstallationSpecial = specialEditor.InstallTextMate(_registryOptions);
            textMateInstallationSpecial.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".json").Id));
            specialEditor.Text = JsonSerializer.Serialize(cblk.SpecialCodeBlockData, cblk.SpecialCodeBlockData.GetType(), new JsonSerializerOptions() { WriteIndented = true });
            tabSpecialEditor.IsVisible = true;
        }

        //if (codeBlock.Interface != null)
        //{
        //    debugEditor.Text = string.Join("\n\n\n\n", codeBlock.Interface.AllSourceXmls);
        //    tabDebugEditor.IsVisible = true;
        //}

        var src = new HierarchicalTreeDataGridSource<Member>(codeBlock.Interface?.Members)
        {
            Columns =
                {
                    new HierarchicalExpanderColumn<Member>(new TextColumn<Member, string>("Name", x => x.Name), x => x.Children),
                    new TextColumn<Member, string>("DataType", x => x.DataType ?? ""),
                    new TextColumn<Member, string>("AtViewOf", x => x.AtViewOf != null ? x.AtViewOf.Name : null),
                    new TextColumn<Member, string>("StartValue", x => x.StartValue),
                    new TextColumn<Member, Remanence>("Retain", x => x.Remanence),
                    new CheckBoxColumn<Member>("HmiAccessible", x => x.HmiAccessible),
                    new CheckBoxColumn<Member>("HmiWriteable", x => !x.HmiReadOnly),
                    new CheckBoxColumn<Member>("HmiVisible", x => x.HmiVisible),
                    new CheckBoxColumn<Member>("SetPoint", x => x.SetPoint),
                    new TextColumn<Member, UInt32?>("OffsetInBits", x => x.OffsetInBits),
                    new TextColumn<Member, string>("Id", x => x.ID),
                    new TextColumn<Member, string>("Comment", x => x.Comment == null ? "" : x.Comment.ToString() ?? ""),
                }
        };
        treedatagrid.Source = src;
        //src.ExpandAll();
        tabTreeGrid.IsVisible = true;

        var src2 = new HierarchicalTreeDataGridSource<MemberInstance>(codeBlock.Interface?.MemberValues)
        {
            Columns =
                {
                    new HierarchicalExpanderColumn<MemberInstance>(new TextColumn<MemberInstance, string>("Name", x => x.Name), x => x.Children),
                    new TextColumn<MemberInstance, string>("DataType", x => x.Member.DataType ?? ""),
                    new TextColumn<MemberInstance, string>("StartValue", x => x.Value),
                    new TextColumn<MemberInstance, UInt32?>("OffsetInBits", x => x.OffsetInBits),
                    new TextColumn<MemberInstance, UInt32?>("CompleteOffsetInBits", x => x.CompleteOffsetInBits),
                    new TextColumn<MemberInstance, uint>("LID", x => x.LID),
                    new TextColumn<MemberInstance, string>("LidPathString", x => x.LidPathString),
                    new TextColumn<MemberInstance, string>("Comment", x => x.Comment == null ? "" : x.Comment.ToString() ?? ""),
                }
        };
        treedatagrid2.Source = src2;
        //src.ExpandAll();
        tabTreeGrid2.IsVisible = true;

        if (codeBlock.IsKowHowProtected && !codeBlock.DecryptionPossible)
        {
            codeEditor.Text = "KowHowProtected, Password not specified or wrong";
            tabCodeEditor.IsVisible = true;
            tabCodeEditor.IsSelected = true;
        }
        else
        {
            if (codeBlock.BlockLang == BlockLang.SCL || codeBlock.BlockLang == BlockLang.STL || codeBlock.BlockLang == BlockLang.UDT)
            {
                if (codeBlock.BlockLang == BlockLang.SCL || codeBlock.BlockLang == BlockLang.UDT)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("TiaProjectBrowser.Views.SCL.xshd"))
                    using (var reader = XmlReader.Create(stream))
                    {
                        codeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
                else if (codeBlock.BlockLang == BlockLang.STL)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("TiaProjectBrowser.Views.STL.xshd"))
                    using (var reader = XmlReader.Create(stream))
                    {
                        codeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }

                var txt = codeBlock.ToSourceBlock(new CodeBlockToSourceBlockConverter.ConvertOptions() { Mnemonik = TiaFileFormat.Wrappers.CodeBlocks.Mnemonic.German });
                if (!string.IsNullOrEmpty(txt))
                {
                    codeEditor.Text = txt;
                    tabCodeEditor.IsVisible = true;
                    tabCodeEditor.IsSelected = true;
                }

                var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                textMateInstallationXml?.Dispose();
                textMateInstallationXml = xmlEditor.InstallTextMate(_registryOptions);
                textMateInstallationXml.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".xml").Id));
                xmlEditor.Text = codeBlock.ToAutomationXml();

                if (foldingManager != null)
                    FoldingManager.Uninstall(foldingManager);
                foldingManager = FoldingManager.Install(xmlEditor.TextArea);
                var strategy = new XmlFoldingStrategy();
                strategy.UpdateFoldings(foldingManager, xmlEditor.Document);

                tabXmlEditor.IsVisible = true;
                if (!tabCodeEditor.IsVisible)
                    tabXmlEditor.IsSelected = true;
            }
            else
            {
                var _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                textMateInstallationXml?.Dispose();
                textMateInstallationXml = xmlEditor.InstallTextMate(_registryOptions);
                textMateInstallationXml.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".xml").Id));
                xmlEditor.Text = codeBlock.ToAutomationXml();

                if (foldingManager != null)
                    FoldingManager.Uninstall(foldingManager);
                foldingManager = FoldingManager.Install(xmlEditor.TextArea);
                var strategy = new XmlFoldingStrategy();
                strategy.UpdateFoldings(foldingManager, xmlEditor.Document);

                tabXmlEditor.IsVisible = true;
                tabXmlEditor.IsSelected = true;
            }
        }
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

            //var sov2 = new StoreObjectLinkView();
            //sov2.DataContext = obj;
            //var wnd2 = new Window();
            //wnd2.Content = sov2;
            //wnd2.Padding = new Avalonia.Thickness(10);
            //wnd2.Title = "StoreObject: " + obj.Header.StoreObjectId + " (" + ttn + ")";
            //wnd2.Show();
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
}