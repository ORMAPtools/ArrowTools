using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ORMAPArrowTools.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace ORMAPArrowTools.Tools
{
    /// <summary>
    /// ToolOptionsViewModel class.
    /// </summary>
    /// <remarks>View model class for tool options panel.</remarks>
    internal class ToolOptionsViewModel : ToolOptionsEmbeddableControl, IEditingCreateToolMultiple
    {
        private readonly MessageHandler _messageHandler = null;
        private readonly object _lock = new object();
        private readonly ArrowUtils _arrowUtils = null;

        public ObservableCollection<string> ReferenceScaleStrings { get; } = new ObservableCollection<string>();

        private string _referenceScalesSelectedItem;
        public string ReferenceScalesSelectedItem
        {
            get { return _referenceScalesSelectedItem; }
            set
            {
                if (MapView.Active != null && SetProperty(ref _referenceScalesSelectedItem, value))
                {
                    double referenceScale = value == "None" ? 0 : Convert.ToDouble(value);
                    ArrowContext.MapReferenceScale = referenceScale;

                    _ = QueuedTask.Run(() =>
                      {
                          MapView.Active.Map.SetReferenceScale(referenceScale);
                      });
                }
            }
        }

        public ICommand SetRequiredScalesCommand { get; }
        public ICommand ClearRequiredScalesCommand { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">XElement options object.</param>
        /// <param name="canChangeOptions">Value indicating whether options can be changed.</param>
        public ToolOptionsViewModel(XElement options, bool canChangeOptions) : base(options, canChangeOptions)
        {
            _messageHandler = new MessageHandler();

            try
            {
                _arrowUtils = new ArrowUtils(_messageHandler);

                lock (_lock)
                {
                    foreach (KeyValuePair<string, ArrowContext.ScaleValues> scaleLabelValuesPair in ArrowContext.ScaleLabelValuesPairs)
                    {
                        ArrowContext.ScaleValues scaleValues = scaleLabelValuesPair.Value;

                        string referenceScaleString = scaleValues.MenuScaleValue == "0" ? "None" : scaleValues.MenuScaleValue;
                        ReferenceScaleStrings.Add(referenceScaleString);
                    }
                }

                SetRequiredScalesCommand = new RelayCommand(param => SetRequiredScales(), () => true);
                ClearRequiredScalesCommand = new RelayCommand(param => ClearRequiredScales(), () => true);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Set required scales according to input text file.
        /// </summary>
        private void SetRequiredScales()
        {
            try
            {
                bool success = _arrowUtils.StoreRequiredScalesFile();

                if (!success)
                {
                    return;
                }

                _arrowUtils.GetRequiredScales();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Clear required scales.
        /// </summary>
        private void ClearRequiredScales()
        {
            try
            {
                _arrowUtils.DeleteStoredScalesFile();

                _arrowUtils.GetRequiredScales();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// Show control.
        /// </summary>
        protected override bool ShowThisControl
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Set reference scales selected item according to currently selected option.
        /// </summary>
        protected override Task LoadFromToolOptions()
        {
            try
            {
                ReferenceScalesSelectedItem = ArrowContext.MapReferenceScale == 0 ? "None" : ArrowContext.MapReferenceScale.ToString();
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }

            return Task.CompletedTask;
        }
    }
}