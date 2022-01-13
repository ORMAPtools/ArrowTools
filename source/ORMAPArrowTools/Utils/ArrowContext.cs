using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ORMAPArrowTools.Utils
{
    /// <summary>
    /// ArrowContext class.
    /// </summary>
    /// <remarks>Context class to support arrow feature(s) creation.</remarks>
    public static class ArrowContext
    {
        #region Arrow Descriptors

        public static ArrowType CurrentArrowType;

        public enum ArrowCategory
        {
            Straight,
            LandHook,
            NoDashes,
            OneDash,
            TwoDashes,
            ThreeDashes,
            FourDashes,
            SingleArrow,
            RoadTic,
            NotSet
        }

        public enum ArrowStyle
        {
            Straight,
            Leader,
            Zigzag,
            RoadTic,
            NotSet
        }

        public class ArrowType
        {
            public ArrowType()
            {
                Category = ArrowCategory.NotSet;
                Style = ArrowStyle.NotSet;
            }

            public ArrowCategory Category { get; set; }
            public ArrowStyle Style { get; set; }
            public override string ToString()
            {
                return $"({Category}, {Style})";
            }
        }

        #endregion Arrow Descriptors

        #region Arrow Creation - General

        public static ObservableCollection<MapPoint> MapPoints = null;

        public static double ArrowScale = default;

        public static double ArrowScaleDefault = 1;

        public static double ArrowOffset = 0;

        public static bool FlipArrows = false;

        public static bool SwitchArrowheads = false;

        public static bool SelectNewArrows = true;

        public static SpatialReference SpatialReference = null;

        public static double SearchShapeBufferDistance = 5;

        #endregion Arrow Creation - General

        #region Single Arrow - Zigzag

        public static double ZigzagWidthDefault = 5;

        public static double ZigzagWidth = ZigzagWidthDefault;

        public static double ZigzagCurveDefault = 5;

        public static double ZigzagCurve = ZigzagCurveDefault;

        public static double ZigzagPositionDefault = 10;

        public static double ZigzagPosition = ZigzagPositionDefault;

        #endregion Single Arrow - Zigzag

        #region Editing

        public static EditingTemplate CurrentTemplate = null;

        #endregion Editing

        #region ObjectIds

        public static long CurrentPointObjectId = ObjectIdDefault;

        public const long ObjectIdDefault = -1;

        public static Dictionary<int, long> CurrentArrowObjectIds = null;

        public static List<long> SessionArrowObjectIds = null;

        #endregion ObjectIds

        #region Map

        public static double MapReferenceScale = 0;

        #endregion Map

        #region Files

        public static string AssemblyFolderPath = "";

        public const string XMLFileName = "ArrowSettings.xml";

        public static string XMLFilePath = null;

        #endregion Files

        #region Layers and Fields

        public const string TempPointsLayerName = "TempPoints";

        public const string MapIndexLayerName = "MapIndex";

        public const string MapScaleFieldName = "MapScale";

        #endregion Layers and Fields

        #region Menu

        public static List<string> CurrentMenuItems = null;

        public static bool UpdateEndPoint = false;

        public static bool UpdateOffset = false;

        public static Dictionary<string, ScaleValues> ScaleLabelValuesPairs = new Dictionary<string, ScaleValues>
        {
            { MenuItems.SCALE_0, new ScaleValues("0", 0)},
            { MenuItems.SCALE_10, new ScaleValues("10", 0.1)},
            { MenuItems.SCALE_20, new ScaleValues("20", 0.2)},
            { MenuItems.SCALE_30, new ScaleValues("30", 0.3)},
            { MenuItems.SCALE_40, new ScaleValues("40", 0.4)},
            { MenuItems.SCALE_50, new ScaleValues("50", 0.5)},
            { MenuItems.SCALE_100, new ScaleValues("100", 1)},
            { MenuItems.SCALE_200, new ScaleValues("200", 2)},
            { MenuItems.SCALE_400, new ScaleValues("400", 4)},
            { MenuItems.SCALE_800, new ScaleValues("800", 8)},
            { MenuItems.SCALE_1000, new ScaleValues("1000", 10)},
            { MenuItems.SCALE_2000, new ScaleValues("2000", 20)}
        };

        public class ScaleValues
        {
            public string MenuScaleValue { get; set; }
            public double ArrowScaleValue { get; set; }

            public ScaleValues(string menuScaleValue, double geometryScaleValue)
            {
                MenuScaleValue = menuScaleValue;
                ArrowScaleValue = geometryScaleValue;
            }
        }

        public static List<string> RequiredMenuScaleValues = null;

        public class MenuItems
        {
            public const string SEPARATOR = "-";
            public const string SHORTER = "Shorter - Space";
            public const string LONGER = "Longer - Shift-Space";
            public const string FLIP_SINGLE = "Flip arrow - Shift-F";
            public const string FLIP_MULTIPLE = "Flip arrows - Shift-F";
            public const string SWITCH_SINGLE = "Switch arrowhead - Shift-S";
            public const string SWITCH_MULTIPLE = "Switch arrowheads - Shift-S";
            public const string CANCEL = "Cancel";
            public const string SCALE_0 = "Scale not set";
            public const string SCALE_10 = "10 scale";
            public const string SCALE_20 = "20 scale";
            public const string SCALE_30 = "30 scale";
            public const string SCALE_40 = "40 scale";
            public const string SCALE_50 = "50 scale";
            public const string SCALE_100 = "100 scale";
            public const string SCALE_200 = "200 scale";
            public const string SCALE_400 = "400 scale";
            public const string SCALE_800 = "800 scale";
            public const string SCALE_1000 = "1000 scale";
            public const string SCALE_2000 = "2000 scale";
            public const string ZIGZAG_DEFAULTS = "Use default settings";
            public const string NARROWER = "Narrower";
            public const string WIDER = "Wider";
            public const string TO_START = "Slide toward start";
            public const string TO_END = "Slide toward end";
            public const string CURVE_LESS = "Less curve - Shift-L";
            public const string CURVE_MORE = "More curve - Shift-M";
            public const string STYLE_STRAIGHT = "Straight arrow style";
            public const string STYLE_LEADER = "Leader arrow style";
            public const string STYLE_ZIGZAG = "Zigzag arrow style";
            public const string STYLE_ROADTIC = "Road Tic";
            public const string FINISH_SINGLE = "Finish arrow - Shift-W";
            public const string FINISH_MULTIPLE = "Finish arrows - Shift-W";
            public const string SELECTION_SINGLE = "Select new arrow";
            public const string SELECTION_MULTIPLE = "Select new arrows";
            public const string UPDATE_END_POINT = "Update end point";
            public const string UPDATE_OFFSET = "Update offset";
        }

        #endregion Menu

        #region Diagnostics

        // Dev diagnostics options.
        public static bool DevShowDiagnosticPoints = false;

        public static bool DevShowDiagnosticLines = false;

        public static bool DevShowMenuPointCount = false;

        #endregion Diagnostics
    }
}
