define(['baseView', 'loading', 'emby-button', 'emby-checkbox', 'emby-scroller', 'flexStyles'], function (BaseView, loading) {
    'use strict';

    var pluginId = '6c35d290-4ef5-47e9-8c7f-51eac8be8c14';

    function esc(value) {
        var div = document.createElement('div');
        div.textContent = value || '';
        return div.innerHTML;
    }

    function norm(value) {
        return (value || '').replace(/^uuid:/i, '').split('::')[0].trim().toLowerCase();
    }

    function View(view, params) {
        BaseView.apply(this, arguments);
        this.config = null;

        var instance = this;
        view.querySelector('#DlnaRendererFilterForm').addEventListener('submit', function (e) {
            e.preventDefault();
            instance.save();
            return false;
        });
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.render = function () {
        var view = this.view;
        var config = this.config;
        var list = view.querySelector('#rendererList');
        var devices = (config && config.Devices) || [];
        var hidden = ((config && config.HiddenUuids) || []).map(norm);

        view.querySelector('#FilterEnabled').checked = !config || config.FilterEnabled !== false;

        if (!devices.length) {
            list.innerHTML = '<p>Noch keine Renderer erkannt. Starte Emby neu oder öffne „Wiedergabe auf …“, damit eine SSDP-Suche ausgelöst wird.</p>';
            return;
        }

        list.innerHTML = devices.map(function (d, i) {
            var id = 'renderer_' + i;
            var uuid = norm(d.Uuid);
            var visible = hidden.indexOf(uuid) === -1;
            var title = d.FriendlyName || d.ModelName || d.Uuid || 'Unbekannter Renderer';
            var details = [d.Manufacturer, d.ModelName, d.IpAddress].filter(Boolean).join(' · ');
            return '<div class="checkboxContainer checkboxContainer-withDescription" style="padding:12px 0;border-bottom:1px solid rgba(255,255,255,.12)">' +
                '<label class="emby-checkbox-label">' +
                '<input type="checkbox" is="emby-checkbox" class="rendererToggle" id="' + id + '" data-uuid="' + esc(d.Uuid) + '" ' + (visible ? 'checked' : '') + ' />' +
                '<span>' + esc(title) + '</span></label>' +
                '<div class="fieldDescription">' + esc(details) + '</div>' +
                '<div class="fieldDescription" style="font-family:monospace">UUID: ' + esc(d.Uuid) + '</div>' +
                '</div>';
        }).join('');
    };

    View.prototype.onResume = function (options) {
        BaseView.prototype.onResume.apply(this, arguments);

        var instance = this;
        loading.show();
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            instance.config = config;
            instance.render();
            loading.hide();
        }, function () {
            loading.hide();
        });
    };

    View.prototype.save = function () {
        var instance = this;
        var view = this.view;
        var config = this.config;

        if (!config) {
            return;
        }

        loading.show();
        config.FilterEnabled = view.querySelector('#FilterEnabled').checked;
        config.HiddenUuids = Array.prototype.slice.call(view.querySelectorAll('.rendererToggle'))
            .filter(function (x) { return !x.checked; })
            .map(function (x) { return x.getAttribute('data-uuid'); });

        ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
            Dashboard.processPluginConfigurationUpdateResult(result);
            instance.config = config;
            loading.hide();
        }, function () {
            loading.hide();
        });
    };

    return View;
});
