using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using TiaFileFormat.Database.StorageTypes;
using TiaFileFormat.Wrappers;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class TreeItemSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return value;

            if (value is INotifyCollectionChanged)
                return value;

            var tp = value.GetType();

            if (!MainView.showAllFolders && value is IEnumerable ienum)
            {
                var lst = new List<object>();
                foreach(var o in ienum)
                {
                    if (o is StorageBusinessObject sb)
                    {
                        if (UnsupportedFolderTypes.ListSubTypes.Contains(sb.CoreAttributes?.Subtype))
                            continue;
                        if (UnsupportedFolderTypes.ListTiaTypes.Contains(sb.TiaTypeName))
                            continue;
                    }
                    lst.Add(o);
                }
                return lst;
            }

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
