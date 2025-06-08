using Avalonia.Controls;
using Northwoods.Go.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TiaAvaloniaProjectBrowser;

public partial class CallOrder : UserControl
{
    public class Model : GraphLinksModel<NodeData, string, object, LinkData, string, string> { }
    public class NodeData : Model.NodeData
    {
        public string Color { get; set; }
    }
    public class LinkData : Model.LinkData { }


    public CallOrder()
    {
        InitializeComponent();
        //aa();
    }

    public async void aa()
    {
        await Task.Delay(2000).ConfigureAwait(true);

        var model = new Model
        {
            NodeDataSource = new List<NodeData> {
        new NodeData { Key = "Alpha", Color = "lightblue" },
        new NodeData { Key = "Beta", Color = "orange" },
        new NodeData { Key = "Gamma", Color = "lightgreen" },
        new NodeData { Key = "Delta", Color = "pink" }
      },
            LinkDataSource = new List<LinkData> {
        new LinkData { From = "Alpha", To = "Beta" },
        new LinkData { From = "Alpha", To = "Gamma" },
        new LinkData { From = "Beta", To = "Beta" },
        new LinkData { From = "Gamma", To = "Delta" },
        new LinkData { From = "Delta", To = "Alpha" }
      }
        };

        try
        {
            var diagram = diagramControl1.Diagram;
            diagram.Model = model;
        }
        catch(Exception)
        { }
    }
}