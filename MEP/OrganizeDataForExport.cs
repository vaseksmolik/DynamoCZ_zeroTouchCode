using Autodesk.DesignScript.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using DynamoCZ.Utils;

namespace DynamoCZ
{

    [IsVisibleInDynamoLibrary(false)]
    class PrizpusobData
    {
        [Transaction(TransactionMode.Manual)]
        public class ExportCommand : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                using (Transaction tr = new Transaction(doc, "PrizpusobDataProExportCommand"))
                {
                    tr.Start();
                    //var refs = Uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element);
                    //List<Element> pickedElements = refs.Select(s => Doc.GetElement(s.ElementId)).ToList();

                    //SaveFileDialog saveFileDialog = new SaveFileDialog();
                    //saveFileDialog.Filter = "csv files (*.csv)|*.txt|All files (*.*)|*.*";
                    //saveFileDialog.DefaultExt = "csv";
                    //saveFileDialog.FilterIndex = 0;


                    List<Element> pickedElements = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Element>().ToList();
                    List<string> parameterNames = new List<string>() { "Název rodiny", "Název typu", "Plocha", "Součinitel prostupu tepla (U)" };


                    // vlastní metoda pro vytvoření dat
                    var rows = Export(doc, pickedElements, parameterNames);


                    // export do csv
                    string filePath = @"C:\Users\vasek\Desktop\aaa.csv";
                    List<string> rowsJoined = rows.Select(s => string.Join(";", s)).ToList();
                    string csv = "";
                    csv += "sep=;\n";
                    csv += string.Join(",", parameterNames) + "\n";
                    csv += string.Join("\n", rows.Select(s => string.Join(";", s)));
                    File.WriteAllText(filePath, csv.ToString(), Encoding.Default);

                    tr.Commit();
                }
                return Result.Succeeded;
            }

        }

        public static List<List<string>> Export(Document doc, List<Element> elements, List<string> parametersNames)
        {
            List<List<string>> rows = new List<List<string>>();
            rows.Add(parametersNames);

            foreach (Element element in elements)
            {
                List<string> row = new List<string>();
                foreach (string parameterName in parametersNames)
                {
                    var p = element.LookupParameter(parameterName);
                    if (p != null && p.AsValueString() != null)
                    {
                        row.Add(p.AsWhatever());
                    }
                    else
                    {
                        // zkontroluj jestli se nejedná o typový parametr
                        var type = doc.GetElement(element.GetTypeId());
                        var typeParameter = type.LookupParameter(parameterName);
                        if (typeParameter != null)
                        {
                            row.Add(typeParameter.AsWhatever());
                        }
                    }
                }
                rows.Add(row);
            }

            return rows;
        }

    }
}
