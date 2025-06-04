using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using TiaFileFormat.Database.BaseObjects;
using TiaFileFormat.Database.Business;
using TiaFileFormat.Database.Business.BaseTypes;
using TiaFileFormat.Database.StorageTypes;

namespace TiaAvaloniaProjectBrowser.Views
{
    public class TreeItemWrapper
    {
        public TreeItemWrapper(object wrappedItem, string name)
        {
            Name = name;
            this.wrappedItem = wrappedItem;

            content = wrappedItem;
        }

        private object wrappedItem;

        public string Name { get; private set; }

        public string StringContent()
        {
            return content.ToString();
        }

        private object content;
        public object Content
        {
            get
            {
                if (content is StoreObjectId soi)
                {
                    var tb = new TextBlock()
                    {
                        Foreground = Brushes.Blue,
                        TextDecorations = TextDecorations.Underline,
                        Text = content?.ToString(),
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    };
                    tb.PointerPressed += (s, e) =>
                    {
                        OpenStoreObject(soi.StorageObject);
                    };
                    return tb;
                }
                else if (content is Relation rel)
                {
                    var tb = new TextBlock()
                    {
                        Foreground = Brushes.Blue,
                        TextDecorations = TextDecorations.Underline,
                        Text = content?.ToString(),
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    };
                    tb.PointerPressed += (s, e) =>
                    {
                        OpenStoreObject(rel.StoreObjectId.StorageObject);
                    };
                    return tb;
                }
                else if (content is ExpandoLink expL)
                {
                    var tb = new TextBlock()
                    {
                        Foreground = Brushes.Blue,
                        TextDecorations = TextDecorations.Underline,
                        Text = content?.ToString(),
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    };
                    tb.PointerPressed += (s, e) =>
                    {
                        OpenStoreObject(expL.LinkedObject);
                    };
                    return tb;
                }
                return new TextBlock() { Text = content?.ToString() };
            }
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

        public IEnumerable<TreeItemWrapper> Children { get { return this.GetChildren(); } }

        IEnumerable<TreeItemWrapper>  GetChildren()
        {
            if (wrappedItem is StoreObjectId)
            {
            }
            else if (wrappedItem is Relation)
            {
            }
            else if (wrappedItem is BaseExpando be)
            {
                foreach (var d in be.Data)
                {
                    yield return new TreeItemWrapper(d.Value, d.Key);
                }
            }
            else if (wrappedItem is BaseRelationList brl)
            {
                int n = 0;
                foreach (var r in brl.Relations)
                {
                    yield return new TreeItemWrapper(r, n++.ToString());
                }
            }
            else if (wrappedItem is BaseBusinessObject bo)
            {
                var tp = bo.GetType();
                foreach (var p in bo.GetSetProperties())
                {
                    var prp = tp.GetProperty(p);
                    if (prp != null)
                    {
                        var val = prp.GetValue(bo);
                        yield return new TreeItemWrapper(val, p);
                    }
                }
            }
            else if (wrappedItem is IEnumerable ienum)
            {
                if (wrappedItem is not string)
                {
                    int n = 0;
                    foreach (var p in ienum)
                    {
                        yield return new TreeItemWrapper(p, n++.ToString());
                    }
                }
            }
            else if (wrappedItem != null)
            {
                if (wrappedItem is not string)
                {
                    var tp = wrappedItem.GetType();
                    if (!tp.IsValueType)
                    {
                        foreach (var p in tp.GetProperties())
                        {
                            if (p.Name != "StorageObjectContainer")
                            {
                                var val = p.GetValue(wrappedItem);
                                yield return new TreeItemWrapper(val, p.Name);
                            }
                        }
                    }
                }
            }
        }
    }
}
