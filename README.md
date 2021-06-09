# Ae.ImGuiBootstrapper

![.NET Core](https://github.com/alanedwardes/Ae.ImGuiBootstrapper/workflows/.NET/badge.svg?branch=master) [![](https://img.shields.io/nuget/v/Ae.ImGuiBootstrapper) ![](https://img.shields.io/badge/framework-netstandard2.0-blue)](https://www.nuget.org/packages/Ae.ImGuiBootstrapper/) 

Provides a simple interface to create ImGui windows on Windows, macOS, Linux.

![](https://s.edward.es/bfeba1bb-c2cb-46d3-96be-2af4a9fcc6dd.png)

## Hello World Example

The below code will render a simple window containing the text "Hello World".

```csharp
using Ae.ImGuiBootstrapper;
using ImGuiNET;
using System.Numerics;

class Program
{
    static void Main()
    {
        using var window = new ImGuiWindow("My App");

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
