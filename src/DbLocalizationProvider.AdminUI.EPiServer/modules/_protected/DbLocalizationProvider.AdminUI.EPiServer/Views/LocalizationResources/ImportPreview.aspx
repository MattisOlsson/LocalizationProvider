﻿<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<DbLocalizationProvider.AdminUI.PreviewImportResourcesViewModel>" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="System.Web.Mvc.Html" %>
<%@ Import Namespace="EPiServer.Framework.Web.Mvc.Html"%>
<%@ Import Namespace="EPiServer.Framework.Web.Resources"%>
<%@ Import Namespace="EPiServer.Shell" %>
<%@ Import Namespace="EPiServer.Shell.Navigation" %>
<%@ Import Namespace="EPiServer" %>
<%@ Import Namespace=" EPiServer.Shell.Web.Mvc.Html"%>
<%@ Assembly Name="EPiServer.Shell.UI" %>
<%@ Import Namespace=" DbLocalizationProvider"%>
<%@ Import Namespace="DbLocalizationProvider.AdminUI" %>
<%@ Import Namespace="DbLocalizationProvider.Import" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Import Localization Resources</title>
    
    <%= Html.CssLink(Paths.ToClientResource(typeof(LocalizationResourceViewModel), "ClientResources/bootstrap.min.css"))%>
    <%= Html.CssLink(Paths.ToClientResource(typeof(LocalizationResourceViewModel), "ClientResources/bootstrap-editable.css"))%>

    <%= Page.ClientResources("ShellCore") %>
    <%= Page.ClientResources("ShellWidgets") %>
    <%= Page.ClientResources("ShellCoreLightTheme") %>
    <%= Page.ClientResources("ShellWidgetsLightTheme")%>
    <%= Page.ClientResources("Navigation") %>
    <%= Page.ClientResources("DijitWidgets", new[] { ClientResourceType.Style })%>

    <%= Html.CssLink(UriSupport.ResolveUrlFromUIBySettings("App_Themes/Default/Styles/ToolButton.css")) %>
    <%--<%= Html.CssLink(Paths.ToClientResource("CMS", "ClientResources/Epi/Base/CMS.css"))%>--%>
    <%= Html.ScriptResource(UriSupport.ResolveUrlFromUtilBySettings("javascript/episerverscriptmanager.js"))%>
    <%= Html.ScriptResource(UriSupport.ResolveUrlFromUIBySettings("javascript/system.js")) %>
    <%= Html.ScriptResource(UriSupport.ResolveUrlFromUIBySettings("javascript/dialog.js")) %>
    <%= Html.ScriptResource(UriSupport.ResolveUrlFromUIBySettings("javascript/system.aspx")) %>

    <%= Html.ScriptResource(Paths.ToClientResource(typeof(PreviewImportResourcesViewModel), "ClientResources/jquery-2.0.3.min.js"))%>
    <%= Html.ScriptResource(Paths.ToClientResource(typeof(PreviewImportResourcesViewModel), "ClientResources/bootstrap.min.js"))%>
    <%= Html.ScriptResource(Paths.ToClientResource(typeof(PreviewImportResourcesViewModel), "ClientResources/bootstrap-editable.min.js"))%>

    <style type="text/css">
        body {
            font-size: 1.2em;
        }

        .epi-contentContainer.has-changes {
            max-width: 100%;
        }

        .EP-systemMessage {
            display: block;
            border: solid 1px #878787;
            background-color: #fffdbd;
            padding: 0.3em;
            margin-top: 0.5em;
            margin-bottom: 0.5em;
        }

        tr.insert {
            background-color: ##90c99f;
        }

        tr.update {
            background-color: #ffe7ba;
        }

    </style>
