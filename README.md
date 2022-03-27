# BitonicPixelSorter

GPU-accelerated pixel sorter with bitonic sorting for Unity.

The screenshot below shows it running on an NVIDIA GeForce GTX 2070 SUPER and it keeps over 250FPS at FHD resolution.

![image](https://user-images.githubusercontent.com/16096562/160289614-d61fd838-9a78-4e9a-b34f-e165f08e46f1.png)

## BitonicPixelSorter Component

![image](https://user-images.githubusercontent.com/16096562/125492519-6a363ad6-87b3-451b-a6a3-37b859821db5.png)
|Property|Type|Description|
|-|-|-|
|Use As Image Effect|`bool`|It works as an image effect when attached to the camera. This is only active when you are using legacy render pipeline.|
|Shader|`ComputeShader`|Set `BoitonicPixelSorter.compute`.|
|Direction|`bool`|Switches sorting direction between horizontal / vertical.|
|Ascending|`bool`|Switches ordering.|
|Threshold Min|`float`|Lower threshold of the brightness.|
|Threshold Max|`float`|Upper threshold of the brightness.|

### Use from code

```csharp
var sorter = GetComponent<BitonicPixelSorter>();

//BitonicPixelSorter.Execute(Texture src, RenderTexture dst)
sorter.Execute(sourceTexture, destinationTexture);
```

## References

https://github.com/hiroakioishi/UnityGPUBitonicSort

https://www.inf.hs-flensburg.de/lang/algorithmen/sortieren/bitonic/oddn.htm
