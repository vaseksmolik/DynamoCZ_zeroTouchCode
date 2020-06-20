using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamoCZ.Utils
{
    [IsVisibleInDynamoLibrary(false)]
    public static class Parameters
    {
        public static string AsWhatever(this Parameter p)
        {
            if (!string.IsNullOrEmpty(p.AsString()))
                return p.AsString();
            else
                return p.AsValueString();
        }
    }
}
