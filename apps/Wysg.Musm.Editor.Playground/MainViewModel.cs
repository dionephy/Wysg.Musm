using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wysg.Musm.Editor.Controls;
using Ghosting = Wysg.Musm.Editor.Ghosting;

namespace Wysg.Musm.Editor.Playground
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly EditorControl _editor;
        private readonly Ghosting.IGhostSuggestionClient _ghostClient;
        private CancellationTokenSource? _cts;

        private string _status = "Ready";
        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        public MainViewModel(EditorControl editor, Ghosting.IGhostSuggestionClient? ghostClient = null)
        {
            _editor = editor;
            _ghostClient = ghostClient ?? new FakeGhostClient();

            // Fire when EditorControl announces 2s idle
            _editor.IdleElapsed += async (_, __) => await FetchAndUpdateGhostsAsync();
            // Simple sanity seed for the renderer path
            _editor.DebugSeedGhosts();
            _editor.EnableGhostDebugAnchors(false);
        }

        // Your MainWindow.xaml.cs calls this explicitly
        public Task ForceServerGhostsAsync() => FetchAndUpdateGhostsAsync(force: true);

        private async Task FetchAndUpdateGhostsAsync(bool force = false)
        {
            // cancel any in-flight request
            Interlocked.Exchange(ref _cts, new CancellationTokenSource())?.Cancel();
            var ct = _cts!.Token;

            try
            {
                // Close popup to avoid UI overlap
                _editor.DismissCompletionPopup();

                var req = BuildRequestFromEditorDynamic();
                var sw = System.Diagnostics.Stopwatch.StartNew();

                Ghosting.GhostResponse res = await _ghostClient.SuggestAsync(req, ct);

                var tuples = ProjectSuggestions(res); // List<(line,text)>
                var idxs = string.Join(",", tuples.Select(t => t.line));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _editor.UpdateServerGhosts(tuples);
                    Status = $"GhostApi: {sw.ElapsedMilliseconds} ms · {tuples.Count} lines · idx=[{idxs}] · {_editor.GetGhostDebugInfo()}";
                });
            }
            catch (OperationCanceledException)
            {
                // user typed again; ignore
            }
            catch (Exception ex)
            {
                Status = $"GhostApi error: {ex.GetType().Name}: {ex.Message}";
            }
        }

        // ---- Map EditorControl DPs → GhostRequest, regardless of DTO shape ----
        private Ghosting.GhostRequest BuildRequestFromEditorDynamic()
        {
            string reportText = _editor.DocumentText ?? string.Empty;
            string patientSex = _editor.PatientSex ?? "";
            int patientAge = _editor.PatientAge;
            string studyHeader = _editor.StudyHeader ?? "";
            string studyInfo = _editor.StudyInfo ?? "";

            var rt = typeof(Ghosting.GhostRequest);
            var ctors = rt.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            // Prefer a 5-arg (string,string,int,string,string) ctor if present
            var ctor5 = ctors.FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                if (p.Length != 5) return false;
                return p[0].ParameterType == typeof(string)
                    && p[1].ParameterType == typeof(string)
                    && (p[2].ParameterType == typeof(int) || p[2].ParameterType == typeof(int?))
                    && p[3].ParameterType == typeof(string)
                    && p[4].ParameterType == typeof(string);
            });

            if (ctor5 != null)
            {
                object? ageArg = ctor5.GetParameters()[2].ParameterType == typeof(int) ? patientAge : (int?)patientAge;
                return (Ghosting.GhostRequest)ctor5.Invoke(new object?[] { reportText, patientSex, ageArg, studyHeader, studyInfo })!;
            }

            // Fallback: parameterless + set properties if they exist
            var ctor0 = ctors.FirstOrDefault(c => c.GetParameters().Length == 0)
                        ?? throw new InvalidOperationException("GhostRequest requires a (text, sex, age, header, info) ctor or a parameterless ctor.");

            var req = (Ghosting.GhostRequest)ctor0.Invoke(Array.Empty<object?>())!;

            SetIfExists(req, "ReportText", reportText);
            SetIfExists(req, "PatientSex", patientSex);
            SetIfExists(req, "PatientAge", patientAge);
            SetIfExists(req, "StudyHeaderText", studyHeader); // some shapes use this flat string
            SetIfExists(req, "StudyHeader", studyHeader); // others might expect a serialized blob
            SetIfExists(req, "StudyInfo", studyInfo);

            // Optional tuning knobs (ignore if absent)
            SetIfExists(req, "TopK", 3);
            SetIfExists(req, "MaxPerLine", 1);
            SetIfExists(req, "LatencyBudgetMs", 800);

            return req;
        }

        // ---- Extract suggestions from GhostResponse without knowing exact property names ----
        private static List<(int line, string text)> ProjectSuggestions(Ghosting.GhostResponse response)
        {
            var list = new List<(int, string)>();
            object res = response;

            // Try 'Suggestions' then 'Lines'
            var items = GetProp(res, "Suggestions") ?? GetProp(res, "Lines");
            if (items is System.Collections.IEnumerable seq)
            {
                foreach (var it in seq)
                {
                    int line = GetIntProp(it, "LineIndex", "Line", "Index");
                    string text = GetStringProp(it, "Text", "Ghost", "Suggestion");
                    if (text is null) continue;
                    list.Add((line, text));
                }
            }
            return list;
        }

        // ---- Reflection helpers ----
        private static object? GetProp(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(obj);

        private static void SetIfExists(object obj, string name, object? value)
        {
            var pi = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi is null || !pi.CanWrite) return;

            var targetType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
            if (value != null && !targetType.IsInstanceOfType(value))
            {
                try
                {
                    value = Convert.ChangeType(value, targetType);
                }
                catch { return; }
            }
            pi.SetValue(obj, value);
        }

        private static int GetIntProp(object obj, params string[] names)
        {
            foreach (var n in names)
            {
                var pi = obj.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null)
                {
                    var v = pi.GetValue(obj);
                    if (v == null) continue;
                    try { return Convert.ToInt32(v); } catch { }
                }
            }
            return 0;
        }

        private static string GetStringProp(object obj, params string[] names)
        {
            foreach (var n in names)
            {
                var pi = obj.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null)
                {
                    var v = pi.GetValue(obj) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v!;
                }
            }
            return "";
        }

        // ------------- INotifyPropertyChanged -------------
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // =======================
        // Fallback demo client that fabricates a GhostResponse
        // =======================
        private sealed class FakeGhostClient : Ghosting.IGhostSuggestionClient
        {
            public Task<Ghosting.GhostResponse> SuggestAsync(Ghosting.GhostRequest request, CancellationToken ct)
            {
                string text = GetProp(request, "ReportText") as string ?? "";
                var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                // Build suggestions with reflection so we don't depend on ctor shape
                var suggestions = new List<object>();

                for (int i = 0; i < lines.Length; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    var l = (lines[i] ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(l)) continue;

                    //if (l.StartsWith("no acute", StringComparison.OrdinalIgnoreCase))
                        //suggestions.Add(CreateSuggestion(i, "No acute intracranial abnormality.", 0.82, "rule"));
                    //else 
                    if (l.Contains("microangiopathy", StringComparison.OrdinalIgnoreCase))
                        suggestions.Add(CreateSuggestion(i, "Mild degree of microangiopathy in bilateral cerebral white matter.", 0.78, "rule"));
                    else if (l.Contains("thalamus", StringComparison.OrdinalIgnoreCase))
                        suggestions.Add(CreateSuggestion(i, "Thalamus.", 0.72, "rule"));
                }

                var resp = CreateResponse(suggestions);
                return Task.FromResult(resp);
            }

            private static object CreateSuggestion(int lineIndex, string text, double conf, string source)
            {
                var st = typeof(Ghosting.GhostSuggestion);
                // Try 4-arg ctor (int,string,double,string)
                var ctor4 = st.GetConstructors().FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 4
                        && (p[0].ParameterType == typeof(int) || p[0].ParameterType == typeof(int?))
                        && p[1].ParameterType == typeof(string)
                        && (p[2].ParameterType == typeof(double) || p[2].ParameterType == typeof(double?))
                        && p[3].ParameterType == typeof(string);
                });
                if (ctor4 != null)
                {
                    object? a0 = ctor4.GetParameters()[0].ParameterType == typeof(int) ? lineIndex : (int?)lineIndex;
                    object? a2 = ctor4.GetParameters()[2].ParameterType == typeof(double) ? conf : (double?)conf;
                    return ctor4.Invoke(new object?[] { a0, text, a2, source })!;
                }

                // Try 2-arg ctor (int,string)
                var ctor2 = st.GetConstructors().FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 2
                        && (p[0].ParameterType == typeof(int) || p[0].ParameterType == typeof(int?))
                        && p[1].ParameterType == typeof(string);
                });
                if (ctor2 != null)
                {
                    object? a0 = ctor2.GetParameters()[0].ParameterType == typeof(int) ? lineIndex : (int?)lineIndex;
                    return ctor2.Invoke(new object?[] { a0, text })!;
                }

                // Fallback: parameterless + settable properties
                var obj = Activator.CreateInstance(st)!;
                SetIfExists(obj, "LineIndex", lineIndex);
                SetIfExists(obj, "Line", lineIndex);
                SetIfExists(obj, "Index", lineIndex);
                SetIfExists(obj, "Text", text);
                SetIfExists(obj, "Ghost", text);
                SetIfExists(obj, "Suggestion", text);
                SetIfExists(obj, "Confidence", conf);
                SetIfExists(obj, "Score", conf);
                SetIfExists(obj, "Source", source);
                return obj;
            }

            private static Ghosting.GhostResponse CreateResponse(List<object> suggestions)
            {
                var rt = typeof(Ghosting.GhostResponse);
                // Try ctor GhostResponse(IReadOnlyList<GhostSuggestion>)
                var ctorList = rt.GetConstructors().FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    if (p.Length != 1) return false;
                    var pt = p[0].ParameterType;
                    if (!pt.IsGenericType) return false;
                    return typeof(IEnumerable<>).IsAssignableFrom(pt.GetGenericTypeDefinition())
                           || typeof(IReadOnlyList<>).IsAssignableFrom(pt.GetGenericTypeDefinition());
                });
                if (ctorList != null)
                {
                    // Convert list<object> -> List<GhostSuggestion> via cast using LINQ
                    var targetElemType = ctorList.GetParameters()[0].ParameterType.GetGenericArguments()[0];
                    var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!.MakeGenericMethod(targetElemType);
                    var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(targetElemType);
                    var casted = castMethod.Invoke(null, new object?[] { suggestions })!;
                    var list = toListMethod.Invoke(null, new object?[] { casted })!;
                    return (Ghosting.GhostResponse)ctorList.Invoke(new object?[] { list })!;
                }

                // Fallback: parameterless + set Suggestions/Lines if available
                var ctor0 = rt.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0)
                            ?? throw new InvalidOperationException("GhostResponse requires a list ctor or a parameterless ctor.");

                var resp = (Ghosting.GhostResponse)ctor0.Invoke(Array.Empty<object?>())!;
                // Try to assign property
                var prop = rt.GetProperty("Suggestions", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                          ?? rt.GetProperty("Lines", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    // Build a strongly typed List<T> for the property type
                    var elemType = prop.PropertyType.IsArray
                                 ? prop.PropertyType.GetElementType()!
                                 : (prop.PropertyType.IsGenericType ? prop.PropertyType.GetGenericArguments()[0] : typeof(object));
                    var listType = typeof(List<>).MakeGenericType(elemType);
                    var list = Activator.CreateInstance(listType)!;

                    var add = listType.GetMethod("Add")!;
                    foreach (var s in suggestions)
                    {
                        // Try to convert element if needed, else rely on variance
                        add.Invoke(list, new[] { s });
                    }

                    if (prop.PropertyType.IsArray)
                    {
                        var toArray = listType.GetMethod("ToArray")!;
                        var arr = toArray.Invoke(list, Array.Empty<object?>())!;
                        prop.SetValue(resp, arr);
                    }
                    else
                    {
                        prop.SetValue(resp, list);
                    }
                }
                return resp;
            }

            private static object? GetProp(object obj, string name)
                => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(obj);
        }
    }
}
