using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace ORMAPArrowTools.Utils
{
    /// <summary>
    /// ArrowUtils class.
    /// </summary>
    /// <remarks>Utility class to support arrow feature(s) creation.</remarks>
    public class ArrowUtils
    {
        private readonly MessageHandler _messageHandler = new MessageHandler();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageHandler">MessageHandler object.</param>
        public ArrowUtils(MessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }

        #region Initialization

        /// <summary>
        /// Initialize feature(s) creation.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task InitializeFeatureCreation()
        {
            try
            {
                bool success = await SetScale();
                if (!success)
                {
                    return;
                }

                ArrowContext.UpdateEndPoint = false;

                InitializeObjectIdProperties();

                MapPoint mapPoint = ArrowContext.MapPoints[0];

                if (ArrowContext.DevShowDiagnosticPoints)
                {
                    await CreatePointFeature(mapPoint);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Initialize dictionary for feature object Ids.
        /// </summary>
        public void InitializeObjectIdProperties()
        {
            try
            {
                ArrowContext.CurrentArrowObjectIds = new Dictionary<int, long>
                {
                    [1] = ArrowContext.ObjectIdDefault,
                    [2] = ArrowContext.ObjectIdDefault
                };

                ArrowContext.CurrentPointObjectId = ArrowContext.ObjectIdDefault;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Sets ArrowContext.ArrowScale according to MapIndex polygon containing first click point.
        /// </summary>
        /// <remarks>Default scale value = 1 (1"=100').</remarks>
        /// <returns>Task<bool> - value indicating whether operation was successful.</returns>
        private async Task<bool> SetScale()
        {
            bool success = false;

            try
            {
                ArrowContext.ArrowScale = ArrowContext.ArrowScaleDefault;

                MapPoint firstPoint = ArrowContext.MapPoints[0];

                BasicFeatureLayer basicFeatureLayer = GetBasicFeatureLayer(ArrowContext.MapIndexLayerName);
                if (basicFeatureLayer == null)
                {
                    string menuCaption = ArrowContext.MenuItems.CANCEL;
                    await ApplyMenuAction(menuCaption);
                    return success;
                }

                await QueuedTask.Run(() =>
                {
                    SpatialQueryFilter spatialQueryFilter = new SpatialQueryFilter
                    {
                        FilterGeometry = firstPoint,
                        SpatialRelationship = SpatialRelationship.Intersects
                    };

                    RowCursor rowCursor = basicFeatureLayer.Search(spatialQueryFilter);

                    if (rowCursor.MoveNext())
                    {
                        Row row = rowCursor.Current;
                        double mapScale = Convert.ToDouble(row[ArrowContext.MapScaleFieldName]);
                        ArrowContext.ArrowScale = mapScale / 1200;
                    }
                });

                success = true;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return success;
        }

        /// <summary>
        /// Set general properties for feature creation.
        /// </summary>
        /// <param name="currentTemplateLayer">Current template layer.</param>
        /// <returns>Task.</returns>
        public async Task SetGeneralProperties(Layer currentTemplateLayer)
        {
            try
            {
                ArrowContext.AssemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                ArrowContext.XMLFilePath = Path.Combine(ArrowContext.AssemblyFolderPath, "Resources", ArrowContext.XMLFileName);

                await SetSpatialReference(currentTemplateLayer);

                SetSearchShapeBufferDistance();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Set spatial reference.
        /// </summary>
        /// <param name="currentTemplateLayer">Current template layer.</param>
        /// <returns>Task.</returns>
        private async Task SetSpatialReference(Layer currentTemplateLayer)
        {
            ArrowContext.SpatialReference = await QueuedTask.Run(() =>
            {
                SpatialReference spatialReference = null;

                try
                {
                    FeatureLayer featureLayer = currentTemplateLayer as FeatureLayer;

                    Table featureClass = featureLayer.GetTable();

                    FeatureClassDefinition featureClassDefinition = featureClass.GetDefinition() as FeatureClassDefinition;

                    spatialReference = featureClassDefinition.GetSpatialReference();
                }
                catch (Exception exception)
                {
                    Exception catchException = new Exception("", exception);
                    _messageHandler.ProcessErrorDetails(catchException);
                }

                return spatialReference;
            });
        }

        /// <summary>
        /// Set search shape buffer distance.
        /// </summary>
        private void SetSearchShapeBufferDistance()
        {
            try
            {
                Unit unit = ArrowContext.SpatialReference.Unit;

                switch (unit.Name)
                {
                    case "Foot":
                        ArrowContext.SearchShapeBufferDistance = 5;
                        break;

                    case "Meter":
                        ArrowContext.SearchShapeBufferDistance = 1.524;
                        break;
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Set ArrowContext.ArrowScale variable.
        /// </summary>
        /// <param name="menuScaleLabel">Menu scale label value.</param>
        private void SetArrowScale(string menuScaleLabel)
        {
            try
            {
                ArrowContext.ScaleValues scaleValues = ArrowContext.ScaleLabelValuesPairs[menuScaleLabel];
                ArrowContext.ArrowScale = scaleValues.ArrowScaleValue;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Initialization

        #region Layers

        /// <summary>
        /// Check layer is present in table of contents.
        /// </summary>
        /// <param name="layerName">Layer name.</param>
        /// <returns>Value indicating whether layer is present.</returns>
        public bool CheckLayerPresent(string layerName)
        {
            bool layerPresent = false;

            try
            {
                BasicFeatureLayer basicFeatureLayer = GetBasicFeatureLayer(layerName);
                if (basicFeatureLayer != null)
                {
                    layerPresent = true;
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return layerPresent;
        }

        /// <summary>
        /// Get basicFeatureLayer.
        /// </summary>
        /// <param name="layerName">Layer name.</param>
        /// <returns>BasicFeatureLayer object.</returns>
        private BasicFeatureLayer GetBasicFeatureLayer(string layerName, bool showMessage = true)
        {
            BasicFeatureLayer basicFeatureLayer = null;

            try
            {
                basicFeatureLayer = MapView.Active.Map.FindLayers(layerName).FirstOrDefault() as BasicFeatureLayer;

                if (showMessage && basicFeatureLayer == null)
                {
                    string message = $"The required layer \'{layerName}\' could not be found. Please add the layer and try again.";
                    _ = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return basicFeatureLayer;
        }

        #endregion Layers

        #region Line Features

        /// <summary>
        /// Create/update arrow feature.
        /// </summary>
        /// <param name="editOpType">Edit operation type.</param>
        /// <returns>Task.</returns>
        public async Task CreateUpdateArrowFeatures(EditOpType editOpType)
        {
            try
            {
                List<Polyline> polylines = null;

                if (ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.SingleArrow)
                {
                    if (ArrowContext.DevShowDiagnosticLines)
                    {
                        Polyline devPolyline = PolylineBuilder.CreatePolyline(ArrowContext.MapPoints, ArrowContext.SpatialReference);
                        if (ArrowContext.SwitchArrowheads)
                        {
                            devPolyline = (Polyline)GeometryEngine.Instance.ReverseOrientation(devPolyline);
                        }
                        await ApplyArrowEditOperation(EditOpType.Create, -1, devPolyline);
                    }

                    Polyline polyline = CreateSingleArrowPolyline();
                    polylines = new List<Polyline>
                    {
                        polyline
                    };
                }
                else
                {
                    polylines = CreateXMLBasedPolylines();
                }

                int arrowNumber = 1;
                foreach (Polyline polyline in polylines)
                {
                    await ApplyArrowEditOperation(editOpType, arrowNumber, polyline);
                    arrowNumber++;
                }

                await UpdateSelection();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Enum for use in edit operations.
        /// </summary>
        public enum EditOpType
        {
            Create,
            Modify,
            Delete,
            NotSet
        }

        /// <summary>
        /// Apply arrow edit operation.
        /// </summary>
        /// <param name="editOpType">Edit operation type.</param>
        /// <param name="arrowNumber">Arrow number (1 or 2).</param>
        /// <param name="polyline">Arrow polyline.</param>
        /// <returns>Task.</returns>
        private async Task ApplyArrowEditOperation(EditOpType editOpType, int arrowNumber, Polyline polyline = null)
        {
            try
            {
                await QueuedTask.Run(async () =>
                {
                    try
                    {
                        // Create an edit operation.
                        string name = $"{editOpType} {ArrowContext.CurrentTemplate.Layer.Name.Trim()} {ArrowContext.CurrentArrowType.Style} Arrow";
                        EditOperation editOperation = new EditOperation
                        {
                            Name = name,
                            SelectNewFeatures = false
                        };

                        long newObjectId = -2;

                        long objectId = -1;
                        if (editOpType == EditOpType.Modify || editOpType == EditOpType.Delete)
                        {
                            objectId = ArrowContext.CurrentArrowObjectIds[arrowNumber];
                        }

                        // Queue feature creation.
                        switch (editOpType)
                        {
                            case EditOpType.Create:
                                editOperation.Create(ArrowContext.CurrentTemplate, polyline, oid => newObjectId = oid);
                                break;
                            case EditOpType.Modify:
                                editOperation.Modify(ArrowContext.CurrentTemplate.Layer, objectId, polyline);
                                break;
                            case EditOpType.Delete:
                                editOperation.Delete(ArrowContext.CurrentTemplate.Layer, objectId);
                                ArrowContext.CurrentArrowObjectIds[arrowNumber] = ArrowContext.ObjectIdDefault;
                                break;
                            default:
                                break;
                        }

                        bool editOperationResult = await editOperation.ExecuteAsync();

                        if (editOpType == EditOpType.Create && arrowNumber != -1)
                        {
                            ArrowContext.CurrentArrowObjectIds[arrowNumber] = newObjectId;
                        }
                    }
                    catch (Exception exception)
                    {
                        Exception catchException = new Exception("", exception);
                        _messageHandler.ProcessErrorDetails(catchException);
                    }
                });
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Create single arrow polyline.
        /// </summary>
        /// <returns>Polyline.</returns>
        private Polyline CreateSingleArrowPolyline()
        {
            Polyline polyline = null;

            try
            {
                switch (ArrowContext.CurrentArrowType.Style)
                {
                    case ArrowContext.ArrowStyle.Straight:
                        polyline = PolylineBuilder.CreatePolyline(ArrowContext.MapPoints, ArrowContext.SpatialReference);
                        break;

                    case ArrowContext.ArrowStyle.Leader:
                        polyline = PolylineBuilder.CreatePolyline(ArrowContext.MapPoints, ArrowContext.SpatialReference);
                        break;

                    case ArrowContext.ArrowStyle.Zigzag:
                        polyline = CreateZigzagPolyline();
                        break;
                }

                if (ArrowContext.SwitchArrowheads)
                {
                    polyline = (Polyline)GeometryEngine.Instance.ReverseOrientation(polyline);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return polyline;
        }

        /// <summary>
        /// Create single arrow zigzag polyline.
        /// </summary>
        /// <returns>Polyline.</returns>
        private Polyline CreateZigzagPolyline()
        {
            Polyline polyline = null;

            try
            {
                int offset = 1;
                if (ArrowContext.FlipArrows)
                {
                    offset = -1;
                }

                MapPoint templatePoint1 = MapPointBuilder.CreateMapPoint(0, 0, ArrowContext.SpatialReference);
                MapPoint templatePoint2 = MapPointBuilder.CreateMapPoint(ArrowContext.ZigzagPosition, 0, ArrowContext.SpatialReference);
                MapPoint templatePoint3 = MapPointBuilder.CreateMapPoint(ArrowContext.ZigzagPosition, ArrowContext.ZigzagWidth * offset, ArrowContext.SpatialReference);
                MapPoint templatePoint4 = MapPointBuilder.CreateMapPoint(20, ArrowContext.ZigzagWidth * offset, ArrowContext.SpatialReference);

                // First segment - straight.
                List<MapPoint> firstPolylinePoints = new List<MapPoint>
                {
                    templatePoint1,
                    templatePoint2
                };
                Polyline firstPolyline = PolylineBuilder.CreatePolyline(firstPolylinePoints, ArrowContext.SpatialReference);

                if (ArrowContext.ZigzagCurve < 0)
                {
                    ArrowContext.ZigzagCurve = 0;
                }

                // Second segment - Bezier.
                Coordinate2D controlPoint1 = new Coordinate2D(templatePoint2.X + ArrowContext.ZigzagCurve, templatePoint2.Y);
                Coordinate2D controlPoint2 = new Coordinate2D(templatePoint3.X - ArrowContext.ZigzagCurve, templatePoint3.Y);

                // Builder convenience methods don't need to run on the MCT.
                CubicBezierSegment bezier = CubicBezierBuilder.CreateCubicBezierSegment(templatePoint2, controlPoint1, controlPoint2, templatePoint3, ArrowContext.SpatialReference);
                Polyline bezierPolyline = PolylineBuilder.CreatePolyline(bezier);

                // Third segment - straight.
                List<MapPoint> lastPolylinePoints = new List<MapPoint>
                {
                    templatePoint3,
                    templatePoint4
                };
                Polyline lastPolyline = PolylineBuilder.CreatePolyline(lastPolylinePoints, ArrowContext.SpatialReference);

                List<Polyline> polylines = new List<Polyline>
                {
                    firstPolyline,
                    bezierPolyline,
                    lastPolyline
                };

                Polyline templatePolyline = PolylineBuilder.CreatePolyline(polylines, ArrowContext.SpatialReference);

                MapPoint firstMapPoint = ArrowContext.MapPoints[0];
                MapPoint lastMapPoint = ArrowContext.MapPoints[1];

                List<MapPoint> firstLastMapPoints = new List<MapPoint>
                {
                    firstMapPoint,
                    lastMapPoint
                };

                Polyline firstLastMapPolyline = PolylineBuilder.CreatePolyline(firstLastMapPoints, ArrowContext.SpatialReference);
                double firstLastMapPolylineAngle = GetSingleSegmentPolylineAngle(firstLastMapPolyline);

                Polyline templateFromToPolyline = CreateFromToPolylineFromPolyline(templatePolyline);
                double templateFromToPolylineAngle = GetSingleSegmentPolylineAngle(templateFromToPolyline);

                double rotateAngle = firstLastMapPolylineAngle - templateFromToPolylineAngle;

                double scale = firstLastMapPolyline.Length / templateFromToPolyline.Length;

                List<Polyline> adjustedPolylines = AdjustPolylines(templatePolyline, null, rotateAngle, firstMapPoint, lastMapPoint, scale);
                polyline = adjustedPolylines[0];
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return polyline;
        }

        /// <summary>
        /// Create straight polyline using the from and to points for a given polyline.
        /// </summary>
        /// <param name="polyline">Input polyline.</param>
        /// <returns>Polyline.</returns>
        private Polyline CreateFromToPolylineFromPolyline(Polyline polyline)
        {
            Polyline fromToPolyline = null;

            try
            {
                ReadOnlyPointCollection polyLinePoints = polyline.Points;
                MapPoint fromPoint = polyLinePoints[0];
                MapPoint toPoint = polyLinePoints[polyLinePoints.Count - 1];

                List<MapPoint> firstLastMapPoints = new List<MapPoint>
                {
                    fromPoint,
                    toPoint
                };

                fromToPolyline = PolylineBuilder.CreatePolyline(firstLastMapPoints, ArrowContext.SpatialReference);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return fromToPolyline;
        }

        /// <summary>
        /// Get angle (radians) from the horizontal axis for given polyline.
        /// </summary>
        /// <param name="polyline">Input polyline.</param>
        /// <returns>Angle (radians).</returns>
        private double GetSingleSegmentPolylineAngle(Polyline polyline)
        {
            double angle = -999;

            try
            {
                // Assume one segment in polyline.
                List<Segment> segmentList = new List<Segment>();
                ICollection<Segment> segmentCollection = segmentList;
                polyline.GetAllSegments(ref segmentCollection);
                LineSegment lineSegment = (LineSegment)segmentList[0];
                angle = lineSegment.Angle;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return angle;
        }

        /// <summary>
        /// Finish arrow feature(s) construction.
        /// </summary>
        private void FinishArrows()
        {
            try
            {
                InitializeConstructionProperties();

                InitializeObjectIdProperties();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Create arrow feature(s) using template provided in XML settings file.
        /// </summary>
        /// <returns>List of polylines.</returns>
        private List<Polyline> CreateXMLBasedPolylines()
        {
            List<Polyline> adjustedPolylines = null;

            try
            {
                ArrowContext.ArrowCategory arrowCategory = ArrowContext.CurrentArrowType.Category;
                List<List<Coordinate2D>> segmentCoordPairs = GetXMLSegmentCoordPairs(arrowCategory);

                double scaleFactor = 0;
                if (ArrowContext.MapPoints.Count == 3)
                {
                    List<Coordinate2D> lastSegmentCoordPair = segmentCoordPairs[segmentCoordPairs.Count - 1];
                    Coordinate2D toCoordinate2D = lastSegmentCoordPair[1];
                    scaleFactor = ArrowContext.ArrowOffset / toCoordinate2D.Y / ArrowContext.ArrowScale;
                }
                else
                {
                    scaleFactor = 1;
                }

                int index = 0;
                List<Polyline> leftSegments = new List<Polyline>();
                List<Polyline> rightSegments = new List<Polyline>();
                foreach (List<Coordinate2D> segmentCoordPair in segmentCoordPairs)
                {
                    Coordinate2D xmlFromPoint = segmentCoordPair[0];
                    Coordinate2D xmlToPoint = segmentCoordPair[1];

                    // Left arrow template segment.
                    double leftFromX = xmlFromPoint.X;
                    double leftFromY = index == 0 ? xmlFromPoint.Y : xmlFromPoint.Y * scaleFactor;
                    double leftToX = xmlToPoint.X;
                    double leftToY = xmlToPoint.Y * scaleFactor;
                    Polyline polyline = CreatePolylineFromCoordValues(leftFromX, leftFromY, leftToX, leftToY);
                    leftSegments.Add(polyline);

                    // Right arrow.
                    double rightFromX = leftFromX * -1;
                    double rightFromY = leftFromY;
                    double rightToX = leftToX * -1;
                    bool invert = ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.LandHook &&
                        index == segmentCoordPairs.Count - 1;
                    double rightToY = invert ? leftToY * -1 : leftToY;
                    polyline = CreatePolylineFromCoordValues(rightFromX, rightFromY, rightToX, rightToY);
                    rightSegments.Add(polyline);

                    index++;
                }

                Polyline leftTemplatePolyline = PolylineBuilder.CreatePolyline(leftSegments, ArrowContext.SpatialReference);
                Polyline rightTemplatePolyline = PolylineBuilder.CreatePolyline(rightSegments, ArrowContext.SpatialReference);

                MapPoint firstMapPoint = ArrowContext.MapPoints[0];
                MapPoint lastMapPoint = ArrowContext.MapPoints[1];

                List<MapPoint> firstLastMapPoints = new List<MapPoint>
                {
                    firstMapPoint,
                    lastMapPoint
                };

                Polyline firstLastMapPolyline = PolylineBuilder.CreatePolyline(firstLastMapPoints, ArrowContext.SpatialReference);
                double firstLastMapPolylineAngle = GetSingleSegmentPolylineAngle(firstLastMapPolyline);
                double scale = ArrowContext.ArrowScale;

                adjustedPolylines = null;
                if (ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.RoadTic)
                {
                    rightTemplatePolyline = null;
                    adjustedPolylines = AdjustPolylines(leftTemplatePolyline, rightTemplatePolyline, firstLastMapPolylineAngle, firstMapPoint, lastMapPoint, scale);
                }
                else
                {
                    adjustedPolylines = AdjustPolylines(leftTemplatePolyline, rightTemplatePolyline, firstLastMapPolylineAngle, firstMapPoint, lastMapPoint, scale);
                }

                if (ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.Straight && ArrowContext.FlipArrows)
                {
                    for (int i = 0; i < adjustedPolylines.Count; i++)
                    {
                        Polyline polyline = adjustedPolylines[i];
                        adjustedPolylines[i] = FlipPolyline(polyline);
                    }
                }

                if (ArrowContext.SwitchArrowheads)
                {
                    for (int i = 0; i < adjustedPolylines.Count; i++)
                    {
                        Polyline polyline = adjustedPolylines[i];
                        adjustedPolylines[i] = (Polyline)GeometryEngine.Instance.ReverseOrientation(polyline);
                    }
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return adjustedPolylines;
        }

        /// <summary>
        /// Get segment coordinate pairs from arrow settings XML file.
        /// </summary>
        /// <param name="arrowCategory">Arrow category.</param>
        /// <returns>List of lists of coordinates (pairs).</returns>
        private List<List<Coordinate2D>> GetXMLSegmentCoordPairs(ArrowContext.ArrowCategory arrowCategory)
        {
            List<List<Coordinate2D>> segmentCoordPairs = null;

            try
            {
                string level3ElementName = "";

                switch (arrowCategory)
                {
                    case ArrowContext.ArrowCategory.Straight:
                        level3ElementName = "straight";
                        break;

                    case ArrowContext.ArrowCategory.LandHook:
                        if (ArrowContext.FlipArrows)
                        {
                            level3ElementName = "landHookFlipped";
                        }
                        else
                        {
                            level3ElementName = "landHook";
                        }
                        break;

                    case ArrowContext.ArrowCategory.NoDashes:
                        level3ElementName = "curved0";
                        break;

                    case ArrowContext.ArrowCategory.OneDash:
                        level3ElementName = "curved1";

                        break;
                    case ArrowContext.ArrowCategory.TwoDashes:
                        level3ElementName = "curved2";

                        break;
                    case ArrowContext.ArrowCategory.ThreeDashes:
                        level3ElementName = "curved3";
                        break;

                    case ArrowContext.ArrowCategory.FourDashes:
                        level3ElementName = "curved4";
                        break;

                    case ArrowContext.ArrowCategory.RoadTic:
                        level3ElementName = "roadTic";
                        break;
                }

                XElement arrowXML = XElement.Load(ArrowContext.XMLFilePath);

                string segmentCountString = arrowXML.Descendants("arrowDef").Descendants(level3ElementName).Descendants("segments").Select(x => (string)x.Attribute("count")).FirstOrDefault();
                int segmentCount = Convert.ToInt32(segmentCountString);

                List<string> segmentPointsStrings = arrowXML.Descendants("arrowDef").Descendants(level3ElementName).Descendants("segment").Select(x => (string)x.Attribute("points")).ToList();

                int stringsCount = segmentPointsStrings.Count();
                if (stringsCount != segmentCount)
                {
                    string message = $"XML segment data mismatch:\nSegment count: {segmentCount}\nNumber of segment points elements: {stringsCount}\nPlease check the file '{ArrowContext.XMLFilePath}'";
                }

                segmentCoordPairs = new List<List<Coordinate2D>>();
                foreach (string segmentPointString in segmentPointsStrings)
                {
                    double[] doubles = Array.ConvertAll(segmentPointString.Split(','), new Converter<string, double>(double.Parse));
                    Coordinate2D fromCoordinate2D = new Coordinate2D(doubles[0], doubles[1]);
                    Coordinate2D toCoordinate2D = new Coordinate2D(doubles[2], doubles[3]);
                    List<Coordinate2D> segmentCoordPair = new List<Coordinate2D>
                    {
                        fromCoordinate2D,
                        toCoordinate2D
                    };
                    segmentCoordPairs.Add(segmentCoordPair);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return segmentCoordPairs;
        }

        /// <summary>
        /// Adjust template polyline(s) according to click points.
        /// </summary>
        /// <param name="leftTemplatePolyline">Left template polyline generated from XML values.</param>
        /// <param name="rightTemplatePolyline">Right template polyline generated from XML values.</param>
        /// <param name="firstLastMapPolylineAngle">Angle (radians) between straight line from first to last map points and horizontal axis.</param>
        /// <param name="firstMapPoint">First map point.</param>
        /// <param name="lastMapPoint">Last map point.</param>
        /// <param name="scale">Arrow feature(s) scale.</param>
        /// <returns>List of polylines.</returns>
        private List<Polyline> AdjustPolylines(Polyline leftTemplatePolyline, Polyline rightTemplatePolyline, double firstLastMapPolylineAngle, MapPoint firstMapPoint, MapPoint lastMapPoint, double scale)
        {
            List<Polyline> adjustedPolylines = null;

            try
            {
                adjustedPolylines = new List<Polyline>();

                MapPoint leftTemplateFromMapPoint = leftTemplatePolyline.Points[0];
                Polyline leftRotatedPolyline = (Polyline)GeometryEngine.Instance.Rotate(leftTemplatePolyline, leftTemplateFromMapPoint, firstLastMapPolylineAngle);

                MapPoint leftRotatedFromPoint = leftRotatedPolyline.Points[0];
                Polyline leftScaledPolyline = (Polyline)GeometryEngine.Instance.Scale(leftRotatedPolyline, leftRotatedFromPoint, scale, scale);

                Polyline leftMovedPolyline = (Polyline)GeometryEngine.Instance.Move(leftScaledPolyline, firstMapPoint.X, firstMapPoint.Y);

                adjustedPolylines = new List<Polyline>
                {
                    leftMovedPolyline
                };

                if (rightTemplatePolyline != null)
                {
                    Polyline rightRotatedPolyline = (Polyline)GeometryEngine.Instance.Rotate(rightTemplatePolyline, leftTemplateFromMapPoint, firstLastMapPolylineAngle);

                    MapPoint rightRotatedFromPoint = rightRotatedPolyline.Points[0];
                    Polyline rightScaledPolyline = (Polyline)GeometryEngine.Instance.Scale(rightRotatedPolyline, rightRotatedFromPoint, scale, scale);

                    Polyline rightMovedPolyline = (Polyline)GeometryEngine.Instance.Move(rightScaledPolyline, lastMapPoint.X, lastMapPoint.Y);

                    adjustedPolylines.Add(rightMovedPolyline);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return adjustedPolylines;
        }

        /// <summary>
        /// Create polyline from end coordinate values.
        /// </summary>
        /// <param name="fromX">From point X.</param>
        /// <param name="fromY">From point Y.</param>
        /// <param name="toX">To point X.</param>
        /// <param name="toY">To point Y.</param>
        /// <returns>Polyline.</returns>
        private Polyline CreatePolylineFromCoordValues(double fromX, double fromY, double toX, double toY)
        {
            Polyline polyline = null;

            try
            {
                Coordinate2D fromPoint = new Coordinate2D()
                {
                    X = fromX,
                    Y = fromY
                };

                Coordinate2D toPoint = new Coordinate2D()
                {
                    X = toX,
                    Y = toY
                };

                List<Coordinate2D> points = new List<Coordinate2D>
                {
                    fromPoint,
                    toPoint
                };

                polyline = PolylineBuilder.CreatePolyline(points, ArrowContext.SpatialReference);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return polyline;
        }

        /// <summary>
        /// Flip polyline.
        /// </summary>
        /// <param name="polyline">Input polyline.</param>
        /// <returns>Flipped polyline.</returns>
        private Polyline FlipPolyline(Polyline polyline)
        {
            Polyline flippedPolyline = null;

            try
            {
                // Rotate polyline 180 degrees about from point.
                MapPoint fromPoint = polyline.Points[0];
                double rotateAngle = 3.14159265;
                Polyline rotatedPolyline = (Polyline)GeometryEngine.Instance.Rotate(polyline, fromPoint, rotateAngle);

                flippedPolyline = rotatedPolyline;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return flippedPolyline;
        }

        /// <summary>
        /// Determine whether current arrow category is DimensionArrows according to dashes categories.
        /// </summary>
        /// <returns>Value indicating whether current arrow category is DimensionArrows.</returns>
        public bool IsCurrentArrowCategoryDimensionArrows()
        {
            bool isCurrentArrowCategoryDimensionArrows = false;

            try
            {
                isCurrentArrowCategoryDimensionArrows = ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.NoDashes ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.OneDash ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.TwoDashes ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.ThreeDashes ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.FourDashes;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return isCurrentArrowCategoryDimensionArrows;
        }

        /// <summary>
        /// Reset current feature(s) creation process.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ResetCurrentFeatureCreation()
        {
            try
            {
                if (!Project.Current.IsEditingEnabled)
                {
                    return;
                }

                if (ArrowContext.CurrentArrowObjectIds != null)
                {
                    EditOpType editOpType = EditOpType.Delete;
                    for (int arrowNumber = 1; arrowNumber < 3; arrowNumber++)
                    {
                        long objectId = ArrowContext.CurrentArrowObjectIds[arrowNumber];
                        if (objectId != ArrowContext.ObjectIdDefault)
                        {
                            await ApplyArrowEditOperation(editOpType, arrowNumber);
                        }
                    }
                }

                InitializeConstructionProperties();

                if (ArrowContext.DevShowDiagnosticPoints)
                {
                    await ClearTempPointsLayer();
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Initialize construction properties.
        /// </summary>
        private void InitializeConstructionProperties()
        {
            try
            {
                ArrowContext.UpdateEndPoint = false;
                ArrowContext.UpdateOffset = false;
                ArrowContext.MapPoints.Clear();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Line Features

        #region Point Features

        /// <summary>
        /// Create point feature.
        /// </summary>
        /// <param name="mapPoint">Map point.</param>
        /// <returns>Task.</returns>
        public async Task CreatePointFeature(MapPoint mapPoint)
        {
            try
            {
                EditOpType editOpType = EditOpType.Create;

                await ApplyPointEditOperation(editOpType, mapPoint);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Apply point edit operation.
        /// </summary>
        /// <param name="editOpType">Edit operation type.</param>
        /// <param name="mapPoint">Map point (required for creation).</param>
        /// <param name="setCurrentObjectId">Set ArrowContext.CurrentPointObjectId, if required.</param>
        /// <returns>Task.</returns>
        private async Task ApplyPointEditOperation(EditOpType editOpType, MapPoint mapPoint = null, bool setCurrentObjectId = true)
        {
            try
            {
                await QueuedTask.Run(async () =>
                {
                    try
                    {
                        BasicFeatureLayer basicFeatureLayer = GetBasicFeatureLayer(ArrowContext.TempPointsLayerName);
                        if (basicFeatureLayer == null)
                        {
                            return;
                        }

                        FeatureLayer tempPointsFeatureLayer = (FeatureLayer)basicFeatureLayer;

                        EditingTemplate editingTemplate = tempPointsFeatureLayer.GetTemplate(ArrowContext.TempPointsLayerName);

                        // Create an edit operation.
                        string name = $"{editOpType} {ArrowContext.TempPointsLayerName} point";
                        EditOperation editOperation = new EditOperation
                        {
                            Name = name,
                            SelectNewFeatures = false
                        };

                        long newObjectId = -2;

                        // Queue feature creation.
                        switch (editOpType)
                        {
                            case EditOpType.Create:
                                editOperation.Create(editingTemplate, mapPoint, oid => newObjectId = oid);
                                break;
                            case EditOpType.Delete:
                                editOperation.Delete(tempPointsFeatureLayer, ArrowContext.CurrentPointObjectId);
                                ArrowContext.CurrentPointObjectId = ArrowContext.ObjectIdDefault;
                                break;
                            default:
                                break;
                        }

                        _ = await editOperation.ExecuteAsync();

                        if (editOpType == EditOpType.Create && setCurrentObjectId)
                        {
                            ArrowContext.CurrentPointObjectId = newObjectId;
                        }
                    }
                    catch (Exception exception)
                    {
                        Exception catchException = new Exception("", exception);
                        _messageHandler.ProcessErrorDetails(catchException);
                    }
                });
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Clear temp points layer.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ClearTempPointsLayer()
        {
            try
            {
                bool showMessage = false;
                BasicFeatureLayer basicFeatureLayer = GetBasicFeatureLayer(ArrowContext.TempPointsLayerName, showMessage);
                if (basicFeatureLayer == null)
                {
                    return;
                }

                FeatureLayer tempPointsFeatureLayer = (FeatureLayer)basicFeatureLayer;

                ArrowContext.CurrentPointObjectId = ArrowContext.ObjectIdDefault;

                IReadOnlyList<string> valueArray = await QueuedTask.Run(() =>
                {
                    return Geoprocessing.MakeValueArray(ArrowContext.TempPointsLayerName);
                });

                _ = await Geoprocessing.ExecuteToolAsync("management.TruncateTable", valueArray);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Point Features

        #region Selection

        /// <summary>
        /// Update feature selection for current layer.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task UpdateSelection()
        {
            try
            {
                if (ArrowContext.SelectNewArrows)
                {
                    UpdateSessionArrowObjectIds();

                    await SetSelection();
                }
                else
                {
                    await ClearArrowSelection();
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Update list of object Ids for current feature(s) selection.
        /// </summary>
        private void UpdateSessionArrowObjectIds()
        {
            try
            {
                if (ArrowContext.SessionArrowObjectIds == null)
                {
                    ArrowContext.SessionArrowObjectIds = new List<long>();
                }

                foreach (KeyValuePair<int, long> keyValuePair in ArrowContext.CurrentArrowObjectIds)
                {
                    long objectId = keyValuePair.Value;

                    if (objectId != -1)
                    {
                        ArrowContext.SessionArrowObjectIds.Add(objectId);
                    }
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Check number of selected features and clear list of object Ids, if required.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task CheckClearSessionArrowSelection()
        {
            try
            {
                int selectionCount = await GetSelectionCount();
                if (selectionCount == 0)
                {
                    if (ArrowContext.SessionArrowObjectIds != null)
                    {
                        ArrowContext.SessionArrowObjectIds.Clear();
                    }
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Get selection count for current layer.
        /// </summary>
        /// <returns>Task<int> - number of selected features.</returns>
        private async Task<int> GetSelectionCount()
        {
            int selectionCount = 0;

            try
            {
                if (ArrowContext.SelectNewArrows)
                {
                    await QueuedTask.Run(() =>
                    {
                        BasicFeatureLayer basicFeatureLayer = (BasicFeatureLayer)ArrowContext.CurrentTemplate.Layer;
                        Selection selection = basicFeatureLayer.GetSelection();
                        IReadOnlyList<long> objectIds = selection.GetObjectIDs();
                        selectionCount = objectIds.Count;
                    });
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return selectionCount;
        }

        /// <summary>
        /// Set current layer feature selection.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task SetSelection()
        {
            try
            {
                if (ArrowContext.SelectNewArrows)
                {
                    await QueuedTask.Run(() =>
                    {
                        BasicFeatureLayer basicFeatureLayer = (BasicFeatureLayer)ArrowContext.CurrentTemplate.Layer;
                        basicFeatureLayer.ClearSelection();

                        Selection selection = basicFeatureLayer.GetSelection();
                        selection.Add(ArrowContext.SessionArrowObjectIds);

                        basicFeatureLayer.SetSelection(selection);
                    });
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Clear feature selection for current layer.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ClearArrowSelection()
        {
            try
            {
                if (!ArrowContext.SelectNewArrows)
                {
                    await QueuedTask.Run(() =>
                    {
                        BasicFeatureLayer basicFeatureLayer = (BasicFeatureLayer)ArrowContext.CurrentTemplate.Layer;
                        basicFeatureLayer.ClearSelection();
                    });
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Selection

        #region Cursor

        /// <summary>
        /// Set cursor according to current arrow style.
        /// </summary>
        private void SetCursor()
        {
            try
            {
                if (ArrowContext.CurrentArrowType == null)
                {
                    return;
                }

                string cursorFileName = GetCursorFileName();
                string cursorFolderPath = ArrowContext.AssemblyFolderPath + "\\Cursors";
                string cursorPath = System.IO.Path.Combine(cursorFolderPath, cursorFileName);
                FrameworkApplication.ActiveTool.Cursor = new System.Windows.Input.Cursor(cursorPath);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Get cursor file name.
        /// </summary>
        /// <returns>Cursor file name.</returns>
        public string GetCursorFileName()
        {
            string cursorFileName = "";

            try
            {
                switch (ArrowContext.CurrentArrowType.Category)
                {
                    case ArrowContext.ArrowCategory.Straight:
                        cursorFileName = "StraightArrows.cur";
                        break;

                    case ArrowContext.ArrowCategory.LandHook:
                        cursorFileName = "LandHook.cur";
                        break;

                    case ArrowContext.ArrowCategory.NoDashes:
                        cursorFileName = "Curved0.cur";
                        break;

                    case ArrowContext.ArrowCategory.OneDash:
                        cursorFileName = "Curved1.cur";
                        break;

                    case ArrowContext.ArrowCategory.TwoDashes:
                        cursorFileName = "Curved2.cur";
                        break;

                    case ArrowContext.ArrowCategory.ThreeDashes:
                        cursorFileName = "Curved3.cur";
                        break;

                    case ArrowContext.ArrowCategory.FourDashes:
                        cursorFileName = "Curved4.cur";
                        break;

                    case ArrowContext.ArrowCategory.SingleArrow:
                        switch (ArrowContext.CurrentArrowType.Style)
                        {
                            case ArrowContext.ArrowStyle.Straight:
                                cursorFileName = "SingleStraight.cur";
                                break;

                            case ArrowContext.ArrowStyle.Leader:
                                cursorFileName = "SingleLeader.cur";
                                break;

                            case ArrowContext.ArrowStyle.Zigzag:
                                cursorFileName = "SingleZigzag.cur";
                                break;
                        }
                        break;

                    case ArrowContext.ArrowCategory.RoadTic:
                        cursorFileName = "RoadTic.cur";
                        break;
                }

                if (cursorFileName == "")
                {
                    string errorMessage = "The cursor file name could not be set.";
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return cursorFileName;
        }

        #endregion Cursor

        #region Menu and Key Commands

        /// <summary>
        /// Add scale items to context menu.
        /// </summary>
        /// <param name="checkMark">Checkmark string.</param>
        public void AddScaleItemsToMenu(string checkMark)
        {
            try
            {
                bool addSeparator = false;

                foreach (KeyValuePair<string, ArrowContext.ScaleValues> scaleLabelValuesPair in ArrowContext.ScaleLabelValuesPairs)
                {
                    ArrowContext.ScaleValues scaleValues = scaleLabelValuesPair.Value;

                    if (scaleValues.MenuScaleValue == "0")
                    {
                        continue;
                    }

                    if (ArrowContext.RequiredMenuScaleValues != null && !ArrowContext.RequiredMenuScaleValues.Contains(scaleValues.MenuScaleValue.ToString()))
                    {
                        continue;
                    }

                    string scaleCheckMark = ArrowContext.MapPoints.Count > 0 && scaleValues.ArrowScaleValue == ArrowContext.ArrowScale ? checkMark : "";

                    ArrowContext.CurrentMenuItems.Add(scaleLabelValuesPair.Key + scaleCheckMark);
                    addSeparator = true;
                }

                if (addSeparator)
                {
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SEPARATOR);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Apply action for clicked menu item.
        /// </summary>
        /// <param name="menuCaption">Menu caption.</param>
        /// <returns>Task.</returns>
        public async Task ApplyMenuAction(string menuCaption)
        {
            try
            {
                bool modifyFeature = false;

                switch (menuCaption)
                {
                    case ArrowContext.MenuItems.SHORTER:
                        ArrowContext.ArrowScale *= 0.9;
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.LONGER:
                        ArrowContext.ArrowScale *= 1.2;
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.CANCEL:
                        await ResetCurrentFeatureCreation();
                        break;

                    case ArrowContext.MenuItems.SCALE_0:

                        SetArrowScale(ArrowContext.MenuItems.SCALE_0);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_10:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_10);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_20:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_20);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_30:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_30);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_40:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_40);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_50:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_50);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_100:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_100);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_200:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_200);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_400:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_400);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_800:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_800);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_1000:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_1000);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.SCALE_2000:
                        SetArrowScale(ArrowContext.MenuItems.SCALE_2000);
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.NARROWER:
                        ArrowContext.ZigzagWidth--;
                        if (ArrowContext.ZigzagWidth < 1)
                        {
                            ArrowContext.ZigzagWidth = 1;
                        }
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.WIDER:
                        ArrowContext.ZigzagWidth++;
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.TO_START:
                        ArrowContext.ZigzagPosition -= 2.5;
                        if (ArrowContext.ZigzagPosition < 1)
                        {
                            ArrowContext.ZigzagPosition = 1;
                        }
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.TO_END:
                        ArrowContext.ZigzagPosition += 2.5;
                        if (ArrowContext.ZigzagPosition > 19)
                        {
                            ArrowContext.ZigzagPosition = 19;
                        }
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.CURVE_LESS:
                        ArrowContext.ZigzagCurve--;
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.CURVE_MORE:
                        ArrowContext.ZigzagCurve++;
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.ZIGZAG_DEFAULTS:
                        ArrowContext.ZigzagWidth = ArrowContext.ZigzagWidthDefault;
                        ArrowContext.ZigzagCurve = ArrowContext.ZigzagCurveDefault;
                        ArrowContext.ZigzagPosition = ArrowContext.ZigzagPositionDefault;
                        modifyFeature = true;
                        break;

                    case ArrowContext.MenuItems.STYLE_STRAIGHT:
                        ArrowContext.ArrowStyle arrowStyle = ArrowContext.ArrowStyle.Straight;
                        await ChangeArrowStyle(arrowStyle);
                        break;

                    case ArrowContext.MenuItems.STYLE_LEADER:
                        arrowStyle = ArrowContext.ArrowStyle.Leader;
                        await ChangeArrowStyle(arrowStyle);
                        break;

                    case ArrowContext.MenuItems.STYLE_ZIGZAG:
                        arrowStyle = ArrowContext.ArrowStyle.Zigzag;
                        await ChangeArrowStyle(arrowStyle);
                        break;

                    case ArrowContext.MenuItems.STYLE_ROADTIC:
                        arrowStyle = ArrowContext.ArrowStyle.RoadTic;
                        await ChangeArrowStyle(arrowStyle);
                        break;

                    default:
                        break;
                }

                if (menuCaption.StartsWith(ArrowContext.MenuItems.UPDATE_END_POINT))
                {
                    ArrowContext.UpdateEndPoint = !ArrowContext.UpdateEndPoint;
                    ArrowContext.UpdateOffset = !ArrowContext.UpdateEndPoint;
                }
                else if (menuCaption.StartsWith(ArrowContext.MenuItems.UPDATE_OFFSET))
                {
                    ArrowContext.UpdateOffset = !ArrowContext.UpdateOffset;
                    ArrowContext.UpdateEndPoint = !ArrowContext.UpdateOffset;
                }
                else if (menuCaption.StartsWith(ArrowContext.MenuItems.FLIP_SINGLE) || menuCaption.StartsWith(ArrowContext.MenuItems.FLIP_MULTIPLE))
                {
                    ArrowContext.FlipArrows = !ArrowContext.FlipArrows;
                    modifyFeature = true;
                }
                else if (menuCaption.StartsWith(ArrowContext.MenuItems.SELECTION_SINGLE) || menuCaption.StartsWith(ArrowContext.MenuItems.SELECTION_MULTIPLE))
                {
                    ArrowContext.SelectNewArrows = !ArrowContext.SelectNewArrows;
                }
                else if (menuCaption.StartsWith(ArrowContext.MenuItems.SWITCH_SINGLE) || menuCaption.StartsWith(ArrowContext.MenuItems.SWITCH_MULTIPLE))
                {
                    ArrowContext.SwitchArrowheads = !ArrowContext.SwitchArrowheads;
                    modifyFeature = true;
                }
                else if (menuCaption.StartsWith(ArrowContext.MenuItems.FINISH_SINGLE) || menuCaption.StartsWith(ArrowContext.MenuItems.FINISH_MULTIPLE))
                {
                    FinishArrows();
                }

                if (modifyFeature && ArrowContext.MapPoints.Count > 1)
                {
                    EditOpType editOpType = EditOpType.Modify;
                    await CreateUpdateArrowFeatures(editOpType);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Respond to key commands.
        /// </summary>
        /// <param name="key">Key pressed.</param>
        /// <param name="shift">Value indicating whether shift was pressed.</param>
        /// <returns>Task.</returns>
        public async Task KeyCommands(System.Windows.Input.Key key, bool shift)
        {
            try
            {
                bool categoryDimensionArrows = IsCurrentArrowCategoryDimensionArrows();

                bool allowShorterLonger = categoryDimensionArrows ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.Straight ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.LandHook;

                bool allowFlip = ArrowContext.CurrentArrowType.Style == ArrowContext.ArrowStyle.Zigzag || ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.Straight ||
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.LandHook;

                if (allowShorterLonger && key == System.Windows.Input.Key.Space && !shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.SHORTER);
                }
                else if (allowShorterLonger && key == System.Windows.Input.Key.Space && shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.LONGER);
                }
                else if (allowFlip && key == System.Windows.Input.Key.F && shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.FLIP_SINGLE);
                }
                else if (ArrowContext.CurrentArrowType.Style == ArrowContext.ArrowStyle.Zigzag && key == System.Windows.Input.Key.L && shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.CURVE_LESS);
                }
                else if (ArrowContext.CurrentArrowType.Style == ArrowContext.ArrowStyle.Zigzag && key == System.Windows.Input.Key.M && shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.CURVE_MORE);
                }
                else if (key == System.Windows.Input.Key.S && shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.SWITCH_SINGLE);
                }
                else if (key == System.Windows.Input.Key.W && shift)
                {
                    await ApplyMenuAction(ArrowContext.MenuItems.FINISH_SINGLE);
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Set ArrowContext.ArrowScale variable.
        /// </summary>
        /// <param name="arrowStyle">ArrowContext.ArrowStyle value.</param>
        /// <returns>Task.</returns>
        private async Task ChangeArrowStyle(ArrowContext.ArrowStyle arrowStyle)
        {
            try
            {
                bool featureFinished = ArrowContext.MapPoints.Count == 0;
                if (!featureFinished)
                {
                    string message = $"The current feature is not finished.{Environment.NewLine}{Environment.NewLine}" +
                        $"Do you wish to continue?{Environment.NewLine}{Environment.NewLine}" +
                        $"Click Yes to clear the current feature and change the arrow type.{Environment.NewLine}{Environment.NewLine}" +
                        "Click No to continue creating the current feature.";
                    string caption = "Unfinished Feature";
                    MessageBoxResult response = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, caption, MessageBoxButton.YesNo);
                    if (response == MessageBoxResult.Yes)
                    {
                        await ResetCurrentFeatureCreation();
                    }
                    else
                    {
                        return;
                    }
                }

                ArrowContext.CurrentArrowType = new ArrowContext.ArrowType
                {
                    Category = ArrowContext.ArrowCategory.SingleArrow,
                    Style = arrowStyle
                };

                await ResetCurrentFeatureCreation();

                ArrowContext.UpdateEndPoint = false;

                SetCursor();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Menu and Key Commands

        #region Required Scales

        /// <summary>
        /// Store required scales file.
        /// </summary>
        /// <returns>Value indicating whether operation was successful.</returns>
        public bool StoreRequiredScalesFile()
        {
            bool success = false;

            try
            {
                string requiredScalesFilePath = GetRequiredScalesFilePath();

                if (requiredScalesFilePath == "")
                {
                    success = false;
                    return success;
                }

                bool validFile = IsRequiredScalesFileValid(requiredScalesFilePath);

                if (validFile)
                {
                    DeleteStoredScalesFile();

                    string storedScalesFilePath = GetStoredScalesFilePath();
                    File.Copy(requiredScalesFilePath, storedScalesFilePath);
                    success = true;
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return success;
        }

        /// <summary>
        /// Check whether input required scales file is valid.
        /// </summary>
        /// <param name="requiredScalesFilePath">Input required scales file path.</param>
        /// <returns>Value indicating whether required scales file is valid.</returns>
        private bool IsRequiredScalesFileValid(string requiredScalesFilePath)
        {
            bool fileValid = false;

            try
            {
                // Check for integers only.
                bool validFile = true;

                using (StreamReader streamReader = new StreamReader(requiredScalesFilePath))
                {
                    string line;
                    int count = 0;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string scale = line.Trim();
                        bool isInt = int.TryParse(scale, out int scaleInt);

                        if (!isInt)
                        {
                            validFile = false;
                            break;
                        }

                        count++;
                    }
                }

                if (!validFile)
                {
                    string message = $"The required scales file '{requiredScalesFilePath}' is invalid." +
                        Environment.NewLine +
                        Environment.NewLine +
                        "Please ensure that the file contains integer values with one value per line.";
                    _ = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message);
                }
                else
                {
                    fileValid = true;
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return fileValid;
        }

        /// <summary>
        /// Get stored required scales file path.
        /// </summary>
        /// <returns>Required scales file path.</returns>
        private string GetStoredScalesFilePath()
        {
            string storedScalesFilePath = "";

            try
            {
                string projectPath = Project.Current.Path;
                string projectFolderPath = Path.GetDirectoryName(projectPath);
                string[] paths = { projectFolderPath, "ArrowTools_Required_Scales.txt" };
                storedScalesFilePath = Path.Combine(paths);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return storedScalesFilePath;
        }

        /// <summary>
        /// Get scales from stored required scales file.
        /// </summary>
        public void GetRequiredScales()
        {
            try
            {
                string storedScalesFilePath = GetStoredScalesFilePath();

                if (!File.Exists(storedScalesFilePath))
                {
                    ArrowContext.RequiredMenuScaleValues = null;
                    return;
                }

                bool validFile = IsRequiredScalesFileValid(storedScalesFilePath);
                if (!validFile)
                {
                    return;
                }

                ArrowContext.RequiredMenuScaleValues = new List<string>();
                using (StreamReader streamReader = new StreamReader(storedScalesFilePath))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        string scaleString = line;
                        ArrowContext.RequiredMenuScaleValues.Add(scaleString);
                    }
                }

                ArrowContext.ArrowScale = default;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Get input required scales file path.
        /// </summary>
        /// <returns>Input required scales file path.</returns>
        private string GetRequiredScalesFilePath()
        {
            string requiredScalesFilePath = "";

            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Required Scales Text File",
                    InitialDirectory = @"C:\",
                    DefaultExt = ".txt",
                    Filter = "Text documents (.txt)|*.txt"
                };

                bool? result = openFileDialog.ShowDialog();

                if (result == true)
                {
                    requiredScalesFilePath = openFileDialog.FileName;
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return requiredScalesFilePath;
        }

        /// <summary>
        /// Delete stored required scales file.
        /// </summary>
        public void DeleteStoredScalesFile()
        {
            try
            {
                string storedScalesFilePath = GetStoredScalesFilePath();

                File.Delete(storedScalesFilePath);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Required Scales

        #region Project

        /// <summary>
        /// Add property changed event handler for Project.Current class.
        /// </summary>
        public void AddProjectCurrentPropertyChangedHandler()
        {
            try
            {
                Project.Current.PropertyChanged -= ProjectCurrent_PropertyChanged;
                Project.Current.PropertyChanged += ProjectCurrent_PropertyChanged;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Property changed event handler for Project.Current class.
        /// </summary>
        private void ProjectCurrent_PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            try
            {
                if (propertyChangedEventArgs.PropertyName == "IsEditingEnabled")
                {
                    Project project = sender as Project;

                    if (!project.IsEditingEnabled)
                    {
                        FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
                    }
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        #endregion Project
    }
}
