using Avalonia.Controls;
using Avalonia.Threading;
using S7CommPlusDriver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.S7CommPlus;
using TiaFileFormat.Wrappers.Controller.Network;
using TiaFileFormat.Wrappers.Online.PlcStructure;
using TiaFileFormat.Wrappers.Online.PlcStructure.StructureTypes;
using static TiaAvaloniaProjectBrowser.Views.MainView;
using static TiaAvaloniaProjectBrowser.Views.OnlineHelper;

namespace TiaAvaloniaProjectBrowser.Views;

public partial class IpSelector : UserControl
{
    public class TiaBrowserNetworkInformation
    {
        public string Source { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string IpAddress { get; set; }
        public string SubnetMask { get; set; }
        public bool UseRouter { get; set; }
        public string RouterIpAddress { get; set; }
    }

    ObservableCollection<TiaBrowserNetworkInformation> networkInfos;
    ProfinetClientFinder pcf;

    public IpSelector(StorageBusinessObject sb)
    {
        InitializeComponent();

        if (sb != null)
        {
            var nwInfos = NetworkInformationConverter.ParseFromCpuOrSubPart(sb).Select(x => new TiaBrowserNetworkInformation()
            {
                Source = "Project",
                Name = x.Name,
                IpAddress = x.IpAddress,
                SubnetMask = x.SubnetMask,
                UseRouter = x.UseRouter,
                RouterIpAddress = x.RouterIpAddress
            });
            networkInfos = new ObservableCollection<TiaBrowserNetworkInformation>(nwInfos);
        }
        else
        {
            networkInfos = new ObservableCollection<TiaBrowserNetworkInformation>();
        }
        lstIps.ItemsSource = networkInfos;

        pcf = new ProfinetClientFinder();
        pcf.ProfinetClientFound = c =>
        {
            if (c.StationType != "SIMATIC-PC")
            {
                var nwInfo = new TiaBrowserNetworkInformation()
                {
                    Source = "Broadcast",
                    Name = c.StationName,
                    Type = c.StationType,
                    IpAddress = c.IPAddress.ToString(),
                    SubnetMask = c.Subnetmask.ToString(),
                    UseRouter = c.Gateway.ToString() != "0.0.0.0",
                    RouterIpAddress = c.Gateway.ToString(),
                };
                Dispatcher.UIThread.Invoke(() =>
                {
                    networkInfos.Add(nwInfo);
                });
            }
        };
        pcf.Find();

        this.Unloaded += (s, e) =>
        {
            pcf.Dispose();
        };
    }

    private void Ok_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        S7Client.WriteSslKeyToFile = true;

        var s7CommPlusConnection = new S7CommPlusDriver.S7CommPlusConnection();
        var ip = txtIp.Text;
        MainView.Instance.SetActionText("Try to connect to IP: " + ip);
        Task.Factory.StartNew(() =>
        {
            var res = s7CommPlusConnection.Connect(ip, "", "", 30000);
            if (res == 0)
            {
                MainView.Instance.SetActionText("Connected to " + ip + " now reading Blocks");

                res = s7CommPlusConnection.GetCpuInfos(out var cpuInfos);

                List<Unit> units = null;
                var resStruct = s7CommPlusConnection.GetPlcStructureXML(out var structureXml);
                if (resStruct == 0)
                {
                    var plcStr = new PlcStructureParser();
                    units = plcStr.ParseStructureXml(structureXml);
                }

                if (units != null)
                {
                    var fileTvItem = new SimpleTreeItem() { Name = "Online: " + ip + " (" + cpuInfos.PlcName + ", " + cpuInfos.ProjectName + ", " + cpuInfos.VersionTia + ")" };

                    Func<IFolderOrUnit, SimpleTreeItem> folderToTreeItem = null;
                    folderToTreeItem = (IFolderOrUnit fld) =>
                    {
                        var subItems = fld.Folders.Select(x => folderToTreeItem(x));
                        if (fld is Folder bf)
                        {
                            subItems = subItems.Union(bf.Blocks.Select(blk =>
                            {
                                return new OnlineTreeItem() { Name = blk.Name + " (" + blk.Number + ")", blockRid = blk.RId, Connection = s7CommPlusConnection };
                            }));
                        }

                        return new SimpleTreeItem()
                        {
                            Name = fld.Name ?? "Default-Unit",
                            ProjectTreeChildrenSorted = subItems.ToList()
                        };
                    };

                    fileTvItem.ProjectTreeChildrenSorted = units.Select(folderToTreeItem).ToList();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        MainView.Instance.rootList.Add(fileTvItem);
                    });
                }
                else
                {
                    var resBlk = s7CommPlusConnection.BrowseAllBlocks(out var brws);
                    if (resBlk == 0)
                    {
                        var fileTvItem = new SimpleTreeItem() { Name = "Online: " + ip };
                        var items = brws.GroupBy(x => x.type).Select(x =>
                                new SimpleTreeItem()
                                {
                                    Name = x.Key.ToString() + "s",
                                    ProjectTreeChildrenSorted = x.GroupBy(x => x.lang).Select(y =>
                                        new SimpleTreeItem()
                                        {
                                            Name = y.Key.ToString(),
                                            ProjectTreeChildrenSorted = y.Select(z => new OnlineTreeItem() { Name = z.name + " (" + z.number + ")", blockRid = z.db_block_relid, Connection = s7CommPlusConnection }).OrderBy(a => a.Name).ToList()
                                        }).ToList()
                                });
                        fileTvItem.ProjectTreeChildrenSorted = items.ToList();
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            MainView.Instance.rootList.Add(fileTvItem);
                        });
                    }
                    else
                    {
                        MainView.Instance.SetActionText("Error Reading Blocks!");
                    }
                }
            }
            else
            {
                MainView.Instance.SetActionText("Error connecting to PLC! - " + res);
            }
        });
        ((Window)this.Parent).Close();
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ((Window)this.Parent).Close();
    }

    private void lstIps_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        var nwInfo = ((TiaBrowserNetworkInformation)lstIps.SelectedItem);
        txtIp.Text = nwInfo.IpAddress;
    }

    private void DataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        Ok_Click(null, null);
    }
}