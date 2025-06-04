using S7CommPlusDriver;
using static TiaAvaloniaProjectBrowser.Views.MainView;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class OnlineHelper
    {
        public class OnlineTreeItem : SimpleTreeItem
        {
            public uint blockRid { get; set; }

            public S7CommPlusConnection Connection { get; set; }
        }
    }
}
