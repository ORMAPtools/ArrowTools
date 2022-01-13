using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ORMAPArrowTools.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ORMAPArrowTools.Tools
{
    /// <summary>
    /// LandHookConstructionTool class.
    /// </summary>
    /// <remarks>Land hook construction tool class.</remarks>
    internal class LandHookConstructionTool : MapTool
    {
        private readonly MessageHandler _messageHandler = null;
        private readonly ArrowUtils _arrowUtils = null;
        private System.Windows.Point _mouseDownPoint;
        private readonly object _lock = new object();
        private bool _menuShowing = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LandHookConstructionTool()
        {
            _messageHandler = new MessageHandler();

            try
            {
                IsSketchTool = true;
                UseSnapping = true;
                SketchType = SketchGeometryType.Point;
                UsesCurrentTemplate = true;
                FireSketchEvents = true;

                _arrowUtils = new ArrowUtils(_messageHandler);

                ArrowContext.MapPoints = new System.Collections.ObjectModel.ObservableCollection<MapPoint>();

                _arrowUtils.AddProjectCurrentPropertyChangedHandler();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnToolActivateAsync event handler.
        /// </summary>
        /// <param name="hasMapViewChanged">A value indicating whether the active MapView has changed.</param>
        protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
        {
            try
            {
                bool layerPresent = _arrowUtils.CheckLayerPresent(ArrowContext.MapIndexLayerName);
                if (!layerPresent)
                {
                    return;
                }

                if (ArrowContext.DevShowDiagnosticPoints)
                {
                    layerPresent = _arrowUtils.CheckLayerPresent(ArrowContext.TempPointsLayerName);
                    if (!layerPresent)
                    {
                        return;
                    }
                }

                await _arrowUtils.ResetCurrentFeatureCreation();

                await _arrowUtils.SetGeneralProperties(CurrentTemplate.Layer);

                ArrowContext.CurrentArrowType = new ArrowContext.ArrowType
                {
                    Category = ArrowContext.ArrowCategory.LandHook,
                    Style = ArrowContext.ArrowStyle.NotSet
                };

                ArrowContext.CurrentTemplate = CurrentTemplate;

                SetCursor();

                _arrowUtils.GetRequiredScales();

                ArrowContext.MapPoints.CollectionChanged += new NotifyCollectionChangedEventHandler(CollectionChangedMethod);

                MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnToolDeactivateAsync event handler.
        /// </summary>
        /// <param name="hasMapViewChanged">A value indicating whether the active MapView has changed.</param>
        protected override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            try
            {
                await _arrowUtils.ResetCurrentFeatureCreation();

                ArrowContext.MapPoints.CollectionChanged -= new NotifyCollectionChangedEventHandler(CollectionChangedMethod);

                MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnToolKeyDown event handler.
        /// </summary>
        /// <param name="mapViewKeyEventArgs">MapViewKeyEventArgs object.</param>
        protected override async void OnToolKeyDown(MapViewKeyEventArgs mapViewKeyEventArgs)
        {
            try
            {
                Key key = mapViewKeyEventArgs.Key;
                bool shiftKeyPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                await _arrowUtils.KeyCommands(key, shiftKeyPressed);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnToolMouseDown event handler.
        /// </summary>
        /// <param name="mapViewMouseButtonEventArgs">MapViewMouseButtonEventArgs object.</param>
        protected override async void OnToolMouseDown(MapViewMouseButtonEventArgs mapViewMouseButtonEventArgs)
        {
            try
            {
                if (mapViewMouseButtonEventArgs.ChangedButton == MouseButton.Right)
                {
                    MapPoint clickedPoint = await QueuedTask.Run(() =>
                    {
                        _mouseDownPoint = mapViewMouseButtonEventArgs.ClientPoint;

                        return MapView.Active.ClientToMap(_mouseDownPoint);
                    });

                    ShowContextMenu();
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnSketchCompleteAsync event handler.
        /// </summary>
        /// <param name="geometry">Geometry object.</param>
        /// <returns>Task<bool> - value indicating whether OnSketchCompleteAsync event was handled.</returns>
        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            try
            {
                MapPoint mapPoint = (MapPoint)geometry;

                if (ArrowContext.UpdateEndPoint)
                {
                    ArrowContext.MapPoints.RemoveAt(1);
                    lock (_lock)
                    {
                        ArrowContext.MapPoints.Add(mapPoint);
                    }
                }

                bool addPoints = !ArrowContext.UpdateEndPoint &&
                    ArrowContext.CurrentArrowType.Category == ArrowContext.ArrowCategory.LandHook &&
                    ArrowContext.MapPoints.Count < 2;

                if (addPoints)
                {
                    lock (_lock)
                    {
                        ArrowContext.MapPoints.Add(mapPoint);
                    }
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return base.OnSketchCompleteAsync(geometry);
        }

        /// <summary>
        /// CollectionChangedMethod event handler for ObservableCollection<MapPoint> ArrowContext.MapPoints.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="notifyCollectionChangedEventArgs">NotifyCollectionChangedEventArgs object.</param>
        private async void CollectionChangedMethod(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            try
            {
                if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
                {
                    if (ArrowContext.MapPoints.Count == 1)
                    {
                        await _arrowUtils.InitializeFeatureCreation();
                    }
                    else if (ArrowContext.MapPoints.Count == 2)
                    {
                        if (ArrowContext.DevShowDiagnosticPoints)
                        {
                            MapPoint mapPoint = ArrowContext.MapPoints[1];
                            await _arrowUtils.CreatePointFeature(mapPoint);
                        }

                        ArrowUtils.EditOpType editOpType = ArrowUtils.EditOpType.NotSet;
                        editOpType = ArrowContext.MapPoints.Count > 2 || ArrowContext.UpdateEndPoint ? ArrowUtils.EditOpType.Modify : ArrowUtils.EditOpType.Create;

                        await _arrowUtils.CreateUpdateArrowFeatures(editOpType);

                        if (ArrowContext.DevShowDiagnosticPoints)
                        {
                            await _arrowUtils.ClearTempPointsLayer();
                        }
                    }
                }

                if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Reset)
                {
                    _arrowUtils.InitializeObjectIdProperties();
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnMapSelectionChanged event handler.
        /// </summary>
        /// <param name="mapSelectionChangedEventArgs">MapSelectionChangedEventArgs object.</param>
        private async void OnMapSelectionChanged(MapSelectionChangedEventArgs mapSelectionChangedEventArgs)
        {
            try
            {
                await _arrowUtils.CheckClearSessionArrowSelection();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Set cursor.
        /// </summary>
        private void SetCursor()
        {
            try
            {
                if (ArrowContext.CurrentArrowType == null)
                {
                    return;
                }

                string cursorFileName = _arrowUtils.GetCursorFileName();
                string cursorFolderPath = ArrowContext.AssemblyFolderPath + "\\Cursors";
                string cursorPath = System.IO.Path.Combine(cursorFolderPath, cursorFileName);
                Cursor = new System.Windows.Input.Cursor(cursorPath);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Show context menu.
        /// </summary>
        private void ShowContextMenu()
        {
            try
            {
                if (_menuShowing)
                {
                    return;
                }

                ArrowContext.CurrentMenuItems = new List<string>();

                string checkMark = $" {(char)0x221A}";
                string updateEndPointCheckMark = ArrowContext.UpdateEndPoint ? checkMark : "";

                if (ArrowContext.MapPoints.Count == 2)
                {
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.UPDATE_END_POINT + updateEndPointCheckMark);
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.FINISH_MULTIPLE);
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SEPARATOR);
                }

                if (ArrowContext.MapPoints.Count == 2)
                {
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SHORTER);
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.LONGER);

                    string flipCheckMark = ArrowContext.FlipArrows ? checkMark : "";
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.FLIP_MULTIPLE + flipCheckMark);

                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SEPARATOR);
                }

                if (ArrowContext.MapPoints.Count >= 1 && ArrowContext.MapPoints.Count <= 2)
                {
                    _arrowUtils.AddScaleItemsToMenu(checkMark);
                }

                string switchCheckMark = ArrowContext.SwitchArrowheads ? checkMark : "";
                ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SWITCH_MULTIPLE + switchCheckMark);

                string selectCheckMark = ArrowContext.SelectNewArrows ? checkMark : "";
                ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SELECTION_MULTIPLE + selectCheckMark);

                bool showCancelItem = ArrowContext.MapPoints.Count > 0;
                if (showCancelItem)
                {
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SEPARATOR);
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.CANCEL);
                }

                if (ArrowContext.DevShowMenuPointCount)
                {
                    ArrowContext.CurrentMenuItems.Add(ArrowContext.MenuItems.SEPARATOR);
                    string devItem = $"DEV - MapPoints count: {ArrowContext.MapPoints.Count}";
                    ArrowContext.CurrentMenuItems.Add(devItem);
                }

                System.Windows.Controls.ContextMenu contextMenu = FrameworkApplication.CreateContextMenu("ORMAPArrowTools_Menus_ArrowContextMenu", () => _mouseDownPoint);
                _menuShowing = true;

                contextMenu.Closed += (returnObject, routedEventArgs) =>
                {
                    _menuShowing = false;
                };

                contextMenu.IsOpen = true;
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }
    }
}