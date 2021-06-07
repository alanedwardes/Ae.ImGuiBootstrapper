# Ae.ImGuiBootstrapper

![.NET Core](https://github.com/alanedwardes/Ae.ImGuiBootstrapper/workflows/.NET%20Core/badge.svg?branch=main)

Provides a simple interface to create ImGui windows on Windows, macOS, Linux.

![](https://s.edward.es/bfeba1bb-c2cb-46d3-96be-2af4a9fcc6dd.png)

## Hello World Example
[![](https://img.shields.io/nuget/v/Ae.ImGuiBootstrapper) ![](https://img.shields.io/badge/framework-netstandard2.0-blue)](https://www.nuget.org/packages/Ae.ImGuiBootstrapper/) 

The below code will render a simple window containing the text "Hello World".

```csharp
class Program
{
    static void Main()
    {
        var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Hello World Sample");

        using var window = new ImGuiWindow(windowInfo);

        while (window.Loop(new Vector3(0.45f, 0.55f, 0.6f)))
        {
            ImGui.Begin("Window Title");
            ImGui.Text("Hello World!");
            ImGui.End();
        }
    }
}
```

See the [samples](https://github.com/alanedwardes/Ae.ImGuiBootstrapper/tree/master/samples) folder for more details.
