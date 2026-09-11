using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.DlnaRendererFilter
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override Guid Id => Guid.Parse("6c35d290-4ef5-47e9-8c7f-51eac8be8c14");
        public override string Name => "DLNA Renderer Filter";
        public override string Description => "Zeigt erkannte DLNA-Renderer an und blendet ausgewählte Geräte aus Embys 'Wiedergabe auf' aus.";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = "DlnaRendererFilter",
                DisplayName = "DLNA Renderer Filter",
                EnableInMainMenu = true,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            };
        }
    }
}
