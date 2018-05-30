﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utility.Strings;

namespace Collector.Common.Platform
{
    public static class Subjects
    {
        public static int[] AddSubjects(string[] subjects, string[] hierarchy)
        {
            var query = new Query.Subjects();
            var parentId = 0;
            var breadcrumb = "";
            var parentTitle = "";
            var parentBreadcrumb = "";
            if (hierarchy.Length > 0)
            {
                var parentHier = hierarchy.ToList();
                parentTitle = hierarchy[hierarchy.Length - 1];
                parentHier.RemoveAt(parentHier.Count - 1);
                parentBreadcrumb = string.Join(">", parentHier);
                breadcrumb = string.Join(">", hierarchy);
                var subject = query.GetSubjectByTitle(parentTitle, parentBreadcrumb);
                parentId = subject.subjectId;
            }

            var ids = new List<int>();
            foreach (string subject in subjects)
            {
                ids.Add(query.CreateSubject(parentId, 0, 0, subject, breadcrumb));
            }
            return ids.ToArray();
        }

        public static Service.Response RenderSubjectsList(int parentId = 0, bool getHierarchy = false, bool isFirst = false)
        {
            var server = Server.Instance;
            var inject = new Service.Response() { };

            var html = new StringBuilder();
            var query = new Query.Subjects();
            var list = new Scaffold("/Views/Subjects/subject.html", server.Scaffold);
            var item = new Scaffold("/Views/Subjects/list-item.html", server.Scaffold);
            var subjects = query.GetList("", parentId);
            var indexes = new string[] { };
            if (parentId > 0)
            {
                var details = query.GetSubjectById(parentId);
                if (details == null) {
                    throw new ServiceErrorException("Parent subject does not exist");
                }

                //set up subject
                var crumb = details.breadcrumb.Replace(">", " &gt; ");
                if (details.parentId == 0) { crumb = details.title; } else { crumb += " &gt; " + details.title; }
                indexes = details.hierarchy.Split('>');
                list.Data["parentId"] = details.subjectId.ToString();
                list.Data["no-words"] = details.haswords == false ? "1" : "";
                list.Data["breadcrumbs"] = crumb;

                if (indexes.Length >= 1 && getHierarchy == true)
                {
                    var hier = details.hierarchy;
                    var bread = details.breadcrumb;
                    if (bread != "") { bread += ">" + details.title; } else { bread = details.title; }
                    var pId = "0";
                    if (hier != "")
                    {
                        var hier2 = hier.Split('>');
                        pId = hier2[hier2.Length - 1];
                    }
                    inject.javascript = "S.subjects.buttons.selectSubject(" + parentId + "," + pId + ",'" + bread + "', 0, true);";

                    //get inject object for parent within hierarchy
                    var parent = RenderSubjectsList(indexes.Length > 1 ? int.Parse(indexes[indexes.Length - 2]) : 0, indexes.Length > 1 ? true : false);
                    html.Append(parent.html + "\n");
                    inject.javascript += parent.javascript;
                }
            }
            else
            {
                list.Data["parentId"] = "0";
            }


            //set up subject sub-items
            subjects.ForEach((Query.Models.Subject subject) =>
            {
                var breadcrumbs = subject.breadcrumb;
                if (breadcrumbs == "") { breadcrumbs = subject.title; }
                item.Data["subjectId"] = subject.subjectId.ToString();
                item.Data["parentId"] = subject.parentId.ToString();
                item.Data["breadcrumbs"] = subject.breadcrumb.Replace(">", "&gt;") + (subject.breadcrumb != "" ? "&gt;" : "") + subject.title;
                item.Data["title"] = subject.title.Capitalize();
                html.Append(item.Render() + "\n");
            });

            list.Data["subjects-list"] = html.ToString();

            inject.html = list.Render();
            return inject;
        }
    }
}