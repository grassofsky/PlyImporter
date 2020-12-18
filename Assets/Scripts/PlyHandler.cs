using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Globalization;
using System.Text;

namespace ThreeDeeBear.Models.Ply
{
    public class PlyResult
    {
        public IList<Vector3> vertices = null;
        public IList<Color32> colors = null;
        public IList<Vector3> normals = null;
        public List<int> triangles = null;
        public List<List<int>> lines;
        public List<Color32> lineColors;

        public string meshName;
        public Color meshColor;
    }

    public static class PlyHandler
    {
        public static string GetOneLine(byte[] content, ref int start)
        {
            StringBuilder builder = new StringBuilder("");
            byte cur;
            for (; ;)
            {
                cur = content[start];
                start = start + 1;
                if (cur == '\n')
                {
                    break;
                }
                builder.Append((char)cur);
            }
            return builder.ToString();
        }

        public static List<string> GetHeader(byte[] content)
        {
            List<string> header = new List<string>();
            int iOffset = 0;
            string curString = "";
            while (curString.IndexOf("end_header") == -1)
            {
                curString = GetOneLine(content, ref iOffset);
                header.Add(curString);
            }

            return header;
        }

        public static List<string> GetLines(byte[] content)
        {
            List<string> lines = new List<string>();
            int iOffset = 0;
            int allBytes = content.Length;
            while (iOffset < allBytes)
            {
                lines.Add(GetOneLine(content, ref iOffset));
            }

            return lines;
        }

        public static PlyResult GetResult(byte[] content)
        {
            PlyResult plyResult = null;
            var header = GetHeader(content);
            var headerParsed = new PlyHeader(header);
            if (!headerParsed.VertexElement.IsValid ||
                (!headerParsed.FaceElement.IsValid && !headerParsed.LineElement.IsValid))
            {
                return null;
            }

            if (headerParsed.Format == PlyFormat.Ascii)
            {
                plyResult = ParseAscii(GetLines(content), headerParsed);
            }
            else if (headerParsed.Format == PlyFormat.BinaryLittleEndian)
            {
                plyResult = ParseBinaryLittleEndian(content, headerParsed);
            }
            else // todo: support BinaryBigEndian
            {
                return null;
            }

            return plyResult; // new PlyResult(mesh, headerParsed.GloablMaterialElement.GetColor());
        }

        #region Ascii

        private static PlyResult ParseAscii(List<string> plyFile, PlyHeader header)
        {
            PlyResult plyResult = new PlyResult();
            plyResult.meshName = header.GlobalMeshInfoElement.GetName();
            plyResult.meshColor = header.GloablMaterialElement.GetColor();

            // TODO: order independent
            var headerEndIndex = plyFile.IndexOf("end_header");
            var vertexStartIndex = headerEndIndex + 1;
            var faceStartIndex = vertexStartIndex + header.VertexElement.NElement;
            var lineStartIndex = faceStartIndex + header.FaceElement.NElement;
            var lineColorStartIndex = lineStartIndex + header.LineElement.NElement;

            header.VertexElement.ParseElement(plyFile.GetRange(vertexStartIndex, header.VertexElement.NElement),
                out plyResult.vertices, out plyResult.normals, out plyResult.colors);
            header.FaceElement.ParseElement(plyFile.GetRange(faceStartIndex, header.FaceElement.NElement), out plyResult.triangles);
            header.LineElement.ParseElement(plyFile.GetRange(lineStartIndex, header.LineElement.NElement), out plyResult.lines);
            header.LineColorElement.ParseElement(plyFile.GetRange(lineColorStartIndex, header.LineColorElement.NElement), out plyResult.lineColors);

            return plyResult;
        }

        #endregion

        #region Binary

        private static PlyResult ParseBinaryLittleEndian(byte[] content, PlyHeader header)
        {
            PlyResult plyResult = new PlyResult();
            plyResult.meshName = header.GlobalMeshInfoElement.GetName();
            plyResult.meshColor = header.GloablMaterialElement.GetColor();

            var headerAsText = header.RawHeader.Aggregate((a, b) => $"{a}\n{b}") + "\n";
            var headerAsBytes = Encoding.ASCII.GetBytes(headerAsText);
            var withoutHeader = content.Skip(headerAsBytes.Length).ToArray();

            int bytesUsed;
            header.VertexElement.ParseElementBinaryLittleEndian(withoutHeader, out bytesUsed, out plyResult.vertices, out plyResult.normals, out plyResult.colors);

            int bytesOffset = bytesUsed;
            header.FaceElement.ParseElementBinaryLittleEndian(withoutHeader, bytesOffset, out bytesUsed, out plyResult.triangles);

            bytesOffset = bytesOffset + bytesUsed;
            header.LineElement.ParseElementBinaryLittleEndian(withoutHeader, bytesOffset, out bytesUsed, out plyResult.lines);

            bytesOffset = bytesOffset + bytesUsed;
            header.LineColorElement.ParseElementBinaryLittleEndian(withoutHeader, bytesOffset, out bytesUsed, out plyResult.lineColors);

            return plyResult;
        }

        #endregion

    }
}