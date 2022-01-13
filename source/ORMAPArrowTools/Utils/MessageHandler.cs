using System;
using System.Reflection;

namespace ORMAPArrowTools.Utils
{
    /// <summary>
    /// MessageHandler class.
    /// </summary>
    /// <remarks>Utility class for creating messages.</remarks>
    public class MessageHandler
    {
        public delegate void _createFinalMessageDelegate();
        private readonly _createFinalMessageDelegate _createFinalMessage;
        private bool _exitEnvironment;

        private bool _programHalted = false;

        public bool ProgramHalted
        {
            get => _programHalted;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="createFinalMessage">_createFinalMessageDelegate delegate for function to create final message.</param>
        public MessageHandler(_createFinalMessageDelegate createFinalMessage = null)
        {
            _createFinalMessage = createFinalMessage;
            _exitEnvironment = false;
        }

        /// <summary>
        /// Process error details.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        public void ProcessErrorDetails(Exception exception)
        {
            string message = CreateErrorMessage(exception);

            bool breaking = true;

            WriteMessage(message, breaking);
        }

        /// <summary>
        /// Create error message.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        /// <returns>Error message.</returns>
        private string CreateErrorMessage(Exception exception)
        {
            string errorMessage = null;

            try
            {
                if (exception == null)
                {
                    return errorMessage;
                }

                ErrorDetails errorDetails = CreateErrorDetails(exception);

                errorMessage = "An error occurred:\n";

                if (errorDetails.ExceptionType != null)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"Exception type: {errorDetails.ExceptionType}";
                }

                if (errorDetails.ClassPath != null)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"\nClass: {errorDetails.ClassPath}";
                }

                if (errorDetails.LineNumber != default)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"\nLine number: {errorDetails.LineNumber}";
                }

                if (errorDetails.MethodName != null)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"\nMethod: {errorDetails.MethodName}";
                }

                if (errorDetails.ExceptionMessage != null)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"\nException message: {errorDetails.ExceptionMessage}";
                }

                if (errorDetails.CustomLocation != null)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"\nLocation: {errorDetails.CustomLocation}";
                }

                if (errorDetails.CustomMessage != null)
                {
                    errorMessage = errorMessage != "" ? errorMessage += "\n" : errorMessage;
                    errorMessage += $"\nMessage: {errorDetails.CustomMessage}";
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            return errorMessage;
        }

        /// <summary>
        /// Create error details object.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        /// <returns>ErrorDetails object.</returns>
        private static ErrorDetails CreateErrorDetails(Exception exception)
        {
            ErrorDetails errorDetails = null;

            try
            {
                errorDetails = new ErrorDetails();

                Exception detailsException = null;
                Exception innerException = exception.InnerException;
                if (innerException != null)
                {
                    // Exception was rethrown with an added message.
                    detailsException = innerException;

                    if (innerException.Message.Contains("|"))
                    {
                        // Format: <location>|<message>
                        string[] parts = innerException.Message.Split('|');
                        errorDetails.CustomLocation = parts[0];
                        errorDetails.CustomMessage = parts[1];
                    }
                    else
                    {
                        errorDetails.ExceptionMessage = innerException.Message;
                    }
                }
                else
                {
                    detailsException = exception;
                }

                errorDetails.ExceptionType = detailsException.GetType().ToString();

                string strackTraceTemp = detailsException.StackTrace.Replace("\r\n", "|");
                string[] stackTraceParts = strackTraceTemp.Split('|');
                string stackTrace0 = stackTraceParts[0].Replace("   at ", "");
                int openParenIndex = stackTrace0.IndexOf("(");
                string methodPath = stackTrace0.Substring(0, openParenIndex);
                string[] pathParts = methodPath.Split('.');
                string methodName = pathParts[pathParts.Length - 1];
                errorDetails.MethodName = methodName;
                errorDetails.ClassPath = methodPath.Replace("." + methodName, "");

                int lineIndex = stackTrace0.IndexOf("line");
                if (lineIndex != -1)
                {
                    string lineString = stackTrace0.Substring(lineIndex);
                    string[] lineParts = lineString.Split(' ');
                    errorDetails.LineNumber = Convert.ToInt32(lineParts[1]);
                }
            }
            catch (Exception ex)
            {
                errorDetails.CustomLocation = MethodBase.GetCurrentMethod().Name;
                errorDetails.CustomMessage = ex.Message;
            }

            return errorDetails;
        }

        /// <summary>
        /// ErrorDetails class.
        /// </summary>
        /// <remarks>Store error details.</remarks>
        private class ErrorDetails
        {
            public ErrorDetails()
            {
                ExceptionType = null;
                ClassPath = null;
                MethodName = null;
                LineNumber = default;
                ExceptionMessage = null;
                CustomLocation = null;
                CustomMessage = null;
            }

            public string ExceptionType { get; set; }
            public string ClassPath { get; set; }
            public int LineNumber { get; set; }
            public string MethodName { get; set; }
            public string ExceptionMessage { get; set; }
            public string CustomLocation { get; set; }
            public string CustomMessage { get; set; }
        }

        /// <summary>
        /// Create message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="reportingLocation">Reporting location.</param>
        /// <param name="breaking">Value indicating whether message is of type breaking.</param>
        public void CreateMessage(string message, string reportingLocation=null, bool breaking=false)
        {
            if (reportingLocation != null)
            {
                message = $"Reporting Location: {reportingLocation}\n{message}";
            }

            message = $"\n{message}";

            WriteMessage(message, breaking);
        }

        /// <summary>
        /// Write message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="breaking">Value indicating whether message is of type breaking.</param>
        private void WriteMessage(string message, bool breaking = false)
        {
            bool multiLineMessage = message.Contains("\n");
            if (multiLineMessage)
            {
                message = $"\n{message}";
            }

            _ = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message);

            if (breaking)
            {
                if (_createFinalMessage != null && !_exitEnvironment)
                {
                    _exitEnvironment = true;
                    _programHalted = true;
                    _createFinalMessage();
                }
            }

            if (_exitEnvironment)
            {
                int exitCode = 1;
                Environment.Exit(exitCode);
            }
        }
    }
}
