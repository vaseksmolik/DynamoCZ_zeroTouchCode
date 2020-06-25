using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RevitServices.Persistence;
using RevitServices.Transactions;
using Autodesk.DesignScript.Geometry;
using Revit.GeometryConversion;
using Line = Autodesk.Revit.DB.Line;
using Curve = Autodesk.Revit.DB.Curve;
using Revit.Elements.Views;

namespace DynamoCZ.MEP
{
    class Views
    {
        /// <summary>
        /// Vytvoří řezy z vybraných křivek.
        /// </summary>
        public static void VytvorRozvinutyRez(List<Autodesk.DesignScript.Geometry.Line> lines, Revit.Elements.PlanView view)
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;
            ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList().First();

            TransactionManager.Instance.EnsureInTransaction(doc);

            foreach (Autodesk.DesignScript.Geometry.Line line in lines)
            {
                Curve rvtCurve = line.ToRevitType();

                Transform t = Transform.Identity;
                t.Origin = rvtCurve.Evaluate(0.5, true);
                t.BasisX = (rvtCurve as Line).Direction;
                t.BasisY = XYZ.BasisZ;
                t.BasisZ = t.BasisY.CrossProduct(t.BasisY);

                BoundingBoxXYZ bbx = new BoundingBoxXYZ();
                bbx.Transform = t;
                bbx.Min = new XYZ(-100, -100, -100);
                bbx.Max = new XYZ(100, 100, 100);
                ViewSection.CreateSection(doc, vft.Id, bbx);

            }
            TransactionManager.Instance.TransactionTaskDone();
        }

        [Transaction(TransactionMode.Manual)]
        public class Command : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                //Application app = uiapp.Application;
                Document doc = uidoc.Document;

                ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList().First(f => f.ViewFamily == ViewFamily.Section);


                List<Reference> listRefs = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element).ToList();
                var lines = listRefs.Select(s => ((doc.GetElement(s.ElementId) as Wall).Location as LocationCurve).Curve as Line).ToList();


                List<ViewSection> listViews = new List<ViewSection>();
                using (Transaction tr = new Transaction(doc, "Command"))
                {
                    tr.Start();
                    ViewSheet viewSheet = ViewSheet.Create(doc, ElementId.InvalidElementId);
                    double endOfLastViewport = 0;
                    foreach (Line line in lines)
                    {
                        //Curve rvtCurve = line.ToRevitType();
                        Curve rvtCurve = line as Curve;

                        Transform t = Transform.Identity;
                        t.Origin = rvtCurve.Evaluate(0.5, true);
                        t.BasisX = (rvtCurve as Line).Direction;
                        t.BasisY = XYZ.BasisZ;
                        t.BasisZ = t.BasisX.CrossProduct(t.BasisY);

                        BoundingBoxXYZ bbx = new BoundingBoxXYZ();
                        bbx.Transform = t;
                        bbx.Min = new XYZ(-t.Origin.DistanceTo(rvtCurve.GetEndPoint(0)), -100, 0);
                        bbx.Max = new XYZ(t.Origin.DistanceTo(rvtCurve.GetEndPoint(1)), 100, 100);
                        ViewSection viewSection = ViewSection.CreateSection(doc, vft.Id, bbx);

                        Viewport.Create(doc, viewSheet.Id, viewSection.Id, new XYZ(endOfLastViewport, 0, 0));
                        endOfLastViewport += rvtCurve.Length / 100;
                    }


                    tr.Commit();
                }
                return Result.Succeeded;
            }

        }
    }
}
