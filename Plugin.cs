using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.GameFunctions;

namespace RenderingWay;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IFramework framework;
    private readonly IClientState clientState;

    private readonly Configuration configuration;

    private bool configWindowOpen;
    private bool renderRequestPlaced;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IClientState clientState)
    {
        this.pluginInterface = pluginInterface;
        this.framework = framework;
        this.clientState = clientState;

        // 初始化 ECommons。
        ECommonsMain.Init(pluginInterface, this);

        // 读取配置。
        configuration =
            pluginInterface.GetPluginConfig() as Configuration
            ?? new Configuration();

        // Framework Update 中处理 RenderDisableManager。
        framework.Update += OnFrameworkUpdate;

        // Territory 切换事件。
        clientState.TerritoryChanged += OnTerritoryChanged;

        // 配置窗口。
        pluginInterface.UiBuilder.Draw += DrawConfig;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (configuration.Enabled)
        {
            if (!renderRequestPlaced)
            {
                RenderDisableManager.PlaceRequest();
                renderRequestPlaced = true;
            }
        }
        else
        {
            if (renderRequestPlaced)
            {
                RenderDisableManager.RemoveRequest();
                renderRequestPlaced = false;
            }
        }
    }

    private void OnTerritoryChanged(uint territoryId)
    {
        if (!configuration.AutoDisableOnTerritoryChange)
            return;

        if (!configuration.Enabled)
            return;

        // Territory 切换后关闭 Enabled。
        //
        // RenderDisableManager 的 RemoveRequest 会在下一次
        // Framework Update 中执行，从而保证调用线程符合要求。
        configuration.Enabled = false;

        SaveConfiguration();
    }

    private void OpenConfigUi()
    {
        configWindowOpen = true;
    }

    private void DrawConfig()
    {
        if (!configWindowOpen)
            return;

        ImGui.SetNextWindowSize(
            new Vector2(430, 160),
            ImGuiCond.FirstUseEver);

        if (!ImGui.Begin(
                "RenderingWay Configuration",
                ref configWindowOpen,
                ImGuiWindowFlags.None))
        {
            ImGui.End();
            return;
        }

        var enabled = configuration.Enabled;

        if (ImGui.Checkbox(
                "Enabled##RenderingWayEnabled",
                ref enabled))
        {
            configuration.Enabled = enabled;
            SaveConfiguration();
        }

        ImGui.TextWrapped(
            "启用后停止游戏内的 3D 场景渲染。Dalamud 和插件 ImGui 界面仍可继续显示。");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var autoDisable =
            configuration.AutoDisableOnTerritoryChange;

        if (ImGui.Checkbox(
                "切换 Territory 时自动关闭 Enabled",
                ref autoDisable))
        {
            configuration.AutoDisableOnTerritoryChange =
                autoDisable;

            SaveConfiguration();
        }

        ImGui.End();
    }

    private void SaveConfiguration()
    {
        pluginInterface.SavePluginConfig(configuration);
    }

    public void Dispose()
    {
        // 先停止自己的事件。
        framework.Update -= OnFrameworkUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;

        pluginInterface.UiBuilder.Draw -= DrawConfig;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;

        // ECommonsMain.Dispose 内部会清理当前插件在
        // RenderDisableManager 中留下的渲染关闭请求。
        ECommonsMain.Dispose();
    }
}
