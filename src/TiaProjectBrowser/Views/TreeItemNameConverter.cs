using Siemens.Simatic.HwConfiguration.Model;
using System.Linq;
using TiaFileFormat.Database.StorageTypes;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class TreeItemNameConverter
    {
        public static string GetName(StorageBusinessObject sb)
        {
            if (sb == null)
                return null;
            if (sb.TiaTypeName == "Siemens.Simatic.HwConfiguration.Model.DeviceData")
            {
                var cpuDevice = sb.GetRelationsWithNameResolved("Siemens.Automation.DomainModel.HWObjectData.DeviceItems").Where(x => x.GetChild<DeviceItemData>() != null && x.GetChild<DeviceItemData>().InvariantTypeName != "Rack").FirstOrDefault();
                if (cpuDevice != null)
                {
                    var cpuType = cpuDevice.GetChild<DeviceItemData>().InvariantTypeName;
                    return sb.Name + " [" + cpuType + "]";
                }
                return sb.ProcessedName;
            }
            return sb.ProcessedName;
        }
    }
}
