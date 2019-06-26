﻿using LDDModder.Simple3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.LDD.Meshes
{
    public class Vertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector2 TexCoord { get; set; }

        public List<BoneWeight> BoneWeights { get; }

        public Vertex()
        {
            BoneWeights = new List<BoneWeight>();
        }

        public Vertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
            TexCoord = Vector2.Empty;
            BoneWeights = new List<BoneWeight>();
        }

        public Vertex(float px, float py, float pz, float nx, float ny, float nz)
        {
            Position = new Vector3(px, py, pz);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = Vector2.Empty;
            BoneWeights = new List<BoneWeight>();
        }

        public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord) : this(position, normal)
        {
            TexCoord = texCoord;
            BoneWeights = new List<BoneWeight>();
        }

        //public override bool Equals(object obj)
        //{
        //    return obj is Vertex vertex &&
        //           Position.Equals(vertex.Position) &&
        //           Normal.Equals(vertex.Normal) &&
        //           TexCoord.Equals(vertex.TexCoord);
        //}

        public bool Equals(Vertex vertex)
        {
            return Position.Equals(vertex.Position) &&
                   Normal.Equals(vertex.Normal) &&
                   TexCoord.Equals(vertex.TexCoord);
        }

        //public override int GetHashCode()
        //{
        //    var hashCode = 550714527;
        //    hashCode = hashCode * -1521134295 + Position.GetHashCode();
        //    hashCode = hashCode * -1521134295 + Normal.GetHashCode();
        //    hashCode = hashCode * -1521134295 + TexCoord.GetHashCode();
        //    return hashCode;
        //}
    }
}
