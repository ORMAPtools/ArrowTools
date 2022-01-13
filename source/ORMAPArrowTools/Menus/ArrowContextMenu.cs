using ArcGIS.Desktop.Framework.Contracts;
using ORMAPArrowTools.Utils;
using System;
using System.Collections.Generic;

namespace ORMAPArrowTools.Menus
{
    /// <summary>
    /// ArrowContextMenu class.
    /// </summary>
    /// <remarks>Context menu for arrow(s) construction tools.</remarks>
    class ArrowContextMenu : DynamicMenu
    {
        private readonly MessageHandler _messageHandler = new MessageHandler();
        private readonly ArrowUtils _arrowUtils = null;
        private Dictionary<int, string> captions = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ArrowContextMenu()
        {
            _arrowUtils = new ArrowUtils(_messageHandler);
        }


        /// <summary>
        /// OnPopup event handler.
        /// </summary>
        protected override void OnPopup()
        {
            try
            {
                int itemIndex = 0;
                captions = new Dictionary<int, string>();

                foreach (string menuOption in ArrowContext.CurrentMenuItems)
                {
                    if (menuOption == ArrowContext.MenuItems.SEPARATOR)
                    {
                        AddSeparator();
                    }
                    else
                    {
                        this.Add(menuOption);
                        captions.Add(itemIndex, menuOption);
                    }

                    itemIndex += 1;
                }
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }

        /// <summary>
        /// OnClick event handler.
        /// </summary>
        /// <param name="index">Menu item index.</param>
        protected override async void OnClick(int index)
        {
            try
            {
                string menuCaption = captions[index];

                await _arrowUtils.ApplyMenuAction(menuCaption);
            }
            catch (Exception exception)
            {
                Exception catchException = new Exception("", exception);
                _messageHandler.ProcessErrorDetails(catchException);
            }
        }
    }
}