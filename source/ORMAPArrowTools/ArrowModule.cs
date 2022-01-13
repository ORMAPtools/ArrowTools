using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace ORMAPArrowTools
{
    internal class ArrowModule : Module
    {
        private static ArrowModule _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here.
        /// </summary>
        public static ArrowModule Current
        {
            get
            {
                return _this ?? (_this = (ArrowModule)FrameworkApplication.FindModule("ORMAPArrowTools_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing.
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True.</returns>
        protected override bool CanUnload()
        {
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides
    }
}
