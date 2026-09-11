using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.DlnaRendererFilter
{
    public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        private const string ThumbPngBase64 = "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";

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

            yield return new PluginPageInfo
            {
                Name = "DlnaRendererFilterjs",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.DlnaRendererFilter.js"
            };
        }

        public Stream GetThumbImage()
        {
            return new MemoryStream(Convert.FromBase64String(ThumbPngBase64), writable: false);
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;
    }
}
