using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;

namespace Emby.DlnaRendererFilter
{
    public sealed class RendererFilterEntryPoint : IServerEntryPoint
    {
        private readonly IDeviceDiscovery _discovery;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        private readonly object _sync = new object();
        private Timer _hookTimer;
        private bool _disposed;
        private bool _hookInstalled;
        private FieldInfo _eventField;
        private Delegate _originalPlayToHandler;
        private Delegate _proxyHandler;

        public RendererFilterEntryPoint(IDeviceDiscovery discovery, ILogger logger)
        {
            _discovery = discovery;
            _logger = logger;
        }

        public void Run()
        {
            _discovery.DeviceDiscovered += OnDeviceDiscovered;
            TryInstallHook();
            _hookTimer = new Timer(_ => TryInstallHook(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            _logger.Info("DLNA Renderer Filter: gestartet.");
        }

        private void OnDeviceDiscovered(object sender, GenericEventArgs<UpnpNotificationInfo> e)
        {
            var info = e?.Argument;
            if (info == null || !IsRenderer(info)) return;
            _ = CaptureDeviceAsync(info);
        }

        private void PlayToFilterProxy(object sender, GenericEventArgs<UpnpNotificationInfo> e)
        {
            var info = e?.Argument;
            if (info != null && IsRenderer(info))
            {
                _ = CaptureDeviceAsync(info);
                var uuid = ExtractUuid(info.Usn);
                if (ShouldBlock(uuid))
                {
                    _logger.Info("DLNA Renderer Filter: PlayTo-Renderer blockiert: {0} ({1})", uuid, info.Location);
                    return;
                }
            }

            try
            {
                _originalPlayToHandler?.DynamicInvoke(sender, e);
            }
            catch (TargetInvocationException ex)
            {
                _logger.ErrorException("DLNA Renderer Filter: Fehler beim Aufruf des originalen PlayTo-Handlers", ex.InnerException ?? ex);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("DLNA Renderer Filter: Fehler beim Aufruf des originalen PlayTo-Handlers", ex);
            }
        }

        private bool ShouldBlock(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid) || Plugin.Instance == null) return false;
            var cfg = Plugin.Instance.Configuration;
            if (cfg == null || !cfg.FilterEnabled || cfg.HiddenUuids == null) return false;
            return cfg.HiddenUuids.Any(x => string.Equals(NormalizeUuid(x), NormalizeUuid(uuid), StringComparison.OrdinalIgnoreCase));
        }

