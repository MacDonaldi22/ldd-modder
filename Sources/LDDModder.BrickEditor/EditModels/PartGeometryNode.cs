﻿using LDDModder.BrickEditor.Resources;
using LDDModder.Modding.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.BrickEditor.EditModels
{
    public class PartGeometryNode : ProjectComponentNode<ModelMesh>
    {
        public ModelMesh Geometry => Component;

        public PartGeometryNode(ModelMesh geometry, bool isReplacement = false) : base(geometry)
        {
            if (isReplacement)
            {
                var allGeoms = (geometry.Parent as FemaleStudModel).ReplacementGeometries;
                int myIndex = allGeoms.IndexOf(geometry);
                if (allGeoms.Count > 1)
                    Name = $"{ModelLocalizations.Label_ReplacementMesh} {myIndex + 1}"; 
                else
                    Name = ModelLocalizations.Label_ReplacementMesh;
            }
            else
            {
                var allGeoms = (geometry.Parent as SurfaceComponent).Geometries;
                int myIndex = allGeoms.IndexOf(geometry);
                if (allGeoms.Count > 1)
                    Name = $"{ModelLocalizations.Label_Mesh} {myIndex + 1}";
                else
                    Name = ModelLocalizations.Label_Mesh;
            }
        }
    }
}