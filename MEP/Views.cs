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
using System.Windows.Forms;

namespace DynamoCZ.MEP
{
    class Views
    {
        /// <summary>
        /// Vytvoří řezy z vybraných křivek.
        /// </summary>
        public static void VytvorRozvinutyRez(
            List<Autodesk.DesignScript.Geometry.Line> lines,
            string nazevRezu = "RozvinutyRez",
            double hloubkaRezu = 2,
            double odsazeniDolu = 0,
            double odsazeniNahoru = 20)
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;
            ViewFamilyType vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList().First(f => f.ViewFamily == ViewFamily.Section);

            //TransactionManager.Instance.EnsureInTransaction(doc);

            var rvtLines = lines.Select(s => s.ToRevitType());

            ViewSheet viewSheet = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList().FirstOrDefault(f => f.Name == nazevRezu);


            List<ViewSection> listViews = new List<ViewSection>();


            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("TransGroup");
                using (Transaction tr = new Transaction(doc, "Command"))
                {
                    tr.Start();

                    if (viewSheet == null)
                    {
                        viewSheet = ViewSheet.Create(doc, ElementId.InvalidElementId);
                        viewSheet.Name = nazevRezu;
                    }

                    tr.Commit();
                }

                XYZ posledniUmisteni = XYZ.Zero;
                XYZ posledniBodLine = rvtLines.First().GetEndPoint(0);

                foreach (Line line in rvtLines)
                {
                    Curve rvtCurve = line as Curve;
                    Viewport viewport;
                    ViewSection viewSection;

                    using (Transaction tr = new Transaction(doc, "Command"))
                    {
                        tr.Start();
                        //Curve rvtCurve = line.ToRevitType();

                        Transform t = Transform.Identity;
                        t.Origin = rvtCurve.Evaluate(0.5, true);
                        t.BasisX = (rvtCurve as Line).Direction.Negate();
                        t.BasisY = XYZ.BasisZ;
                        t.BasisZ = t.BasisX.CrossProduct(t.BasisY);

                        BoundingBoxXYZ bbx = new BoundingBoxXYZ();
                        bbx.Transform = t;
                        bbx.Min = new XYZ(-(rvtCurve.Length / 2), odsazeniDolu, 0);
                        bbx.Max = new XYZ(rvtCurve.Length / 2, odsazeniNahoru, hloubkaRezu);
                        viewSection = ViewSection.CreateSection(doc, vft.Id, bbx);

                        if (Viewport.CanAddViewToSheet(doc, viewSheet.Id, viewSection.Id))
                        {
                            viewport = Viewport.Create(doc, viewSheet.Id, viewSection.Id, XYZ.Zero);
                        }
                        else
                            throw new Exception("Nepodařilo se umístit výřezy do výkresu.");

                        tr.Commit();
                    }

                    using (Transaction tr = new Transaction(doc, "Command"))
                    {
                        tr.Start();

                        // posuň do nuly
                        XYZ zeroPosition = viewport.GetBoxCenter().Negate();
                        ElementTransformUtils.MoveElement(doc, viewport.Id, zeroPosition);

                        double sirka = rvtCurve.Length / viewSection.Scale;
                        posledniUmisteni += new XYZ(sirka / 2, 0, 0);
                        ElementTransformUtils.MoveElement(doc, viewport.Id, posledniUmisteni);

                        posledniUmisteni += new XYZ(sirka / 2, 0, 0);

                        posledniUmisteni += new XYZ(rvtCurve.GetEndPoint(0).DistanceTo(posledniBodLine) / viewSection.Scale, 0, 0);
                        posledniBodLine = rvtCurve.GetEndPoint(1);
                        tr.Commit();
                    }
                }





                transGroup.Assimilate();


            }

            //TransactionManager.Instance.TransactionTaskDone();
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
                double odsazeniNahoru = 20;
                double odsazeniDolu = 0;
                double hloubkaRezu = 2;
                string nazevRezu = "RozvinutyRez";

                ViewSheet viewSheet = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList().FirstOrDefault(f => f.Name == nazevRezu);


                List<ViewSection> listViews = new List<ViewSection>();


                using (TransactionGroup transGroup = new TransactionGroup(doc))
                {
                    transGroup.Start("TransGroup");
                    using (Transaction tr = new Transaction(doc, "Command"))
                    {
                        tr.Start();

                        if (viewSheet == null)
                        {
                            viewSheet = ViewSheet.Create(doc, ElementId.InvalidElementId);
                            viewSheet.Name = nazevRezu;
                        }

                        tr.Commit();
                    }

                    XYZ posledniUmisteni = XYZ.Zero;
                    XYZ posledniBodLine = lines.First().GetEndPoint(0);

                    foreach (Line line in lines)
                    {
                        Curve rvtCurve = line as Curve;
                        Viewport viewport;
                        ViewSection viewSection;

                        using (Transaction tr = new Transaction(doc, "Command"))
                        {
                            tr.Start();
                            //Curve rvtCurve = line.ToRevitType();

                            Transform t = Transform.Identity;
                            t.Origin = rvtCurve.Evaluate(0.5, true);
                            t.BasisX = (rvtCurve as Line).Direction.Negate();
                            t.BasisY = XYZ.BasisZ;
                            t.BasisZ = t.BasisX.CrossProduct(t.BasisY);

                            BoundingBoxXYZ bbx = new BoundingBoxXYZ();
                            bbx.Transform = t;
                            bbx.Min = new XYZ(-(rvtCurve.Length / 2), odsazeniDolu, 0);
                            bbx.Max = new XYZ(rvtCurve.Length / 2, odsazeniNahoru, hloubkaRezu);
                            viewSection = ViewSection.CreateSection(doc, vft.Id, bbx);

                            if (Viewport.CanAddViewToSheet(doc, viewSheet.Id, viewSection.Id))
                            {
                                viewport = Viewport.Create(doc, viewSheet.Id, viewSection.Id, XYZ.Zero);
                            }
                            else
                                throw new Exception("Nepodařilo se umístit výřezy do výkresu.");

                            tr.Commit();
                        }

                        using (Transaction tr = new Transaction(doc, "Command"))
                        {
                            tr.Start();

                            // posuň do nuly
                            XYZ zeroPosition = viewport.GetBoxCenter().Negate();
                            ElementTransformUtils.MoveElement(doc, viewport.Id, zeroPosition);

                            double sirka = rvtCurve.Length / viewSection.Scale;
                            posledniUmisteni += new XYZ(sirka / 2, 0, 0);
                            ElementTransformUtils.MoveElement(doc, viewport.Id, posledniUmisteni);

                            posledniUmisteni += new XYZ(sirka / 2, 0, 0);

                            posledniUmisteni += new XYZ(rvtCurve.GetEndPoint(0).DistanceTo(posledniBodLine) / viewSection.Scale, 0, 0);
                            posledniBodLine = rvtCurve.GetEndPoint(1);
                            tr.Commit();
                        }
                    }





                    transGroup.Assimilate();


                }
                return Result.Succeeded;
            }

        }
    }
}
