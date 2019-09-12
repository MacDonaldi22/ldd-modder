﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LDDModder.LDD.Files;
using LDDModder.LDD.Meshes;
using LDDModder.LDD.Primitives;
using LDDModder.Simple3D;

namespace LDDModder.LDD.Data
{
    public class PartWrapper
    {
        public Primitives.Primitive Primitive { get; set; }
        public List<PartSurfaceMesh> Surfaces { get; set; }

        public PartSurfaceMesh MainSurface => Surfaces.FirstOrDefault(x => x.SurfaceID == 0);

        public IEnumerable<PartSurfaceMesh> DecorationSurfaces => Surfaces.Where(x => x.SurfaceID > 0);

        public Files.MeshFile MainMesh => MainSurface?.Mesh;

        public IEnumerable<Files.MeshFile> DecorationMeshes => DecorationSurfaces.Select(x => x.Mesh);

        public IEnumerable<Files.MeshFile> AllMeshes => Surfaces.Select(x => x.Mesh);

        public PartWrapper()
        {
            Surfaces = new List<PartSurfaceMesh>();
        }

        public PartWrapper(Primitive primitive, IEnumerable<PartSurfaceMesh> surfaces)
        {
            Primitive = primitive;
            Surfaces = new List<PartSurfaceMesh>(surfaces);
        }

        #region Shader Data Generation

        public void ComputeAverageNormals()
        {
            ShaderDataGenerator.ComputeAverageNormals(AllMeshes.SelectMany(x => x.Triangles));
        }

        public void ComputeEdgeOutlines()
        {
            ShaderDataGenerator.ComputeEdgeOutlines(AllMeshes.SelectMany(x => x.Triangles));
        }

        #endregion

        #region Loading

        public static PartWrapper Read(LDDEnvironment environment, int partID)
        {
            if (environment.DatabaseExtracted)
            {
                var primitivesDir = Path.Combine(environment.ApplicationDataPath, "db\\Primitives");
                var meshesDir = Path.Combine(environment.ApplicationDataPath, "db\\Primitives\\LOD0");

                var primitiveFile = Path.Combine(primitivesDir, $"{partID}.xml");
                if (!File.Exists(primitiveFile))
                    throw new FileNotFoundException($"Primitive file not found. ({partID}.xml)");

                var surfaces = new List<PartSurfaceMesh>();
                foreach (string meshPath in Directory.GetFiles(meshesDir, $"{partID}.g*"))
                {
                    string surfIdStr = Path.GetExtension(meshPath).TrimStart('.').Replace('g', '0');
                    if (int.TryParse(surfIdStr, out int surfID))
                        surfaces.Add(new PartSurfaceMesh(partID, surfID, MeshFile.Read(meshPath)));
                }

                if (!surfaces.Any())
                    throw new FileNotFoundException($"Mesh file not found. ({partID}.g)");

                return new PartWrapper(Primitive.Load(primitiveFile), surfaces);
            }
            else
            {
                using (var lif = LifFile.Open(Path.Combine(environment.ApplicationDataPath, "db.lif")))
                {
                    var primitiveFolder = lif.GetFolder("Primitives");

                }
            }
            return null;
        }


        #endregion
    }
}
