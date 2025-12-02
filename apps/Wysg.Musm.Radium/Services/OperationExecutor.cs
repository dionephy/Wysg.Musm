using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Shared operation executor for both AutomationWindow (UI testing) and ProcedureExecutor (background automation).
    /// Centralizes operation execution logic to avoid duplication and ensure consistency.
    /// 
    /// Architecture (split across multiple files using partial classes):
    /// - OperationExecutor.cs (this file): Main API, initialization, routing
    /// - OperationExecutor.StringOps.cs: String manipulation operations
    /// - OperationExecutor.ElementOps.cs: UI element interaction operations
    /// - OperationExecutor.SystemOps.cs: System-level operations (mouse, clipboard, keyboard)
    /// - OperationExecutor.MainViewModelOps.cs: MainViewModel data access operations
    /// - OperationExecutor.Http.cs: HTTP/web operations and encoding detection
    /// - OperationExecutor.Encoding.cs: Korean/UTF-8/CP949 encoding helpers
    /// - OperationExecutor.Helpers.cs: Header parsing and element reading helpers
    /// </summary>
    internal static partial class OperationExecutor
    {
        private static readonly HttpClient _http = CreateHttp();
        private static bool _encProviderRegistered;

        static OperationExecutor()
        {
            EnsureEncodingProviders();
        }

        private static HttpClient CreateHttp()
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };
            var client = new HttpClient(handler, disposeHandler: true);
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0 Safari/537.36");
            }
            catch { }
            return client;
        }

        private static void EnsureEncodingProviders()
        {
            if (_encProviderRegistered) return;
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch
            {
                try
                {
                    var provType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                    if (provType != null)
                    {
                        var instanceProp = provType.GetProperty("Instance");
                        var instance = instanceProp?.GetValue(null);
                        var register = typeof(Encoding).GetMethod("RegisterProvider", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (instance != null && register != null)
                        {
                            register.Invoke(null, new[] { instance });
                        }
                    }
                }
                catch { }
            }
            _encProviderRegistered = true;
        }

        /// <summary>
        /// Execute a procedure operation synchronously.
        /// Routes to appropriate operation implementation based on operation name.
        /// </summary>
        public static (string preview, string? value) ExecuteOperation(
            string operation,
            Func<AutomationElement?> resolveArg1Element,
            Func<string?> resolveArg1String,
            Func<string?> resolveArg2String,
            Func<string?> resolveArg3String,
            Dictionary<string, AutomationElement>? elementCache = null)
        {
            switch (operation)
            {
                // String Operations
                case "Split":
                    return ExecuteSplit(resolveArg1String(), resolveArg2String(), resolveArg3String());
                case "IsMatch":
                    return ExecuteIsMatch(resolveArg1String(), resolveArg2String());
                case "IsAlmostMatch":
                    return ExecuteIsAlmostMatch(resolveArg1String(), resolveArg2String());
                case "And":
                    return ExecuteAnd(resolveArg1String(), resolveArg2String());
                case "Not":
                    return ExecuteNot(resolveArg1String());
                case "GetLongerText":
                    return ExecuteGetLongerText(resolveArg1String(), resolveArg2String());
                case "TrimString":
                    return ExecuteTrimString(resolveArg1String(), resolveArg2String());
                case "Replace":
                    return ExecuteReplace(resolveArg1String(), resolveArg2String(), resolveArg3String());
                case "Merge":
                    return ExecuteMerge(resolveArg1String(), resolveArg2String(), resolveArg3String());
                case "TakeLast":
                    return ExecuteTakeLast(resolveArg1String());
                case "Trim":
                    return ExecuteTrim(resolveArg1String());
                case "ToDateTime":
                    return ExecuteToDateTime(resolveArg1String());

                // Element Operations
                case "GetText":
                    return ExecuteGetText(resolveArg1Element());
                case "GetTextWait":
                    // Special handling: pass resolution function to allow retry
                    return ExecuteGetTextWaitWithRetry(resolveArg1Element);
                case "GetName":
                    return ExecuteGetName(resolveArg1Element());
                case "GetTextOCR":
                    return ExecuteGetTextOCR(resolveArg1Element());
                case "Invoke":
                    return ExecuteInvoke(resolveArg1Element());
                case "SetFocus":
                    return ExecuteSetFocus(resolveArg1Element());
                case "SetValue":
                    return ExecuteSetValue(resolveArg1Element(), resolveArg2String());
                case "SetValueWeb":
                    return ExecuteSetValueWeb(resolveArg1Element(), resolveArg2String());
                case "ClickElement":
                    return ExecuteClickElement(resolveArg1Element(), restoreCursor: true);
                case "ClickElementAndStay":
                    return ExecuteClickElement(resolveArg1Element(), restoreCursor: false);
                case "MouseMoveToElement":
                    return ExecuteMouseMoveToElement(resolveArg1Element());
                case "IsVisible":
                    return ExecuteIsVisible(resolveArg1Element());
                case "GetValueFromSelection":
                    return ExecuteGetValueFromSelection(resolveArg1Element(), resolveArg2String() ?? "ID");
                case "GetSelectedElement":
                    return ExecuteGetSelectedElement(resolveArg1Element(), elementCache);

                // System Operations
                case "MouseClick":
                    return ExecuteMouseClick(resolveArg1String(), resolveArg2String());
                case "SetClipboard":
                    return ExecuteSetClipboard(resolveArg1String());
                case "SimulateTab":
                    return ExecuteSimulateTab();
                case "SimulatePaste":
                    return ExecuteSimulatePaste();
                case "SimulateSelectAll":
                    return ExecuteSimulateSelectAll();
                case "SimulateDelete":
                    return ExecuteSimulateDelete();
                case "Delay":
                    return ExecuteDelay(resolveArg1String());

                // MainViewModel Operations
                case "GetCurrentPatientNumber":
                    return ExecuteGetCurrentPatientNumber();
                case "GetCurrentStudyDateTime":
                    return ExecuteGetCurrentStudyDateTime();
                case "GetCurrentHeader":
                    return ExecuteGetCurrentHeader();
                case "GetCurrentFindings":
                    return ExecuteGetCurrentFindings();
                case "GetCurrentConclusion":
                    return ExecuteGetCurrentConclusion();

                default:
                    return ("(unsupported)", null);
            }
        }

        /// <summary>
        /// Execute a procedure operation asynchronously (for operations that need async).
        /// Currently supports: GetTextOCR, GetHTML
        /// </summary>
        public static async Task<(string preview, string? value)> ExecuteOperationAsync(
            string operation,
            Func<AutomationElement?> resolveArg1Element,
            Func<string?> resolveArg1String,
            Func<string?> resolveArg2String,
            Func<string?> resolveArg3String,
            Dictionary<string, AutomationElement>? elementCache = null)
        {
            switch (operation)
            {
                case "GetTextOCR":
                    return await ExecuteGetTextOCRAsync(resolveArg1Element());

                case "GetHTML":
                    return await ExecuteGetHTMLAsync(resolveArg1String());

                default:
                    // Fallback to synchronous execution
                    return ExecuteOperation(operation, resolveArg1Element, resolveArg1String, resolveArg2String, resolveArg3String, elementCache);
            }
        }
    }
}
