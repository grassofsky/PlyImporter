using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Globalization;

namespace ThreeDeeBear.Models.Ply
{
    public enum CoordinateType
    {
        Right,
        Left
    }

    public enum PlyFormat
    {
        Ascii,
        BinaryBigEndian,
        BinaryLittleEndian,
        Unknown
    }

    public abstract class PlyProperty
    {
        protected string _name;

        public PlyProperty(string valueFormat)
        {
            ValueNumFormat = PlyProperty.GetDataFormat(valueFormat);
            ValueDataBytes = PlyProperty.GetDataBytes(ValueNumFormat);
        }

        public enum ENumFormat
        {
            nf_uchar,
            nf_ushort,
            nf_uint,
            nf_char,
            nf_short,
            nf_int,
            nf_float,
            nf_double,
            nf_string,
            nf_invalid
        };

        public bool IsValid = true;

        public ENumFormat ValueNumFormat;

        public int ValueDataBytes;

        public int Index = -1;

        public static int GetDataBytes(ENumFormat numFormat)
        {
            int dataBytes = 0;
            switch (numFormat)
            {
                case ENumFormat.nf_uchar:
                case ENumFormat.nf_char:
                    dataBytes = 1;
                    break;
                case ENumFormat.nf_ushort:
                case ENumFormat.nf_short:
                    dataBytes = 2;
                    break;
                case ENumFormat.nf_int:
                case ENumFormat.nf_float:
                case ENumFormat.nf_uint:
                    dataBytes = 4;
                    break;
                case ENumFormat.nf_double:
                    dataBytes = 8;
                    break;
            }
            return dataBytes;
        }

        protected static ENumFormat GetDataFormat(string format)
        {
            ENumFormat numFormat;
            switch (format)
            {
                case "uchar":
                    numFormat = ENumFormat.nf_uchar;
                    break;
                case "ushort":
                    numFormat = ENumFormat.nf_ushort;
                    break;
                case "uint":
                    numFormat = ENumFormat.nf_uint;
                    break;
                case "char":
                    numFormat = ENumFormat.nf_char;
                    break;
                case "short":
                    numFormat = ENumFormat.nf_short;
                    break;
                case "int":
                    numFormat = ENumFormat.nf_int;
                    break;
                case "float":
                    numFormat = ENumFormat.nf_float;
                    break;
                case "double":
                    numFormat = ENumFormat.nf_double;
                    break;
                case "string":
                    numFormat = ENumFormat.nf_string;
                    break;
                default:
                    numFormat = ENumFormat.nf_invalid;
                    break;
            }
            return numFormat;
        }
    }

    public class PlyMultiProperty : PlyProperty
    {
        public int BytesOffset;
        public string Value = null; // Used for global property

        public PlyMultiProperty(string format, string name, int index, int bytesOffset, string value = null) : base(format)
        {
            BytesOffset = bytesOffset;
            IsValid = ValueNumFormat != ENumFormat.nf_invalid;
            _name = name;
            Index = index;
            Value = value;
        }
    }

    public class PlyListProperty : PlyProperty
    {
        public ENumFormat CountNumFormat;

        public PlyListProperty(string countFormat, string valueFormat, string name) : base(valueFormat)
        {
            CountNumFormat = PlyProperty.GetDataFormat(countFormat);
            IsValid = CountNumFormat != ENumFormat.nf_invalid && ValueNumFormat != ENumFormat.nf_invalid;
            _name = name;
        }
    }

    public abstract class PlyElement
    {
        public bool IsValid = true;
        public int NElement = 0;
        public IDictionary<string, PlyProperty> DictProperties = null;

        public string ElementName { set; get; } 
        public bool IsGlobal { set; get; }

        public PlyElement(string name, bool isGlobal=false)
        {
            ElementName = name;
            IsGlobal = isGlobal;
        }

