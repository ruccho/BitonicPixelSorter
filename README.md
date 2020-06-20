# BitonicPixelSorter
 GPU-accelerated pixel sorter with bitonic sorting for Unity.

![image](https://user-images.githubusercontent.com/16096562/85205446-628a1f80-b356-11ea-8a0b-ddc198db3572.png)

## PixelSortingBitonic Component
![image](https://user-images.githubusercontent.com/16096562/85205482-8d747380-b356-11ea-9b32-ab1c613e5db0.png)
|Property|Type|Description|
|-|-|-|
|Use As Image Effect|`bool`|It works as an image effect when attached to the camera. This is only active when you are using legacy render pipeline.|
|Bitonic Sort Shader|`ComputeShader`|Set `PixelSortingBitonic.compute`.|
|Threshold Min|`float`|Lower limit of brightness threshold.|
|Threshold Max|`float`|Upper limit of brightness threshold.|

### Use from code
```csharp
var sorter = GetComponent<PixelSortingBitonic>();

sorter.Execute(sourceTrxture, destinationTexture);
```