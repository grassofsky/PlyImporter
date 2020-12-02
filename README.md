# PlyImporter
原始项目的说明见：https://github.com/3DBear/PlyImporter

PLY (Polygon File Format) importer for Unity.

在原来基础上的改动主要有

- 增加了PlyElement基类，专门用来处理不同的element，方便后期进行扩展，目前支持PlyFaceElement，PlyVertexElement；
- 增加了PlyProperty基类，处理不同的属性，按照属性的功能又分为PlyMultiProperty，用来支持单个property定义；PlyListProperty支持property list定义。

## 支持的功能以及需要满足的要求

### 1. element vertex

元素顶点支持的格式类似如下：

```
element vertex 6462
property float x
property float y
property float z
property float nx
property float ny
property float nz
property uchar red
property uchar green
property uchar blue
property uchar alpha
```

其中

- `x,y,z,nx,ny,nz,red,green,blue,alpha`的名字是固定的；
- `x,y,z`必须同时出现；类型必须是`float`
- `nx,ny,nz`必须同时出现；类型必须是`float`
- `red,green,blue,alpha`属性必须同时出现；类型必须是`uchar`

- `property`定义的顺序可以是乱序的；
- `vertex`的名字是固定的；

### 2. element face

类似如下：

```
element face 12920
property list uchar int vertex_indices
```

其中

- `vertex_indices`的名字是固定的；
- `face`的名字是固定的；
- list中的具体index类型支持int和uint两种格式；
- list中的count类型为uchar；

### 3. element g_material

考虑到unity中提供的shader一般不支持顶点颜色设置，此处对常规的ply进行了一定的扩展；在comment区域给出全局元素和属性的设置；

```
comment element g_material
comment property uchar red 255
comment property uchar green 0
comment property uchar blue 0
comment property uchar alpha 255
```

其中：

- `g_material`名字是固定的；`red,green,blue,alpha`名字是固定的，必须是`uchar`

### 4. element g_meshinfo

考虑到需要识别网格名称，引入该元素，具体如下：

```
comment element g_meshinfo
comment property string name what_ever_you_want
comment property string unit mm               { one of cm/m/mm }
comment property string x_inner x                   { one of x/y/z/-x/-y/-z}
comment property string y_inner y                   { one of x/y/z/-x/-y/-z}
comment property string z_inner z                   { one of x/y/z/-x/-y/-z}
comment property string coordinate right      { one of right/left }
```

其中：

- `g_meshinfo`名字是不定的；
- `name,unit,x_inner,y_inner,z_inner,coordinate`名字是固定的；
- `name,unit,x_inner,y_inner,z_inner,coordinate`的类型必须是`string`；
- 具体的名字`what_ever_you_want`中不能带空格；
- `unit`的值为`mm/cm/m`中的一个;
- `coordinate`的值为`right/left`中的一个，right表示右手坐标系，left表示左手坐标系；
- `comment property string x_inner x`前一个`x_inner`指的是unity坐标系中的x轴，后一个x对应网格数据中的x轴；用于定义坐标变换；
- property的顺序也可变化；

### 5. data store order

必须先存储顶点element，然后再存储face element；

## TODO

- 支持PlyEdgeElement；
- Support for Binary Big Endian
- PLY exporting