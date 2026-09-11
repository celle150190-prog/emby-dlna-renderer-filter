using System;
using MediaBrowser.Model.Plugins;

namespace Emby.DlnaRendererFilter
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool FilterEnabled { get; set; } = true;
        public RendererDevice[] Devices { get; set; } = Array.Empty<RendererDevice>();
        public string[] HiddenUuids { get; set; } = Array.Empty<string>();
    }

    public class RendererDevice
    {
        public string Uuid { get; set; }
        public string Usn { get; set; }
        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ModelName { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public string LastSeenUtc { get; set; }
    }
}
