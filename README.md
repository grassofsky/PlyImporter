# PlyImporter
原始项目的说明见：https://github.com/3DBear/PlyImporter

PLY (Polygon File Format) importer for Unity.

在原来基础上的改动主要有

- 增加了PlyElement基类，专门用来处理不同的element，方便后期进行扩展，目前支持PlyFaceElement，PlyVertexElement；
- 增加了PlyProperty基类，处理不同的属性，按照属性的功能又分为PlyMultiProperty，用来支持单个property定义；PlyListProperty支持property list定义。

## TODO

- 支持PlyEdgeElement；
- 考虑到Unity中的shader基本没有用到顶点的颜色，增加PlyMaterialElement用来设定材质颜色；
- 通常对于加载的文件，需要知道该文件对应的网格名称，扩展comment支持更多的选项；
- Support for Binary Big Endian
- PLY exporting