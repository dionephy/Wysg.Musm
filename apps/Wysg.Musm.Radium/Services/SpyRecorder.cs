using System;
using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using FlaUI.Core.Conditions;

namespace Wysg.Musm.Radium.Services
{
    public static class SpyRecorder
    {
        public static UiBookmarks.Bookmark BuildChain(string processName, Window mainWindow, AutomationElement target, bool preferAutomationId = true, int maxAncestors = 256)
        {
            var chain = new List<AutomationElement>();
            var cur = target;
            while (cur != null && !Equals(cur, mainWindow) && chain.Count < maxAncestors)
            {
                chain.Add(cur);
                cur = cur.Parent;
            }
            chain.Reverse();

            var b = new UiBookmarks.Bookmark { Name = string.Empty, ProcessName = processName };
            AutomationElement currentScope = mainWindow;
            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            foreach (var nodeEl in chain)
            {
                string? automationId = nodeEl.AutomationId;
                string? className = nodeEl.ClassName;
                string? name = nodeEl.Name;
                var ct = nodeEl.ControlType;

                ConditionBase? q = null;
                if (preferAutomationId && !string.IsNullOrEmpty(automationId)) q = cf.ByAutomationId(automationId);
                if (!string.IsNullOrEmpty(className)) q = (q == null ? cf.ByClassName(className) : q.And(cf.ByClassName(className)));
                if (ct != ControlType.Custom) q = (q == null ? cf.ByControlType(ct) : q.And(cf.ByControlType(ct)));
                if (!string.IsNullOrEmpty(name)) q = (q == null ? cf.ByName(name) : q.And(cf.ByName(name)));
                q ??= cf.ByClassName(className ?? string.Empty);

                var matches = currentScope.FindAllDescendants(q);
                int index = 0;
                for (int i = 0; i < matches.Length; i++)
                {
                    if (ReferenceEquals(matches[i].FrameworkAutomationElement, nodeEl.FrameworkAutomationElement)) { index = i; break; }
                }

                b.Chain.Add(new UiBookmarks.Node
                {
                    Name = name,
                    ClassName = className,
                    AutomationId = (preferAutomationId ? automationId : null),
                    ControlTypeId = (int)ct,
                    IndexAmongMatches = index,
                    Include = true,
                    UseName = !string.IsNullOrEmpty(name),
                    UseClassName = !string.IsNullOrEmpty(className),
                    UseControlTypeId = ct != ControlType.Custom,
                    UseAutomationId = preferAutomationId && !string.IsNullOrEmpty(automationId),
                    UseIndex = true
                });

                currentScope = matches.Length > 0 ? matches[index] : nodeEl;
            }

            return b;
        }
    }
}