</head>
<body>
    <% if (Model.ShowMenu)
       {
           %><%= Html.GlobalMenu(string.Empty, "/global/cms/localization") %><%
       } %>
    <div class="epi-contentContainer epi-padding <%= Model.Changes.Any() ? "has-changes" : "" %>">
        <div class="epi-contentArea epi-paddingHorizontal">
            <h1 class="EP-prefix">Import Localization Resources (PREVIEW)</h1>
            <form id="backForm" action="<%= Model.ShowMenu ? Url.Action("Main") : Url.Action("Index") %>" method="get"></form>
            <div class="epi-paddingVertical">
                <form action="<%= Url.Action("CommitImportResources") %>" method="post" enctype="multipart/form-data" id="importForm">
                    <input type="hidden" name="showMenu" value="<%= Model.ShowMenu %>"/>
                    <% if(Model.Changes.Any())
                       { %>
                        <p class="EP-systemInfo">Please review your pending changes:</p>
                        <div class="epi-formArea">
                            <table style="border: none; max-width: 150px">
                                <tr>
                                    <td><input type="checkbox" class="changeTypeSelector insert" data-changeType="insert" /></td>
                                    <td>Inserts:</td>
                                    <td><%= Model.Changes.Count(c => c.ChangeType == ChangeType.Insert) %></td>
                                </tr>
                                <tr>
                                    <td><input type="checkbox" class="changeTypeSelector update" data-changeType="update" /></td>
                                    <td>Updates:</td>
                                    <td><%= Model.Changes.Count(c => c.ChangeType == ChangeType.Update) %></td>
                                </tr>
                                <tr>
                                    <td><input type="checkbox" class="changeTypeSelector delete" data-changeType="delete" /></td>
                                    <td>Deletes:</td>
                                    <td><%= Model.Changes.Count(c => c.ChangeType == ChangeType.Delete) %></td>
                                </tr>
                            </table>
                            <div class="table-responsive">
                                <table class="table table-bordered" id="importResourceList" style="clear: both">
                                    <tr>
                                        <th rowspan="2">Choose</th>
                                        <th rowspan="2">Operation</th>
                                        <th rowspan="2">Key</th>
                                        <% foreach (var language in Model.Languages)
                                           { %>
                                               <th colspan="2" class="text-center"><%= language.EnglishName %></th>
                                        <% } %>
                                    </tr>
                                    <tr>
                                    <% foreach (var language in Model.Languages)
                                       { %>
                                            <th>Importing</th>
                                            <th>Existing</th>
                                    <% } %>
                                    </tr>
                                    <%
                                        var i = 0;
                                        foreach (var change in Model.Changes)
                                        { %>
                                    <tr class="<%= change.ChangeType.ToString().ToLower() %>">
                                        <td>
                                            <input type="checkbox" name="changes[<%= i %>].Selected" class="selectImportingResource" data-changeType="<%= change.ChangeType.ToString().ToLower() %>" value="true"/>
                                            <input type="hidden" name="changes[<%= i %>].Selected" value="false"/>
                                            <input type="hidden" name="changes[<%= i %>].ChangeType" value="<%= change.ChangeType %>" />
                                        </td>
                                        <td><%= change.ChangeType %></td>
                                        <td>
                                            <%= change.ImportingResource.ResourceKey %>
                                            <input type="hidden" name="changes[<%= i %>].ImportingResource.ResourceKey" value="<%= change.ImportingResource.ResourceKey %>"/>
                                        </td>
                                        <%
                                            var ii = 0;
                                            foreach (var language in Model.Languages)
                                            { %>
                                                <td>
                                                    <%= change.ImportingResource.Translations.ByLanguage(language) %>
                                                    <input type="hidden" name="changes[<%= i %>].ImportingResource.Translations[<%= ii %>].Language" value="<%= language.Name %>" />
                                                    <input type="hidden" name="changes[<%= i %>].ImportingResource.Translations[<%= ii %>].Value" value="<%= change.ImportingResource.Translations.ByLanguage(language) %>" />
                                                </td>
                                                <td><%= change.ExistingResource.Translations.ByLanguage(language) %></td>
                                        <%
                                            ii++;
                                            } %>
                                    </tr>
                                    <%
                                        i++;
                                        } %>
                                </table>
                            </div>
                        </div>
                    <% }
                       else
                       { %>
                        <p class="EP-systemInfo">You all good, no changes detected!</p>
                    <%
                       } %>
                    <div class="epi-buttonContainer">
                        <% if(Model.Changes.Any())
                           { %>
                            <span class="epi-cmsButton">
                                <input class="epi-cmsButton-text epi-cmsButton-tools epi-cmsButton-Import" type="submit" id="importResources" value="Import" title="Import" />
                            </span>
                        <% } %>
                        <span class="epi-cmsButton">
                            <input class="epi-cmsButton-text epi-cmsButton-tools epi-cmsButton-Undo" type="button" id="back" value="Back" title="Back" onclick="$('#backForm').submit();" />
                        </span>
                    </div>
                </form>
            </div>
        </div>
    </div>
    
    <script type="text/javascript">
        $(function() {
            $('.changeTypeSelector').change(function () {
                var $this = $(this),
                    changeType = $this.data('changetype');

                if (this.checked) {
                    $('tr.' + changeType + ' input[type="checkbox"]').prop('checked', true);
                } else {
                    $('tr.' + changeType + ' input[type="checkbox"]').prop('checked', false);
                }
            });

            $('input.selectImportingResource').change(function() {
                var $this = $(this),
                    changeType = $this.data('changetype');

                if (this.checked) {
                    $('.changeTypeSelector.' + changeType).prop('checked', true);
                } else {
                    if ($('tr.' + changeType + ' input[type="checkbox"]:checked').length == 0) {
                        $('.changeTypeSelector.' + changeType).prop('checked', false);
                    }
                }
            });
        });
    </script>
</body>
</html>