        public static byte[] GetBytesSubarray(byte[] content, int start, int count)
        {
            byte[] subarray = new byte[count];
            for (int i = 0; i < count; ++i)
            {
                subarray[i] = content[start + i];
            }

            return subarray;
        }
		
        protected bool ParseElementInfo(IList<string> headerUnparsed)
        {
            var elementStartIndex = headerUnparsed.IndexOf(headerUnparsed.FirstOrDefault(x => x.Contains("element " + ElementName)));
            if (elementStartIndex == -1)
            {
                return false;
            }

            NElement = IsGlobal ? 1 : Convert.ToInt32(headerUnparsed[elementStartIndex].Split(' ')[2]);
            if (!ParsePropertyList(headerUnparsed, elementStartIndex) && !ParseMultiProperties(headerUnparsed, elementStartIndex))
            {
                return false;
            }

            return true;
        }

        private bool ParsePropertyList(IList<string> header, int elementIndex)
        {
            if (IsGlobal)
            {
                return false;
            }

            DictProperties = new Dictionary<string, PlyProperty>();
            if (header[elementIndex+1].Contains("property list"))
            {
                var propertyElements = header[elementIndex + 1].Split(' ');
                PlyListProperty plyProperty = new PlyListProperty(propertyElements[2], propertyElements[3], propertyElements[4]);
                if (!plyProperty.IsValid)
                {
                    Debug.LogWarning("Ply: Unknown property value type of element " + ElementName);
                    return false;
                }
                DictProperties.Add(propertyElements[4], plyProperty);
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool ParseMultiProperties(IList<string> header, int elementIndex)
        {
            int propertyIndexOffset = 0;
            if (IsGlobal)
            {
                propertyIndexOffset = 1;
            }
            DictProperties = new Dictionary<string, PlyProperty>();
            int iStart = elementIndex + 1;
            int bytesOffset = 0;
            string value = null;
            for (int i = iStart; i < header.Count; i++)
            {
                var propertyLine = header[i];
                if (propertyLine.Contains("property"))
                {
                    var propertyElements =  propertyLine.Split(' ');
                    if (IsGlobal)
                    {
                        value = propertyElements[3 + propertyIndexOffset];
                    }
                    PlyMultiProperty plyProperty = new PlyMultiProperty(propertyElements[1 + propertyIndexOffset], propertyElements[2 + propertyIndexOffset], i - iStart, bytesOffset, value);
                    if (!IsGlobal)
                    {
                        bytesOffset += plyProperty.ValueDataBytes;
                    }
                    if (!plyProperty.IsValid)
                    {
                        Debug.LogWarning("Ply: Unknown property value type of element " + ElementName);
                        return false;
                    }
                    DictProperties.Add(propertyElements[2 + propertyIndexOffset], plyProperty);
                }
                else
                {
                    break;
                }
            }
            return true;
        }
    
        protected bool CheckValueDataTypeEqual(IList<string> keys, PlyProperty.ENumFormat dataType)
        {
            bool isEqual = true;
            foreach (var key in keys)
            {
                isEqual = isEqual && (dataType == DictProperties[key].ValueNumFormat);
            }
            return isEqual;
        }
    }

    public class PlyVertexElement : PlyElement
    {
        public bool HasPosition = false;
        public bool HasColor = false;
        public bool HasNormal = false;

        private PlyHeader _header;

        public PlyVertexElement(IList<string> headerUnparsed, PlyHeader header) : base("vertex")
        {
            _header = header;
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            HasPosition = DictProperties.ContainsKey("x") && DictProperties.ContainsKey("y") && DictProperties.ContainsKey("z");
            HasNormal = DictProperties.ContainsKey("nx") && DictProperties.ContainsKey("ny") && DictProperties.ContainsKey("nz");
            HasColor = DictProperties.ContainsKey("red") && DictProperties.ContainsKey("green") &&
                DictProperties.ContainsKey("blue") && DictProperties.ContainsKey("alpha");


            IsValid = IsValid && IsElementValid();
        }

        public void ParseElement(IList<string> vertexContents, 
            out IList<Vector3> vertices, out IList<Vector3> normals, out IList<Color32> colors)
        {
            if (NElement == 0)
            {
                vertices = null;
                normals = null;
                colors = null;
                return;
            }

            vertices = HasPosition ? new List<Vector3>() : null;
            normals = HasNormal ? new List<Vector3>() : null;
            colors = HasColor ? new List<Color32>() : null;

            int[] positionIndex = new int[3];
            int[] normalIndex = new int[3];
            int[] colorIndex = new int[4];

            GetPositionIndex(positionIndex);
            GetNormalIndex(normalIndex);
            GetColorIndex(colorIndex);

            int[] coordinateTranslate;
            int[] signs;
            _header.GlobalMeshInfoElement.GetCoordinateTransalte(out coordinateTranslate, out signs);
            float ratioToMeter = _header.GlobalMeshInfoElement.GetUnitToMeterRatio();
            float[] value = new float[3];
            foreach (var vertexContent in vertexContents)
            {
                var vertexProperties = vertexContent.Split(' ');
                if (HasPosition)
                {
                    float.TryParse(vertexProperties[positionIndex[0]], NumberStyles.Float, CultureInfo.InvariantCulture, out value[0]);
                    float.TryParse(vertexProperties[positionIndex[1]], NumberStyles.Float, CultureInfo.InvariantCulture, out value[1]);
                    float.TryParse(vertexProperties[positionIndex[2]], NumberStyles.Float, CultureInfo.InvariantCulture, out value[2]);
                    vertices.Add(new Vector3(value[coordinateTranslate[0]] * signs[0], value[coordinateTranslate[1]] * signs[1], value[coordinateTranslate[2]] * signs[2]) * ratioToMeter);
                }
                if (HasNormal)
                {
                    float.TryParse(vertexProperties[normalIndex[0]], NumberStyles.Float, CultureInfo.InvariantCulture, out value[0]);
                    float.TryParse(vertexProperties[normalIndex[1]], NumberStyles.Float, CultureInfo.InvariantCulture, out value[1]);
                    float.TryParse(vertexProperties[normalIndex[2]], NumberStyles.Float, CultureInfo.InvariantCulture, out value[2]);
                    Vector3 normal = new Vector3(value[coordinateTranslate[0]] * signs[0], value[coordinateTranslate[1]] * signs[1], value[coordinateTranslate[2]] * signs[2]) * ratioToMeter;
                    normal.Normalize();
                    normals.Add(normal);
                }
                if (HasColor)
                {
                    byte r, g, b, a;
                    byte.TryParse(vertexProperties[colorIndex[0]], out r);
                    byte.TryParse(vertexProperties[colorIndex[1]], out g);
                    byte.TryParse(vertexProperties[colorIndex[2]], out b);
                    byte.TryParse(vertexProperties[colorIndex[3]], out a);
                    colors.Add(new Color32(r, g, b, a));
                }
            }
        }

        public void ParseElementBinaryLittleEndian(byte[] bytes, out int bytesUsed,
            out IList<Vector3> vertices, out IList<Vector3> normals, out IList<Color32> colors)
        {
            if (NElement == 0)
            {
                bytesUsed = 0;
                vertices = null;
                normals = null;
                colors = null;
                return;
            }

            vertices = HasPosition ? new List<Vector3>() : null;
            normals = HasNormal ? new List<Vector3>() : null;
            colors = HasColor ? new List<Color32>() : null;

            int bytesPerVertex = 0;
            foreach (var keyvalue in DictProperties)
            {
                bytesPerVertex += keyvalue.Value.ValueDataBytes;
            }
            bytesUsed = bytesPerVertex * NElement;

            int[] coordinateTranslate;
            int[] signs;
            _header.GlobalMeshInfoElement.GetCoordinateTransalte(out coordinateTranslate, out signs);
            float ratioToMeter = _header.GlobalMeshInfoElement.GetUnitToMeterRatio();
            float[] value = new float[3];
            for (int i = 0; i < NElement; ++i)
            {
                int byteIndex = i * bytesPerVertex;

                if (HasPosition)
                {
                    PlyMultiProperty xmultiProperty = (DictProperties["x"] as PlyMultiProperty);
                    PlyMultiProperty ymultiProperty = (DictProperties["y"] as PlyMultiProperty);
                    PlyMultiProperty zmultiProperty = (DictProperties["z"] as PlyMultiProperty);
                    value[0] = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + xmultiProperty.BytesOffset, xmultiProperty.ValueDataBytes), 0);
                    value[1] = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + ymultiProperty.BytesOffset, ymultiProperty.ValueDataBytes), 0);
                    value[2] = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + zmultiProperty.BytesOffset, zmultiProperty.ValueDataBytes), 0);
                    vertices.Add(new Vector3(value[coordinateTranslate[0]]*signs[0], value[coordinateTranslate[1]]*signs[1], value[coordinateTranslate[2]]*signs[2]) * ratioToMeter);
                }

                if (HasNormal)
                {
                    PlyMultiProperty xmultiProperty = (DictProperties["nx"] as PlyMultiProperty);
                    PlyMultiProperty ymultiProperty = (DictProperties["ny"] as PlyMultiProperty);
                    PlyMultiProperty zmultiProperty = (DictProperties["nz"] as PlyMultiProperty);
                    value[0] = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + xmultiProperty.BytesOffset, xmultiProperty.ValueDataBytes), 0);
                    value[1] = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + ymultiProperty.BytesOffset, ymultiProperty.ValueDataBytes), 0);
                    value[2] = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + zmultiProperty.BytesOffset, zmultiProperty.ValueDataBytes), 0);
                    Vector3 normal = new Vector3(value[coordinateTranslate[0]]*signs[0], value[coordinateTranslate[1]]*signs[1], value[coordinateTranslate[2]]*signs[2]) * ratioToMeter;
                    normal.Normalize();
                    normals.Add(normal);
                }

                if (HasColor)
                {
                    PlyMultiProperty redmultiProperty = (DictProperties["red"] as PlyMultiProperty);
                    PlyMultiProperty greenmultiProperty = (DictProperties["green"] as PlyMultiProperty);
                    PlyMultiProperty bluemultiProperty = (DictProperties["blue"] as PlyMultiProperty);
                    PlyMultiProperty alphamultiProperty = (DictProperties["alpha"] as PlyMultiProperty);
                    byte r = bytes[byteIndex + redmultiProperty.BytesOffset];
                    byte g = bytes[byteIndex + greenmultiProperty.BytesOffset];
                    byte b = bytes[byteIndex + bluemultiProperty.BytesOffset];
                    byte a = bytes[byteIndex + alphamultiProperty.BytesOffset];
                    colors.Add(new Color32(r, g, b, a));
                }
            }
        }

        private bool GetPositionIndex(int[] index)
        {
            if (!HasPosition || index.Length != 3)
            {
                return false;
            }
            index[0] = DictProperties["x"].Index;
            index[1] = DictProperties["y"].Index;
            index[2] = DictProperties["z"].Index;
            return true;
        }

        private bool GetColorIndex(int[] index)
        {
            if (!HasColor || index.Length != 4)
            {
                return false;
            }
            index[0] = DictProperties["red"].Index;
            index[1] = DictProperties["green"].Index;
            index[2] = DictProperties["blue"].Index;
            index[3] = DictProperties["alpha"].Index;
            return true;
        }

        private bool GetNormalIndex(int[] index)
        {
            if (!HasNormal || index.Length != 3)
            {
                return false;
            }
            index[0] = DictProperties["nx"].Index;
            index[1] = DictProperties["ny"].Index;
            index[2] = DictProperties["nz"].Index;
            return true;
        }

        private bool IsElementValid()
        {
            int nProperty = 0;
            if (HasPosition) nProperty += 3;
            if (HasNormal) nProperty += 3;
            if (HasColor) nProperty += 4;

            bool dataTypePositionEqual = HasPosition ? CheckValueDataTypeEqual(new List<string> { "x", "y", "z" }, PlyProperty.ENumFormat.nf_float) : true;
            bool dataTypeColorEqual = HasColor ? CheckValueDataTypeEqual(new List<string> { "red", "green", "blue", "alpha" }, PlyProperty.ENumFormat.nf_uchar) : true;
            bool dataTypeNormalEqual = HasNormal ? CheckValueDataTypeEqual(new List<string> { "nx", "ny", "nz" }, PlyProperty.ENumFormat.nf_float) : true;

            return HasPosition && nProperty == DictProperties.Count && dataTypePositionEqual && dataTypeColorEqual && dataTypeNormalEqual;
        }
    }

    public class PlyFaceElement : PlyElement
    {
        private PlyHeader _plyHeader;

        public PlyFaceElement(IList<string> headerUnparsed, PlyHeader header) : base("face")
        {
            _plyHeader = header;
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            IsValid = IsValid && IsElementValid();
        }

        public void ParseElement(IList<string> faceContents, out List<int> triangles)
        {
            if (NElement == 0)
            {
                triangles = null;
                return;
            }

            triangles = new List<int>();

            CoordinateType coordType = _plyHeader.GlobalMeshInfoElement.GetCoordinateType();
            foreach (var faceContent in faceContents)
            {
                var split = faceContent.Split(' ');
                var count = Convert.ToInt32(split.First());
                if (count != 3)
                {
                    Debug.LogWarning("Warning: Found a face is not a triangle face, skipping...");
                    continue;
                }

                List<int> triangle = split.ToList().GetRange(1, 3).Select(x => Convert.ToInt32(x)).ToList();
                if (coordType == CoordinateType.Left)
                {
                    triangles.AddRange(triangle);
                }
                else // CoordinateType.Right
                {
                    List<int> newTriangle = new List<int>{ triangle[0], triangle[2], triangle[1] };
                    triangles.AddRange(newTriangle);
                }
            }
        }

        public void ParseElementBinaryLittleEndian(byte[] bytes, int bytesOffset, 
            out int bytesUsed, out List<int> triangles)
        {
            if (NElement == 0)
            {
                bytesUsed = 0;
                triangles = null;
                return;
            }

            var listProperty = DictProperties["vertex_indices"] as PlyListProperty;
            bytesUsed = PlyProperty.GetDataBytes(listProperty.CountNumFormat)+ NElement * 3 * PlyProperty.GetDataBytes(listProperty.ValueNumFormat);
            
            triangles = new List<int>();

            int facesRead = 0;
            int bytesRead = 0;
            int bytesPerTriangleIndex = 4;
            CoordinateType coordType = _plyHeader.GlobalMeshInfoElement.GetCoordinateType();

            int[] triangle = new int[3];
            while (facesRead < NElement)
            {
                var faceIndex = bytesOffset + bytesRead;
                var indexCount = bytes[faceIndex];
                if (indexCount == 3)
                {
                    for (int i=0; i<indexCount; ++i)
                    {
                        triangle[i] = System.BitConverter.ToInt32(PlyElement.GetBytesSubarray(bytes, faceIndex + 1 + i * bytesPerTriangleIndex, bytesPerTriangleIndex), 0);
                    }
                    if (coordType == CoordinateType.Left)
                    {
                        triangles.AddRange(triangle);
                    }
                    else // Coordinate.Right
                    {
                        int tmp = triangle[1];
                        triangle[1] = triangle[2];
                        triangle[2] = tmp;
                        triangles.AddRange(triangle);
                    }

                    bytesRead += 1 + indexCount * bytesPerTriangleIndex;
                }
                else
                {
                    Debug.LogWarning("Warning: Found a face is not a triangle face, skipping...");
                }

                facesRead++;
            }
        }

        private bool IsElementValid()
        {
            bool isValid = DictProperties.Count == 1 && DictProperties.ContainsKey("vertex_indices");
            var listProperty = (DictProperties["vertex_indices"] as PlyListProperty);
            isValid = isValid && 
                (listProperty.ValueNumFormat == PlyProperty.ENumFormat.nf_int || 
                 listProperty.ValueNumFormat == PlyProperty.ENumFormat.nf_uint);
            isValid = isValid && (listProperty.CountNumFormat == PlyProperty.ENumFormat.nf_uchar);

            return isValid;
        }
    }


    public class PlyLineElement : PlyElement
    {
        public PlyLineElement(IList<string> headerUnparsed) : base("line")
        {
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            IsValid = IsValid && IsElementValid();
        }

        public void ParseElement(IList<string> lineContents, out List<List<int>> lines)
        {
            if (NElement == 0)
            {
                lines = null;
                return;
            }

            lines = new List<List<int>>();

            foreach (var lineContent in lineContents)
            {
                var split = lineContent.Split(' ').ToList();
                var count = Convert.ToInt32(split[0]);
                if (count < 2)
                {
                    Debug.LogWarning("Warning: Found a line points < 2, skipping...");
                    continue;
                }

                List<int> line = split.GetRange(1, split.Count-1).Select(x => Convert.ToInt32(x)).ToList();
                lines.Add(line);
            }
        }

        // TODO: test
        public void ParseElementBinaryLittleEndian(byte[] bytes, int bytesOffset, out int bytesUsed, out List<List<int>> lines)
        {
            if (NElement == 0)
            {
                bytesUsed = 0;
                lines = null;
                return;
            }

            lines = new List<List<int>>();

            int linesRead = 0;
            int bytesRead = 0;
            int bytesPerLineIndex = 4;

            int[] triangle = new int[3];
            while (linesRead < NElement)
            {
                var lineIndex = bytesOffset + bytesRead;

                var count = bytes[lineIndex];
                bytesRead += 1 + count * bytesPerLineIndex;
                if (count < 2)
                {
                    Debug.LogWarning("Warning: Found a line points < 2, skipping...");
                    continue;
                }

                var line = new List<int>();
                for (int i = 0; i < count; ++i)
                {
                    line.Add(System.BitConverter.ToInt32(PlyElement.GetBytesSubarray(bytes, lineIndex + 1 + i * bytesPerLineIndex, bytesPerLineIndex), 0));
                }
                lines.Add(line);
                linesRead++;
            }
            bytesUsed = bytesRead;
        }


        private bool IsElementValid()
        {
            bool isValid = DictProperties.Count == 1 && DictProperties.ContainsKey("vertex_indices");


            var listProperty = (DictProperties["vertex_indices"] as PlyListProperty);
            isValid = isValid &&
                (listProperty.ValueNumFormat == PlyProperty.ENumFormat.nf_int ||
                 listProperty.ValueNumFormat == PlyProperty.ENumFormat.nf_uint);
            isValid = isValid && (listProperty.CountNumFormat == PlyProperty.ENumFormat.nf_uchar);

            return isValid;
        }
    }

    public class PlyLineColorElement : PlyElement
    {
        public PlyLineColorElement(IList<string> headerUnparsed) : base("line_color")
        {
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            IsValid = IsValid && IsElementValid();
        }

        public void ParseElement(IList<string> lineContents, out List<Color32> lineColors)
        {
            if (NElement == 0)
            {
                lineColors = null;
                return;
            }

            lineColors = new List<Color32>();

            foreach (var lineContent in lineContents)
            {
                var split = lineContent.Split(' ').ToList();
                Color32 color = new Color32(Convert.ToByte(split[0]), Convert.ToByte(split[1]), Convert.ToByte(split[2]), Convert.ToByte(split[3]));
                lineColors.Add(color);
            }
        }

        public void ParseElementBinaryLittleEndian(byte[] bytes, int bytesOffset, out int bytesUsed, out List<Color32> lineColors)
        {
            if (NElement == 0)
            {
                bytesUsed = 0;
                lineColors = null;
                return;
            }

            lineColors = new List<Color32>();

            int linesRead = 0;
            int bytesRead = 0;

            int[] triangle = new int[3];
            while (linesRead < NElement)
            {
                var lineIndex = bytesOffset + bytesRead;

                // First read color info
                PlyMultiProperty redmultiProperty = (DictProperties["red"] as PlyMultiProperty);
                PlyMultiProperty greenmultiProperty = (DictProperties["green"] as PlyMultiProperty);
                PlyMultiProperty bluemultiProperty = (DictProperties["blue"] as PlyMultiProperty);
                PlyMultiProperty alphamultiProperty = (DictProperties["alpha"] as PlyMultiProperty);
                byte r = bytes[lineIndex + redmultiProperty.BytesOffset];
                byte g = bytes[lineIndex + greenmultiProperty.BytesOffset];
                byte b = bytes[lineIndex + bluemultiProperty.BytesOffset];
                byte a = bytes[lineIndex + alphamultiProperty.BytesOffset];

                lineColors.Add(new Color32(r, g, b, a));
                bytesRead += 4; // rgba 4 bytes

                linesRead++;
            }
            bytesUsed = bytesRead;
        }

        private bool IsElementValid()
        {
            bool isValid = DictProperties.Count == 4 && DictProperties.ContainsKey("red") && DictProperties.ContainsKey("blue") &&
                DictProperties.ContainsKey("green") && DictProperties.ContainsKey("alpha");

            isValid = isValid && CheckValueDataTypeEqual(new List<string> { "red", "green", "blue", "alpha" }, PlyProperty.ENumFormat.nf_uchar);

            return isValid;
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    public class PlyEdgeElement : PlyElement
    {
        public PlyEdgeElement(IList<string> headerUnparsed) : base("edge")
        {
        }

        public bool IsElementValid()
        {
            return DictProperties.Count == 2 && DictProperties.ContainsKey("vertex1") && DictProperties.ContainsKey("vertex2"); 
        }
    }

    public class PlyGlobalMaterialElement : PlyElement
    {
        public PlyGlobalMaterialElement(IList<string> headerUnparsed) : base("g_material", true)
        {
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            IsValid = IsValid && IsElementValid();
        }

        public Color GetColor()
        {
            if (!IsValid)
            {
                return new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }

            byte r, g, b, a;
            byte.TryParse((DictProperties["red"] as PlyMultiProperty).Value, out r);
            byte.TryParse((DictProperties["green"] as PlyMultiProperty).Value, out g);
            byte.TryParse((DictProperties["blue"] as PlyMultiProperty).Value, out b);
            byte.TryParse((DictProperties["alpha"] as PlyMultiProperty).Value, out a);
            return new Color((float)r/255.0f, (float)g/255.0f, (float)b/255.0f, (float)a/255.0f);
        }

        private bool IsElementValid()
        {
            bool HasColor = DictProperties.ContainsKey("red") && DictProperties.ContainsKey("green") &&
                            DictProperties.ContainsKey("blue") && DictProperties.ContainsKey("alpha");

            return HasColor && CheckValueDataTypeEqual(new List<string> { "red", "green", "blue", "alpha" }, PlyProperty.ENumFormat.nf_uchar);
        }
    }

    public class PlyGlobalMeshInfoElement : PlyElement
    {
        private List<string> _validKey = new List<string>{ "name", "unit", "x_inner", "y_inner", "z_inner", "coordinate" };
        private List<string> _validValue = new List<string> { "cm", "m", "mm", "x", "y", "z", "-x", "-y", "-z", "right", "left" };

        private Dictionary<string, int> _axises = new Dictionary<string, int> { { "x", 0 }, { "y", 1 }, { "z", 2 } };

        public PlyGlobalMeshInfoElement(IList<string> headerUnparsed) : base("g_meshinfo", true)
        {
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            foreach (var element in DictProperties)
            {
                if (element.Key == "name")
                {
                    continue;
                }
                if (!_validKey.Contains(element.Key))
                {
                    IsValid = false;
                    break;
                }
                if (!_validValue.Contains((element.Value as PlyMultiProperty).Value))
                {
                    IsValid = false;
                    break;
                }
            }
        }

        public string GetName()
        {
            if (!IsValid)
            {
                return "";
            }

            return (DictProperties["name"] as PlyMultiProperty).Value;
        }

        public void GetCoordinateTransalte(out int[] coordinate, out int[] sign)
        {
            coordinate = new int[3];
            sign = new int[3];
            for (int i=0; i<coordinate.Length; ++i)
            {
                coordinate[i] = i;
                sign[i] = 1;
            }

            if (!IsValid)
            {
                return;
            }
            
            if (DictProperties.ContainsKey("x_inner"))
            {
                string value = (DictProperties["x_inner"] as PlyMultiProperty).Value;
                if (value[0] == '-')
                {
                    sign[0] = -1;
                    value = value.Substring(1);
                }
                coordinate[0] = _axises[value];
            }

            if (DictProperties.ContainsKey("y_inner"))
            {
                string value = (DictProperties["y_inner"] as PlyMultiProperty).Value;
                if (value[0] == '-')
                {
                    sign[1] = -1;
                    value = value.Substring(1);
                }
                coordinate[1] = _axises[value];
            }

            if(DictProperties.ContainsKey("z_inner"))
            {
                string value = (DictProperties["z_inner"] as PlyMultiProperty).Value;
                if (value[0] == '-')
                {
                    sign[2] = -1;
                    value = value.Substring(1);
                }
                coordinate[2] = _axises[value];
            }
        }

        public float GetUnitToMeterRatio()
        {
            if (!IsValid)
            {
                return 1.0f;
            }

            string unit = (DictProperties["unit"] as PlyMultiProperty).Value;
            if (unit == "mm")
            {
                return 0.001f;
            }
            else if (unit == "cm")
            {
                return 0.01f;
            }

            return 1.0f;
        }

        public CoordinateType GetCoordinateType()
        {
            if (!IsValid)
            {
                return CoordinateType.Left;
            }

            if ((DictProperties["coordinate"] as PlyMultiProperty).Value == "right")
            {
                return CoordinateType.Right;
            }

            return CoordinateType.Left;
        }
    }

    public class PlyHeader
    {
        public PlyFormat Format;
        public PlyVertexElement VertexElement;
        public PlyFaceElement FaceElement;
        public PlyLineElement LineElement;
        public PlyLineColorElement LineColorElement;
        public PlyGlobalMaterialElement GloablMaterialElement;
        public PlyGlobalMeshInfoElement GlobalMeshInfoElement;

        public List<string> RawHeader;

        public PlyHeader(List<string> headerUnparsed)
        {
            Format = GetFormat(headerUnparsed.FirstOrDefault(x => x.Contains("format")).Split(' ')[1]);
            GloablMaterialElement = new PlyGlobalMaterialElement(headerUnparsed);
            GlobalMeshInfoElement = new PlyGlobalMeshInfoElement(headerUnparsed);
            VertexElement = new PlyVertexElement(headerUnparsed, this);
            FaceElement = new PlyFaceElement(headerUnparsed, this);
            LineElement = new PlyLineElement(headerUnparsed);
            LineColorElement = new PlyLineColorElement(headerUnparsed);
            RawHeader = headerUnparsed;
        }

		private PlyFormat GetFormat(string formatLine)
        {
            switch (formatLine)
            {
                case "binary_little_endian":
                    return PlyFormat.BinaryLittleEndian;
                case "binary_big_endian":
                    return PlyFormat.BinaryBigEndian;
                case "ascii":
                    return PlyFormat.Ascii;
                default:
                    return PlyFormat.Unknown;
            }
        }
    }
}