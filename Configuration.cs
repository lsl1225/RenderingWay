using Dalamud.Configuration;

namespace RenderingWay;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>
    /// 是否停止游戏内 3D 渲染。
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Territory 发生变化时，自动关闭 Enabled。
    /// </summary>
    public bool AutoDisableOnTerritoryChange { get; set; } = true;
}
