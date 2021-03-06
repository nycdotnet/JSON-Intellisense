﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using EnvDTE80;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Language.Intellisense;

namespace JSON_Intellisense.Bower
{
    class BowerNameCompletionEntry : JSONCompletionEntry
    {
        private DTE2 _dte;
        private JSONDocument _doc;
        internal static IEnumerable<string> _searchResults;

        public BowerNameCompletionEntry(string text, IIntellisenseSession session, DTE2 dte, JSONDocument doc)
            : base(text, "\"" + text + "\"", null, Constants.Icon, null, false, session as ICompletionSession)
        {
            _dte = dte;
            _doc = doc;
        }

        public override void Commit()
        {
            if (_doc == null)
            {
                base.Commit();
            }
            else
            {
                string searchTerm = _doc.GetMemberName(base.Session);

                if (string.IsNullOrEmpty(searchTerm))
                    return;

                ExecuteSearch(searchTerm);
            }
        }

        private void ExecuteSearch(string searchTerm)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                string url = string.Format(Constants.SearchUrl, HttpUtility.UrlEncode(searchTerm));
                string result = Helper.DownloadText(url);
                var children = GetChildren(result);

                if (children.Count() == 0)
                {
                    _dte.StatusBar.Text = "No packages found matching '" + searchTerm + "'";
                    base.Session.Dismiss();
                    return;
                }

                _dte.StatusBar.Text = string.Empty;
                _searchResults = children;

                Helper.ExecuteCommand(_dte, "Edit.CompleteWord");
            });
        }

        private static IEnumerable<string> GetChildren(string result)
        {
            var arr = Helper.ParseJSON(result) as JSONArray;

            if (arr == null)
                yield break;

            foreach (JSONBlockItemChild child in arr.BlockItemChildren.Take(25))
            {
                var foo = child.Children.First() as JSONObject;
                yield return foo.SelectItemText("name");
            }
        }
    }
}