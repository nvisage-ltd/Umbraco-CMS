<%@ Control Language="C#" AutoEventWireup="True" CodeBehind="LoadStarterKits.ascx.cs" Inherits="Umbraco.Web.UI.Install.Steps.Skinning.LoadStarterKits" %>
<%@ Import Namespace="Umbraco.Web.org.umbraco.our" %>

<%@ Register TagPrefix="umb" Namespace="ClientDependency.Core.Controls" Assembly="ClientDependency.Core" %>

<asp:PlaceHolder ID="pl_loadStarterKits" runat="server">
    
    <umb:JsInclude ID="JsInclude1" runat="server" FilePath="installer/js/PackageInstaller.js" PathNameAlias="UmbracoClient" />

    <% if (!CannotConnect) { %>
    <script type="text/javascript">
        (function ($) {
            $(document).ready(function () {
                var installer = new Umbraco.Installer.PackageInstaller({
                    starterKits: $("a.selectStarterKit"),
                    baseUrl: "<%= PackageInstallServiceBaseUrl %>",
                    serverError: $("#serverError"),
                    connectionError: $("#connectionError"),
                    setProgress: updateProgressBar,
                    setStatusMessage: updateStatusMessage
                });
                installer.init();
            });
        })(jQuery);
    </script>
    <% } %>

    <asp:Repeater ID="rep_starterKits" runat="server">
        <HeaderTemplate>
             <ul class="thumbnails">
        </HeaderTemplate>
        <ItemTemplate>
             <li class="add-<%# ((Package)Container.DataItem).Text.Replace(" ","").ToLower() %>">
                    <div class="image">
                        <a href="#" class="single-tab selectStarterKit" title="<%# ((Package)Container.DataItem).Text %>" data-repoId="<%# ((Package)Container.DataItem).RepoGuid %>">
                            <img src="http://our.umbraco.org<%# ((Package)Container.DataItem).Thumbnail %>" alt="<%# ((Package)Container.DataItem).Text %>">
                        </a>
                    </div>
            </li>
        </ItemTemplate>
        <FooterTemplate>
                </ul>
                
                 <asp:LinkButton runat="server" ID="declineStarterKits" CssClass="declineKit" OnClientClick="return confirm('Are you sure you do not want to install a starter kit?');" OnClick="NextStep">
                    No thanks, do not install a starterkit
                </asp:LinkButton>
        </FooterTemplate>
    </asp:Repeater>

</asp:PlaceHolder>

<div id="connectionError" style="<%= CannotConnect ? "" : "display:none;" %>">
    
    <div style="padding: 0 100px 13px 5px;">
        <h2>Oops...the installer can't connect to the repository</h2>
        Starter Kits could not be fetched from the repository as there was no connection - which can occur if you are using a proxy server or firewall with certain configurations,
            or if you are not currently connected to the internet.
            <br />
        Click <strong>Continue</strong> to complete the installation then navigate to the Developer section of your Umbraco installation
            where you will find the Starter Kits listed in the Packages tree.
    </div>

    <!-- btn box -->
    <footer class="btn-box">
        <div class="t">&nbsp;</div>
        <asp:LinkButton ID="LinkButton1" class="btn-step btn btn-continue" runat="server" OnClick="GotoLastStep"><span>Continue</span></asp:LinkButton>
    </footer>

</div>

<div id="serverError" style="display:none;">
    
    <div style="padding: 0 100px 13px 5px;">
        <h2>Oops...the installer encountered an error</h2>
        <div class="error-message"></div>
    </div>

    <!-- btn box -->
    <footer class="btn-box">
        <div class="t">&nbsp;</div>
        <asp:LinkButton ID="LinkButton2" class="btn-step btn btn-continue" runat="server" OnClick="GotoLastStep"><span>Continue</span></asp:LinkButton>
    </footer>

</div>