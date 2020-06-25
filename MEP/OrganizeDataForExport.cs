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
                        else
                            row.Add("");
                    }
                }
                rows.Add(row);
            }

            return rows;
        }

    }
}
