using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamoCZ
{
    [IsVisibleInDynamoLibrary(false)]
    public class GetOutermostWalls
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="view">视图,</param>
        /// <returns></returns>
        public static List<ElementId> MetodaOhranicujiciMistnosti(Document doc)
        {
            double offset = 1000 / 304.8;
            List<Level> levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
            List<ElementId> elementIds = new List<ElementId>();
            foreach (Level level in levels)
            {
                List<Wall> wallList = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().Where(w => w.LevelId == level.Id).Where(w => w.WallType.Function == WallFunction.Exterior).ToList();

                double maxX = -1D;
                double minX = -1D;
                double maxY = -1D;
                double minY = -1D;
                wallList.ForEach((wall) =>
                {
                    Curve curve = (wall.Location as LocationCurve).Curve;
                    XYZ xyz1 = curve.GetEndPoint(0);
                    XYZ xyz2 = curve.GetEndPoint(1);

                    double _minX = Math.Min(xyz1.X, xyz2.X);
                    double _maxX = Math.Max(xyz1.X, xyz2.X);
                    double _minY = Math.Min(xyz1.Y, xyz2.Y);
                    double _maxY = Math.Max(xyz1.Y, xyz2.Y);

                    if (curve.IsCyclic)
                    {
                        Arc arc = curve as Arc;
                        double _radius = arc.Radius;
                        //粗略对x和y 加/减
                        _maxX += _radius;
                        _minX -= _radius;
                        _maxY += _radius;
                        _minY += _radius;
                    }

                    if (minX == -1) minX = _minX;
                    if (maxX == -1) maxX = _maxX;
                    if (maxY == -1) maxY = _maxY;
                    if (minY == -1) minY = _minY;

                    if (_minX < minX) minX = _minX;
                    if (_maxX > maxX) maxX = _maxX;
                    if (_maxY > maxY) maxY = _maxY;
                    if (_minY < minY) minY = _minY;
                });
                minX -= offset;
                maxX += offset;
                minY -= offset;
                maxY += offset;

                CurveArray curves = new CurveArray();
                Line line1 = Line.CreateBound(new XYZ(minX, maxY, 0), new XYZ(maxX, maxY, 0));
                Line line2 = Line.CreateBound(new XYZ(maxX, maxY, 0), new XYZ(maxX, minY, 0));
                Line line3 = Line.CreateBound(new XYZ(maxX, minY, 0), new XYZ(minX, minY, 0));
                Line line4 = Line.CreateBound(new XYZ(minX, minY, 0), new XYZ(minX, maxY, 0));
                curves.Append(line1); curves.Append(line2); curves.Append(line3); curves.Append(line4);


                Room newRoom = null;
                RoomTag tag1 = null;
                View view;
                ModelCurveArray modelCaRoomBoundaryLines;

                var a = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>();
                var b = a.Where(w => !w.IsTemplate && w.GenLevel.Id.IntegerValue != -1).FirstOrDefault(f => f.GenLevel.Id.IntegerValue == level.Id.IntegerValue);
                if (b != null)
                    view = b as View;
                else
                    continue;

                using (Transaction tr = new Transaction(doc, "Command"))
                {
                    tr.Start();
                    SketchPlane sketchPlane = SketchPlane.Create(doc, view.GenLevel.Id);

                    modelCaRoomBoundaryLines = doc.Create.NewRoomBoundaryLines(sketchPlane, curves, view);


                    XYZ point = new XYZ(minX + 100 / 304.8, maxY - 100 / 304.8, 0);


                    newRoom = doc.Create.NewRoom(view.GenLevel, new UV(point.X, point.Y));

                    if (newRoom == null)
                    {
                        string msg = "创建房间失败。";
                        TaskDialog.Show("xx", msg);
                        return null;
                    }
                    tag1 = doc.Create.NewRoomTag(new LinkElementId(newRoom.Id), new UV(point.X, point.Y), view.Id);


                    tr.Commit();
                }
                elementIds.AddRange(DetermineAdjacentElementLengthsAndWallAreas(doc, newRoom));
                using (Transaction tr = new Transaction(doc, "Command"))
                {
                    tr.Start();
                    doc.Delete(tag1.Id);
                    doc.Delete(newRoom.Id);
                    foreach (ModelLine segment in modelCaRoomBoundaryLines)
                    {
                        doc.Delete(segment.Id);
                    }

                    tr.Commit();
                }
            }

            return elementIds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        static List<ElementId> DetermineAdjacentElementLengthsAndWallAreas(Document doc, Room room)
        {
            List<ElementId> elementIds = new List<ElementId>();

            IList<IList<BoundarySegment>> boundaries
              = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

            int n = boundaries.Count;//.Size;

            int iBoundary = 0, iSegment;

            foreach (IList<BoundarySegment> b in boundaries)
            {
                ++iBoundary;
                iSegment = 0;
                foreach (BoundarySegment s in b)
                {
                    ++iSegment;
                    Element neighbour = doc.GetElement(s.ElementId);// s.Element;
                    Curve curve = s.GetCurve();//.Curve;
                    double length = curve.Length;

                    if (neighbour is Wall)
                    {
                        Wall wall = neighbour as Wall;

                        Parameter p = wall.get_Parameter(
                          BuiltInParameter.HOST_AREA_COMPUTED);

                        double area = p.AsDouble();

                        LocationCurve lc
                          = wall.Location as LocationCurve;

                        double wallLength = lc.Curve.Length;

                        if (wall.WallType.Function == WallFunction.Exterior)
                            elementIds.Add(wall.Id);
                    }
                }
            }
            return elementIds;
        }

    }

}
