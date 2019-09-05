﻿using ObjectTK.Buffers;
using ObjectTK.Shaders.Variables;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.BrickEditor.Rendering
{
    public abstract class GLMeshBase
    {
        public abstract void Draw();

        public abstract void AssignShaderValues();

        public static GLMeshBase CreateFromGeometry(LDD.Meshes.MeshGeometry geometry)
        {
            int[] triIndices = geometry.GetTriangleIndices();

            if (geometry.IsTextured)
            {
                
                var verts = new List<VertVNT>();
                foreach (var v in geometry.Vertices)
                {
                    verts.Add(new VertVNT()
                    {
                        Position = v.Position.ToGL(),
                        Normal = v.Normal.ToGL(),
                        TexCoord = v.TexCoord.ToGL()
                    });
                }

                var mesh = new GLTexturedMesh();
                mesh.UpdateVertices(triIndices, verts);

                return mesh;
            }
            else
            {
                var verts = new List<VertVN>();
                foreach (var v in geometry.Vertices)
                {
                    verts.Add(new VertVN()
                    {
                        Position = v.Position.ToGL(),
                        Normal = v.Normal.ToGL()
                    });
                }

                var mesh = new GLSimpleMesh();
                mesh.UpdateVertices(triIndices, verts);

                return mesh;
            }
        }

        public abstract void BindToProgram(ObjectTK.Shaders.Program program);
    }

    public abstract class GLMeshBase<VT> : GLMeshBase, IDisposable where VT : struct
    {
        public VertexArray Vao { get; protected set; }

        public Buffer<int> IndexBuffer { get; protected set; }

        public Buffer<VT> VertexBuffer { get; private set; }

        public Matrix4 Transform { get; set; }

        public Color4 MaterialColor { get; set; }

        public bool Disposed { get; private set; }

        public ObjectTK.Shaders.Program BoundProgram { get; protected set; }

        protected List<Tuple<VertexAttrib,int>> BoundAttributes = new List<Tuple<VertexAttrib, int>> ();

        public GLMeshBase()
        {
            Vao = new VertexArray();
        }

        ~GLMeshBase()
        {
            if (!Disposed)
                Dispose();
        }

        protected void DisposeBuffers()
        {
            if (Vao != null)
                Vao.Bind();

            if (IndexBuffer != null)
            {
                if (Vao != null)
                    Vao.UnbindElementBuffer();

                IndexBuffer.Dispose();
                IndexBuffer = null;
            }

            if (VertexBuffer != null)
            {
                if (Vao != null && BoundAttributes != null)
                    BoundAttributes.ForEach(a => Vao.UnbindAttribute(a.Item1));

                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
        }

        public void UpdateVertices(IEnumerable<int> indices, IEnumerable<VT> vertices)
        {
            DisposeBuffers();

            VertexBuffer = new Buffer<VT>();
            VertexBuffer.Init(BufferTarget.ArrayBuffer, vertices.ToArray());

            IndexBuffer = new Buffer<int>();
            IndexBuffer.Init(BufferTarget.ElementArrayBuffer, indices.ToArray());

            if (BoundProgram != null)
            {
                foreach(var vAttr in BoundAttributes)
                    Vao.BindAttribute(vAttr.Item1, VertexBuffer, vAttr.Item2);

                Vao.BindElementBuffer(IndexBuffer);
            }
        }

        protected void UnbindProgram()
        {
            if (Vao != null && BoundAttributes != null)
            {
                Vao.Bind();
                BoundAttributes.ForEach(a => Vao.UnbindAttribute(a.Item1));
            }

            BoundProgram = null;
            BoundAttributes.Clear();
        }

        public void BindVertexAttribute(VertexAttrib attrib, int offset = 0)
        {
            BoundAttributes.Add(new Tuple<VertexAttrib, int>(attrib, offset));
            if (VertexBuffer != null && Vao != null)
                Vao.BindAttribute(attrib, VertexBuffer, offset);
        }

        public override void BindToProgram(ObjectTK.Shaders.Program program)
        {
            if (BoundProgram != null)
                UnbindProgram();
            
            BoundProgram = program;

            Vao.Bind();
            BindShaderAttributes(program);
            Vao.BindElementBuffer(IndexBuffer);
        }

        protected virtual void BindShaderAttributes(ObjectTK.Shaders.Program program)
        {

        }

        public void Dispose()
        {
            DisposeBuffers();

            if (Vao != null)
            {
                Vao.Dispose();
                Vao = null;
            }

            Disposed = true;
            GC.SuppressFinalize(this);
        }

        public override void AssignShaderValues()
        {
            SetProgramUniforms();
        }

        protected virtual void SetProgramUniforms()
        {
            if (BoundProgram is IMeshShaderProgram program)
            {
                program.ModelMatrix.Set(Transform);
                var v = new Vector4(MaterialColor.R, MaterialColor.G, MaterialColor.B, MaterialColor.A);
                program.MaterialColor.Set(v);
            }
        }

        protected virtual void OnDraw()
        {

        }

        public override void Draw()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            Vao.Bind();

            OnDraw();

            Vao.DrawElements(PrimitiveType.Triangles, IndexBuffer.ElementCount);
        }
    }
}
