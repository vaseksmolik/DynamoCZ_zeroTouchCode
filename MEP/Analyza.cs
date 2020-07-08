using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Revit.Elements;
using System.Collections;
using Element = Revit.Elements.Element;
using Autodesk.Revit.DB.Plumbing;
using Dynamo.Graph.Nodes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Family = Revit.Elements.Family;
using RevitServices.Persistence;
using System;
using RevitServices.Transactions;
using System.Linq;
using ProtoCore.AST.AssociativeAST;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.Attributes;
using System.Windows;
using Level = Autodesk.Revit.DB.Level;
using System.Data;

namespace DynamoCZ
{
    public class Analyza
    {
        private Analyza() { }

        /// <summary>
        /// Vybere více elementů z dokumentu
        /// </summary>
        /// <returns>List vybraných elementů</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<Element> vyberElementy()
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;
            UIDocument uidoc = new UIDocument(doc);
            List<Element> returnList = new List<Element>();
            while (true)
            {
                try
                {
                    ElementId idVybranehoPrvku = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element).ElementId;
                    returnList.Add(doc.GetElement(idVybranehoPrvku).ToDSType(true));
                }
                catch
                {
                    break;
                }
            }
            return returnList;
        }

        /// <summary>
        /// Detekuje obvodové stěny objektu hraničící s exteriérem. Tento nod využívá metodu "vytvoření místnosti kolem objektu", kde jsou detekovány ohraničující konstrukce. Následně je místnost opět smazána.
        /// </summary>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<Element> DetekujObvodoveSteny()
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;
            List<ElementId> _return = new List<ElementId>();

            using (TransactionGroup trGr = new TransactionGroup(doc, "rollbackTrans"))
            {
                trGr.Start();
                try
                {
                    _return = DynamoCZ.GetOutermostWalls.MetodaOhranicujiciMistnosti(doc);
                }
                catch
                {
                    trGr.RollBack();
                    throw;
                }
                trGr.RollBack();
            }

            if (_return.Count() == 0)
                throw new Exception("Nebyly nalezeny žádné konstrukce !");

            return _return.Select(s => ElementWrapper.ToDSType(doc.GetElement(s), true)).ToList();
        }

        /// <summary>
        /// Najde všechny Výplně otvorů hostované na zadaných konstrukcích.
        /// </summary>
        /// <param name="materskeKonstrukce"></param>
        /// <returns></returns>
        public static List<Element> NajdiHostovaneVyplne(List<Element> materskeKonstrukce)
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;

            List<int> materskeKonstrukceIEsIDsIvs = materskeKonstrukce.Select(s => s.InternalElement.Id.IntegerValue).ToList();
            List<Autodesk.Revit.DB.FamilyInstance> familyInstances = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.FamilyInstance)).Cast<Autodesk.Revit.DB.FamilyInstance>().Where(w => w.Host != null).Where(w => materskeKonstrukceIEsIDsIvs.Contains(w.Host.Id.IntegerValue)).ToList();
            return familyInstances.Select(s => ElementWrapper.ToDSType(s, true)).ToList();
        }


        /// <summary>
        /// Detekuje prostory v objektu a sečte jejich objem.
        /// </summary>
        /// <returns></returns>
        public static double SectiObjemProstoru()
        {

            Document doc = DocumentManager.Instance.CurrentDBDocument;

            List<SpatialElement> spaces = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).Cast<SpatialElement>().Where(w => w is Space).ToList();
            double celkovyObjem = 0;

            if (spaces == null || spaces.Count() == 0)
                throw new Exception("V dokumentu nebyly identifikovány žádné prostory. Prosím vygenerujte je (Analýza -> Prostory -> Automaticky vygenerovat prostory)");

            foreach (Space space in spaces)
                celkovyObjem += space.get_Parameter(BuiltInParameter.ROOM_VOLUME).AsDouble() * (Math.Pow((304.8 / 1000), 3.0));

            if (celkovyObjem == 0)
                throw new Exception("Zjištěný objem prostorů je roven nule !");

            return celkovyObjem;
        }


        /// <summary>
        /// Nod zjistí parametry objektů a zapíše je přehledně do řádků podle vstupních elementů. Výstupní informace lze nadále použít např. pro export / import do csv nebo xls souborů.
        /// </summary>
        /// <param name="elements">Vstupní elementy</param>
        /// <param name="parameterNames">Názvy "sloupců" - parametrů. Pokud parametr neexistuje nebo není přiřazený, bude příslušná buňka prázdná.</param>
        /// <returns>Data pro export.</returns>
        [IsVisibleInDynamoLibrary(true)]
        public static List<List<string>> ZformatujDataProExport(List<Element> elements, List<string> parameterNames)
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;

            TransactionManager.Instance.EnsureInTransaction(doc);

            List<List<string>> _return = DynamoCZ.PrizpusobData.Export(doc, elements.Select(s => s.InternalElement).ToList(), parameterNames);

            TransactionManager.Instance.TransactionTaskDone();

            return _return;
        }
    }
}
