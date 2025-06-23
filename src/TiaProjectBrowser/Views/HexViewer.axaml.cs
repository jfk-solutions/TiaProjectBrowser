using Avalonia.Controls;
using AvaloniaHex.Document;

namespace TiaProjectBrowser;

public partial class HexViewer : UserControl
{
    public HexViewer(byte[] data)
    {
        InitializeComponent();

        if (data != null)
        {
            DisplayData(data);
        }
    }

    public void DisplayData(byte[] data)
    {
        editor.Document = new MemoryBinaryDocument(data);
    }
}