        private void TryInstallHook()
        {
            if (_disposed || _hookInstalled) return;

            lock (_sync)
            {
                if (_disposed || _hookInstalled) return;
                try
                {
                    foreach (var field in GetAllInstanceFields(_discovery.GetType()))
                    {
                        if (!typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                        var current = field.GetValue(_discovery) as Delegate;
                        if (current == null) continue;

                        var invocation = current.GetInvocationList();
                        for (var i = 0; i < invocation.Length; i++)
                        {
                            var handler = invocation[i];
                            var declaring = handler.Method.DeclaringType?.FullName ?? string.Empty;
                            var method = handler.Method.Name ?? string.Empty;

                            if (!declaring.Equals("Emby.Dlna.PlayTo.PlayToManager", StringComparison.Ordinal) ||
                                method.IndexOf("DeviceDiscovered", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;

                            var proxyMethod = GetType().GetMethod(nameof(PlayToFilterProxy), BindingFlags.Instance | BindingFlags.NonPublic);
                            var proxy = Delegate.CreateDelegate(field.FieldType, this, proxyMethod, false);
                            if (proxy == null) continue;

                            _originalPlayToHandler = handler;
                            _proxyHandler = proxy;
                            _eventField = field;
                            invocation[i] = proxy;
                            field.SetValue(_discovery, Combine(invocation));
                            _hookInstalled = true;
                            _hookTimer?.Dispose();
                            _hookTimer = null;
                            _logger.Info("DLNA Renderer Filter: PlayTo-Filter aktiv. Event-Feld: {0}", field.Name);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("DLNA Renderer Filter: PlayTo-Hook konnte nicht installiert werden", ex);
                }
            }
        }

        private static IEnumerable<FieldInfo> GetAllInstanceFields(Type type)
        {
            while (type != null)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    yield return field;
                type = type.BaseType;
            }
        }

        private static Delegate Combine(IEnumerable<Delegate> handlers)
        {
            Delegate result = null;
            foreach (var handler in handlers)
                result = result == null ? handler : Delegate.Combine(result, handler);
            return result;
        }

        private async Task CaptureDeviceAsync(UpnpNotificationInfo info)
        {
            var uuid = ExtractUuid(info.Usn);
            if (string.IsNullOrWhiteSpace(uuid) || Plugin.Instance == null) return;

            var location = info.Location?.ToString() ?? string.Empty;
            var ip = info.Location?.Host ?? string.Empty;
            string friendlyName = null;
            string manufacturer = null;
            string modelName = null;

            if (!string.IsNullOrWhiteSpace(location))
            {
                try
                {
                    var xml = await _httpClient.GetStringAsync(location).ConfigureAwait(false);
                    var doc = XDocument.Parse(xml);
                    friendlyName = FindLocalValue(doc, "friendlyName");
                    manufacturer = FindLocalValue(doc, "manufacturer");
                    modelName = FindLocalValue(doc, "modelName");
                }
                catch (Exception ex)
                {
                    _logger.Debug("DLNA Renderer Filter: Gerätebeschreibung für {0} konnte nicht gelesen werden: {1}", uuid, ex.Message);
                }
            }

            lock (_sync)
            {
                var plugin = Plugin.Instance;
                if (plugin == null) return;

                var cfg = plugin.Configuration ?? new PluginConfiguration();
                var devices = (cfg.Devices ?? Array.Empty<RendererDevice>()).ToList();
                var existing = devices.FirstOrDefault(x => string.Equals(NormalizeUuid(x.Uuid), NormalizeUuid(uuid), StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    existing = new RendererDevice { Uuid = uuid };
                    devices.Add(existing);
                }

                existing.Usn = info.Usn;
                existing.Location = location;
                existing.IpAddress = ip;
                existing.LastSeenUtc = DateTime.UtcNow.ToString("o");
                if (!string.IsNullOrWhiteSpace(friendlyName)) existing.FriendlyName = friendlyName;
                if (!string.IsNullOrWhiteSpace(manufacturer)) existing.Manufacturer = manufacturer;
                if (!string.IsNullOrWhiteSpace(modelName)) existing.ModelName = modelName;

                cfg.Devices = devices.OrderBy(x => x.FriendlyName ?? x.Uuid).ToArray();
                plugin.UpdateConfiguration(cfg);
            }
        }

        private static string FindLocalValue(XDocument doc, string localName)
        {
            return doc.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static bool IsRenderer(UpnpNotificationInfo info)
        {
            return (info.Usn ?? string.Empty).IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (info.NotificationType ?? string.Empty).IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ExtractUuid(string usn)
        {
            if (string.IsNullOrWhiteSpace(usn)) return string.Empty;
            var value = usn.Trim();
            var marker = value.IndexOf("uuid:", StringComparison.OrdinalIgnoreCase);
            if (marker >= 0) value = value.Substring(marker + 5);
            var separator = value.IndexOf("::", StringComparison.Ordinal);
            if (separator >= 0) value = value.Substring(0, separator);
            return value.Trim();
        }

        private static string NormalizeUuid(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = value.Trim();
            if (value.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase)) value = value.Substring(5);
            var separator = value.IndexOf("::", StringComparison.Ordinal);
            if (separator >= 0) value = value.Substring(0, separator);
            return value.Trim();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _hookTimer?.Dispose();
            _hookTimer = null;
            _discovery.DeviceDiscovered -= OnDeviceDiscovered;

            lock (_sync)
            {
                try
                {
                    if (_hookInstalled && _eventField != null && _proxyHandler != null && _originalPlayToHandler != null)
                    {
                        var current = _eventField.GetValue(_discovery) as Delegate;
                        if (current != null)
                        {
                            var handlers = current.GetInvocationList().ToList();
                            for (var i = 0; i < handlers.Count; i++)
                            {
                                if (handlers[i].Equals(_proxyHandler))
                                {
                                    handlers[i] = _originalPlayToHandler;
                                    _eventField.SetValue(_discovery, Combine(handlers));
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("DLNA Renderer Filter: Original-PlayTo-Handler konnte beim Beenden nicht wiederhergestellt werden", ex);
                }
            }

            _httpClient.Dispose();
        }
    }
}
