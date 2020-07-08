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
using FamilyInstance = Autodesk.Revit.DB.FamilyInstance;

namespace DynamoCZ
{
    class Izolace
    {
        [IsVisibleInDynamoLibrary(true)]
        public static double ZjistiPovrchPotrubi(List<Element> elements)
        {
            Document doc = DocumentManager.Instance.CurrentDBDocument;
            double povrch = 0;

            foreach (Element dynamoElement in elements)
            {
                Autodesk.Revit.DB.Element element = dynamoElement.InternalElement;

                Options options = new Options();
                options.DetailLevel = ViewDetailLevel.Undefined;
                var geometry = element.get_Geometry(options);
                while (!(geometry.First() is Solid))
                {
                    if (geometry.First() is GeometryInstance)
                        geometry = (geometry.First() as GeometryInstance).GetInstanceGeometry();

                }

                Solid union = null;

                foreach (GeometryObject obj in geometry)
                {
                    Solid solid = obj as Solid;

                    if (null != solid
                      && 0 < solid.Faces.Size)
                    {
                        if (null == union)
                        {
                            union = solid;
                        }
                        else
                        {
                            union = BooleanOperationsUtils
                              .ExecuteBooleanOperation(union, solid,
                                BooleanOperationsType.Union);
                        }
                    }
                }

                ConnectorManager connectorManager;
                if (element is FamilyInstance)
                    connectorManager = (element as FamilyInstance).MEPModel.ConnectorManager;
                if (element is MEPCurve)
                    connectorManager = (element as MEPCurve).ConnectorManager;
                else
                    throw new Exception("element není potrubí");

                double povrchKonektoru = 0;
                foreach (Connector connector in connectorManager.Connectors.Cast<Connector>().Where(w => w.Domain == Domain.DomainHvac || w.Domain == Domain.DomainPiping))
                {
                    if (connector.Shape == ConnectorProfileType.Round)
                        povrchKonektoru = Math.PI * Math.Pow(connector.Radius, 2);
                    else if (connector.Shape == ConnectorProfileType.Rectangular)
                        povrchKonektoru = connector.Width * connector.Height;
                    else if (connector.Shape == ConnectorProfileType.Oval)
                        povrchKonektoru = Math.PI * (connector.Width / 2) * (connector.Height / 2);
                    else
                        throw new Exception("potrubí nemá validní průměr");
                }

                double povrchPlaste = union.SurfaceArea - povrchKonektoru;
                povrch += povrchPlaste * (Math.Pow(304.8, 2.0));
            }
            return povrch;
        }
    }
}
