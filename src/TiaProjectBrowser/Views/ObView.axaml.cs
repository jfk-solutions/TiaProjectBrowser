using Avalonia.Controls;
using System;
using System.Linq;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.ExtensionMethods;
using TiaFileFormat.Wrappers.CodeBlocks;

namespace TiaAvaloniaProjectBrowser.Views;

public partial class ObView : UserControl
{
    public class Info
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public int? EnableActual { get; set; }
        public int? EnableInitial { get; set; }
        public byte? EventClassNumber { get; set; }
        public uint? EventNumber { get; set; }
        public DateTime? LastModified { get; set; }
        public int? MaxBufferedEvents { get; set; }
        public int? Priority { get; set; }
        public int? ReactionWithoutOb { get; set; }
        public string? TaskGroupName { get; set; }
        public int? Threshold { get; set; }
    }

    public ObView(StorageBusinessObject sb)
    {
        InitializeComponent();

        var cv = new CodeBlockConverter();
        var blks=sb.Traverse(x => x.ProjectTreeChildren).Where(x => CodeBlockConverter.IsConvertableObject(x)).Select(x => cv.Convert(x, null));
        var obs = blks.OfType<CodeBlock>().Where(x => x.BlockType == Siemens.Simatic.Lang.Model.Idents.BlockType.OB);

        var infos = obs.Select(x => new Info()
        {
            Name = x.Name,
            Path = x.StorageBusinessObject.Path,

            EnableActual = x.OrganizationBlockEventData?.EnableActual,
            EnableInitial = x.OrganizationBlockEventData?.EnableInitial,
            EventClassNumber = x.OrganizationBlockEventData?.EventClassNumber,
            EventNumber = x.OrganizationBlockEventData?.EventNumber,
            LastModified = x.OrganizationBlockEventData?.LastModified,
            MaxBufferedEvents = x.OrganizationBlockEventData?.MaxBufferedEvents,
            Priority = x.OrganizationBlockEventData?.Priority,
            ReactionWithoutOb = x.OrganizationBlockEventData?.ReactionWithoutOb,
            TaskGroupName = x.OrganizationBlockEventData?.TaskGroupName,
            Threshold = x.OrganizationBlockEventData?.Threshold,
        });

        lst.ItemsSource = infos;

    }
}