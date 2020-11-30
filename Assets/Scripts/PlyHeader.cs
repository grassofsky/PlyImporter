using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Globalization;

namespace ThreeDeeBear.Models.Ply
{

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

        public string Name { set { } get { return _name; } }

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
        public int NElement;
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

        public PlyVertexElement(IList<string> headerUnparsed) : base("vertex")
        {
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
            vertices = HasPosition ? new List<Vector3>() : null;
            normals = HasNormal ? new List<Vector3>() : null;
            colors = HasColor ? new List<Color32>() : null;

            int[] positionIndex = new int[3];
            int[] normalIndex = new int[3];
            int[] colorIndex = new int[4];

            GetPositionIndex(positionIndex);
            GetNormalIndex(normalIndex);
            GetColorIndex(colorIndex);

            foreach (var vertexContent in vertexContents)
            {
                var vertexProperties = vertexContent.Split(' ');
                if (HasPosition)
                {
                    float x, y, z;
                    float.TryParse(vertexProperties[positionIndex[0]], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                    float.TryParse(vertexProperties[positionIndex[1]], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                    float.TryParse(vertexProperties[positionIndex[2]], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
                    vertices.Add(new Vector3(x, y, z));
                }
                if (HasNormal)
                {
                    float x, y, z;
                    float.TryParse(vertexProperties[normalIndex[0]], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                    float.TryParse(vertexProperties[normalIndex[1]], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                    float.TryParse(vertexProperties[normalIndex[2]], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
                    normals.Add(new Vector3(x, y, z));
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
            vertices = HasPosition ? new List<Vector3>() : null;
            normals = HasNormal ? new List<Vector3>() : null;
            colors = HasColor ? new List<Color32>() : null;

            int bytesPerVertex = 0;
            foreach (var keyvalue in DictProperties)
            {
                bytesPerVertex += keyvalue.Value.ValueDataBytes;
            }
            bytesUsed = bytesPerVertex * NElement;
            for (int i = 0; i < NElement; ++i)
            {
                int byteIndex = i * bytesPerVertex;

                if (HasPosition)
                {
                    PlyMultiProperty xmultiProperty = (DictProperties["x"] as PlyMultiProperty);
                    PlyMultiProperty ymultiProperty = (DictProperties["y"] as PlyMultiProperty);
                    PlyMultiProperty zmultiProperty = (DictProperties["z"] as PlyMultiProperty);
                    var x = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + xmultiProperty.BytesOffset, xmultiProperty.ValueDataBytes), 0);
                    var y = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + ymultiProperty.BytesOffset, ymultiProperty.ValueDataBytes), 0);
                    var z = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + zmultiProperty.BytesOffset, zmultiProperty.ValueDataBytes), 0);
                    vertices.Add(new Vector3(x, y, z));
                }

                if (HasNormal)
                {
                    PlyMultiProperty xmultiProperty = (DictProperties["nx"] as PlyMultiProperty);
                    PlyMultiProperty ymultiProperty = (DictProperties["ny"] as PlyMultiProperty);
                    PlyMultiProperty zmultiProperty = (DictProperties["nz"] as PlyMultiProperty);
                    var x = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + xmultiProperty.BytesOffset, xmultiProperty.ValueDataBytes), 0);
                    var y = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + ymultiProperty.BytesOffset, ymultiProperty.ValueDataBytes), 0);
                    var z = System.BitConverter.ToSingle(PlyElement.GetBytesSubarray(bytes, byteIndex + zmultiProperty.BytesOffset, zmultiProperty.ValueDataBytes), 0);
                    normals.Add(new Vector3(x, y, z));
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
        public PlyFaceElement(IList<string> headerUnparsed) : base("face")
        {
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            IsValid = IsValid && IsElementValid();
        }

        public void ParseElement(IList<string> faceContents, out List<int> triangles)
        {
            triangles = new List<int>();

            foreach (var faceContent in faceContents)
            {
                var split = faceContent.Split(' ');
                var count = Convert.ToInt32(split.First());
                if (count != 3)
                {
                    Debug.LogWarning("Warning: Found a face is not a triangle face, skipping...");
                    triangles = null;
                    return;
                }

                triangles.AddRange(split.ToList().GetRange(1, 3).Select(x => Convert.ToInt32(x)).ToList());
            }
        }

        public void ParseElementBinaryLittleEndian(byte[] bytes, int bytesOffset, 
            out int bytesUsed, out List<int> triangles)
        {
            var listProperty = DictProperties["vertex_indices"] as PlyListProperty;
            bytesUsed = PlyProperty.GetDataBytes(listProperty.CountNumFormat)+ NElement * 3 * PlyProperty.GetDataBytes(listProperty.ValueNumFormat);
            
            triangles = new List<int>();

            int facesRead = 0;
            int bytesRead = 0;
            int bytesPerTriangleIndex = 4;

            while (facesRead < NElement)
            {
                var faceIndex = bytesOffset + bytesRead;
                var indexCount = bytes[faceIndex];
                if (indexCount == 3)
                {
                    for (int i=0; i<indexCount; ++i)
                    {
                        triangles.Add(System.BitConverter.ToInt32(PlyElement.GetBytesSubarray(bytes, faceIndex + 1 + i * bytesPerTriangleIndex, bytesPerTriangleIndex), 0));
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

            return DictProperties.Count == 1 && isValid;
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

    public class PlyGlobalNameElement : PlyElement
    {
        public PlyGlobalNameElement(IList<string> headerUnparsed) : base("g_name", true)
        {
            if (!ParseElementInfo(headerUnparsed))
            {
                IsValid = false;
                return;
            }

            IsValid = IsValid && IsElementValid();
        }

        public string GetName()
        {
            if (!IsValid)
            {
                return "";
            }

            return (DictProperties["name"] as PlyMultiProperty).Value;
        }

        private bool IsElementValid()
        {
            return DictProperties.ContainsKey("name") && DictProperties.Count == 1 && CheckValueDataTypeEqual(new List<string> { "name" }, PlyProperty.ENumFormat.nf_string);
        }
    }

    public class PlyHeader
    {
        public PlyFormat Format;
        public PlyVertexElement VertexElement;
        public PlyFaceElement FaceElement;
        public PlyGlobalMaterialElement GloablMaterialElement;
        public PlyGlobalNameElement GlobalNameElement;

        public List<string> RawHeader;

        public PlyHeader(List<string> headerUnparsed)
        {
            Format = GetFormat(headerUnparsed.FirstOrDefault(x => x.Contains("format")).Split(' ')[1]);
            VertexElement = new PlyVertexElement(headerUnparsed);
            FaceElement = new PlyFaceElement(headerUnparsed);
            GloablMaterialElement = new PlyGlobalMaterialElement(headerUnparsed);
            GlobalNameElement = new PlyGlobalNameElement(headerUnparsed);
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