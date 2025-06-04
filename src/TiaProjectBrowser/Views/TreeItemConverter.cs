using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using TiaFileFormat.Database.StorageTypes;
using static TiaAvaloniaProjectBrowser.Views.MainView;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class TreeItemConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if ( value is SimpleTreeItem sti)
            {
                var tbn =  new TextBlock() { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                var binding = new Binding
                {
                    Path = nameof(SimpleTreeItem.Name)
                };
                tbn.Bind(TextBlock.TextProperty, binding);
                return tbn;
            }
            var sb = value as StorageBusinessObject;

            Control ctl = null;
            ctl = StorageObjectToImage.ConvertToImage(sb);

            var tb = new TextBlock() { Text = TreeItemNameConverter.GetName(sb), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            if (ctl == null)
                return tb;
            ctl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            var sp = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            tb.Margin = new Avalonia.Thickness(10, 0, 0, 0);
            sp.Children.Add(ctl);
            sp.Children.Add(tb);
            return sp;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
