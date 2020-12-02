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
        public Mesh MeshResult;
        public Color MeshColor;


        public PlyResult(Mesh mesh, Color color)
        {
            MeshResult = mesh;
            MeshColor = color;
        }
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
            Mesh mesh = null;
            var header = GetHeader(content);
            var headerParsed = new PlyHeader(header);
            if (!headerParsed.VertexElement.IsValid ||
                !headerParsed.FaceElement.IsValid)
            {
                return null;
            }

            if (headerParsed.Format == PlyFormat.Ascii)
            {
                mesh = ParseAscii(GetLines(content), headerParsed);
            }
            else if (headerParsed.Format == PlyFormat.BinaryLittleEndian)
            {
                mesh = ParseBinaryLittleEndian(content, headerParsed);
            }
            else // todo: support BinaryBigEndian
            {
                return null;
            }

            mesh.name = headerParsed.GlobalMeshInfoElement.GetName();
            return new PlyResult(mesh, headerParsed.GloablMaterialElement.GetColor());
        }

        #region Ascii

        private static Mesh ParseAscii(List<string> plyFile, PlyHeader header)
        {
            Mesh mesh = new Mesh();

            // TODO: order independent
            var headerEndIndex = plyFile.IndexOf("end_header");
            var vertexStartIndex = headerEndIndex + 1;
            var faceStartIndex = vertexStartIndex + header.VertexElement.NElement;

            IList<Vector3> vertices;
            IList<Color32> colors;
            IList<Vector3> normals;

            header.VertexElement.ParseElement(plyFile.GetRange(vertexStartIndex, header.VertexElement.NElement), out vertices, out normals, out colors);

            List<int> triangles;
            header.FaceElement.ParseElement(plyFile.GetRange(faceStartIndex, header.FaceElement.NElement), out triangles);

            mesh.vertices = vertices.ToArray();
            if (normals != null) mesh.normals = normals.ToArray();
            if (colors != null) mesh.SetColors(colors.ToArray());
            if (triangles != null) mesh.triangles = triangles.ToArray();

            return mesh;
        }

        #endregion

        #region Binary

        private static Mesh ParseBinaryLittleEndian(byte[] content, PlyHeader header)
        {
            Mesh mesh = new Mesh();

            var headerAsText = header.RawHeader.Aggregate((a, b) => $"{a}\n{b}") + "\n";
            var headerAsBytes = Encoding.ASCII.GetBytes(headerAsText);
            var withoutHeader = content.Skip(headerAsBytes.Length).ToArray();

            IList<Vector3> vertices;
            IList<Color32> colors;
            IList<Vector3> normals;
            int bytesUsed;
            header.VertexElement.ParseElementBinaryLittleEndian(withoutHeader, out bytesUsed, out vertices, out normals, out colors);

            List<int> triangles;
            int bytesOffset = bytesUsed;
            header.FaceElement.ParseElementBinaryLittleEndian(withoutHeader, bytesOffset, out bytesUsed, out triangles);

            mesh.vertices = vertices.ToArray();
            if (normals != null) mesh.normals = normals.ToArray();
            if (colors != null) mesh.SetColors(colors.ToArray());
            if (triangles != null) mesh.triangles = triangles.ToArray();
            return mesh;
        }

        #endregion

    }
}