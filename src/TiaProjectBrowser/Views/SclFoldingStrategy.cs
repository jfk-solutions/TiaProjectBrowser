//using AvaloniaEdit;
//using AvaloniaEdit.Document;
//using AvaloniaEdit.Folding;
//using System;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;
//using System.Xml.Linq;

////public void ConfigureFolding(TextEditor editor)
////{
////    // Holen des FoldingManagers
////    var foldingManager = FoldingManager.Install(editor.TextArea);

////    // Definieren der benutzerdefinierten Folding-Regeln
////    var foldingStrategy = new SclFoldingStrategy();
////    foldingManager.FoldingStrategy = foldingStrategy;
////}

//public class SclFoldingStrategy
//{
//    // Regex für REGION und IF-Erkennung
//    private static readonly Regex regionRegex = new Regex(@"^\s*#\s*region\b", RegexOptions.IgnoreCase);
//    private static readonly Regex endRegionRegex = new Regex(@"^\s*#\s*endregion\b", RegexOptions.IgnoreCase);
//    private static readonly Regex ifRegex = new Regex(@"^\s*IF\b", RegexOptions.IgnoreCase);
//    private static readonly Regex endIfRegex = new Regex(@"^\s*END_IF\b", RegexOptions.IgnoreCase);

//    public void UpdateFoldings(FoldingManager manager, TextDocument document)
//    { 
    
//    }

//    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
//    {
//        var foldings = new List<FoldingSection>();

//        bool inRegion = false;
//        int regionStart = -1;
//        int regionEnd = -1;

//        bool inIfBlock = false;
//        int ifStart = -1;
//        int ifEnd = -1;

//        // Durch alle Zeilen iterieren und nach REGION und IF-Blocks suchen
//        for (int line = 0; line <= document.LineCount; line++)
//        {
           
//            var lineText = document.GetText(document.GetLineByNumber(line));

//            // REGION starten
//            if (regionRegex.IsMatch(lineText))
//            {
//                var fld = new NewFolding();
//                if (!inRegion)
//                {
//                    inRegion = true;
//                    regionStart = line;
//                }
//            }
//            else if (endRegionRegex.IsMatch(lineText))
//            {
//                if (inRegion)
//                {
//                    inRegion = false;
//                    regionEnd = line;
//                    foldings.Add(new FoldingSection(manager,  document.GetLineByNumber(regionStart), document.GetLineByNumber(regionEnd)));
//                }
//            }

//            // IF starten
//            if (ifRegex.IsMatch(lineText))
//            {
//                if (!inIfBlock)
//                {
//                    inIfBlock = true;
//                    ifStart = line;
//                }
//            }
//            else if (endIfRegex.IsMatch(lineText))
//            {
//                if (inIfBlock)
//                {
//                    inIfBlock = false;
//                    ifEnd = line;
//                    foldings.Add(new FoldingSection(manager.Document.GetLineByNumber(ifStart), manager.Document.GetLineByNumber(ifEnd)));
//                }
//            }
//        }

//        // Foldings anwenden
//        manager.UpdateFoldings(foldings, startLine, endLine);
//    }
//}
