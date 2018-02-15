using System.Windows;

namespace HomeOS.Hub.Common.WebCam.WebCamWrapper.Contracts
{
    public interface ITouchlessAddIn
    {
        string Name { get; }
        string Description { get; }
        bool HasConfiguration { get; }
        UIElement ConfigurationElement { get; }
    }
}