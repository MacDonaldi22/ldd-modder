﻿using LDDModder.BrickEditor.ProjectHandling;
using LDDModder.BrickEditor.Resources;
using LDDModder.Modding.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.BrickEditor.Models.Navigation
{
    public class ProjectElementNode : BaseProjectNode
    {
        public override PartProject Project => Element.Project;

        public PartElement Element { get; set; }

        public Type ElementType => Element?.GetElementType();

        public ProjectElementNode(PartElement element)
        {
            Element = element;
            NodeID = element.ID;
            if (string.IsNullOrEmpty(NodeID))
                NodeID = element.GetHashCode().ToString();
        }

        public ProjectElementNode(PartElement element, string text)
        {
            Element = element;
            NodeID = element.ID;
            if (string.IsNullOrEmpty(NodeID))
                NodeID = element.GetHashCode().ToString();
            Text = text;
        }

        public override void RebuildChildrens()
        {
            base.RebuildChildrens();
            Childrens.Clear();

            if (Element is PartSurface surface)
            {
                foreach (var elemGroup in surface.Components.GroupBy(x => x.ComponentType))
                {
                    string groupTitle = ModelLocalizations.ResourceManager.GetString($"Label_{elemGroup.Key}Components");
                    
                    int itemCount = elemGroup.Count();
                    int groupSize = 10;
                    if (itemCount >= 50)
                        groupSize = 20;
                    else if (itemCount >= 100)
                        groupSize = 50;
                    AutoGroupElements(elemGroup, groupTitle, 5, groupSize);
                }
            }
            else if (Element is SurfaceComponent surfaceComponent)
            {
                if (surfaceComponent is FemaleStudModel femaleStud/* && 
                    femaleStud.ReplacementMeshes.Any()*/)
                {
                    Childrens.Add(new ElementCollectionNode(femaleStud, 
                        femaleStud.Meshes, ModelLocalizations.Label_DefaultMeshes));

                    Childrens.Add(new ElementCollectionNode(femaleStud,
                        femaleStud.ReplacementMeshes, ModelLocalizations.Label_AlternateMeshes));
                }
                else
                {
                    AutoGroupElements(surfaceComponent.Meshes,
                        ModelLocalizations.Label_Models, 10, 10, true);
                }
            }
            else
            {

                foreach (var elemCollection in Element.ElementCollections)
                {
                    if (elemCollection.ElementType == typeof(StudReference))
                        continue;

                    AutoGroupElements(elemCollection.GetElements(), "Items", 5, 10);
                }

                foreach (var childElem in Element.ChildElements)
                {
                    if (childElem is StudReference)
                        continue;
                    Childrens.Add(CreateDefault(childElem));
                }
            }
        }

        public static ProjectElementNode CreateDefault(PartElement element)
        {
            var node = new ProjectElementNode(element);

            if (element is PartSurface surface)
            {
                if (surface.SurfaceID == 0)
                {
                    node.Text = ModelLocalizations.Label_MainSurface;

                    node.ImageKey = "Surface_Main";
                }
                else
                {
                    node.Text = string.Format(ModelLocalizations.Label_DecorationSurfaceNumber, surface.SurfaceID);
                    node.ImageKey = "Surface_Decoration";
                }
            }
            else
            {
                node.Text = element.Name;
                if (element is SurfaceComponent component)
                    node.ImageKey = $"Model_{component.ComponentType}";
                else if (element is PartConnection connection)
                    node.ImageKey = $"Connection_{connection.ConnectorType}";
                else if (element is PartCollision collision)
                    node.ImageKey = $"Collision_{collision.CollisionType}";
                else if (element is ModelMeshReference || element is ModelMesh)
                    node.ImageKey = "Mesh";
            }
            return node;
        }

        public override bool CanDragDrop()
        {
            if (Element is ModelMeshReference)
                return true;
            return base.CanDragDrop();
        }

        public override bool CanDropOn(BaseProjectNode node)
        {
            if (ElementType == typeof(ModelMeshReference))
            {
                if (node is ProjectElementNode elementNode)
                {
                    if (elementNode.Element is SurfaceComponent)
                        return true;
                }
                else if (node is ElementCollectionNode collectionNode)
                {
                    if (collectionNode.CollectionType == typeof(ModelMeshReference))
                        return true;
                }
            }

            return base.CanDropOn(node);
        }

        public override bool CanDropBefore(BaseProjectNode node)
        {
            if (ElementType == typeof(ModelMeshReference))
            {
                if (node is ProjectElementNode elementNode)
                {
                    if (elementNode.Element is ModelMeshReference)
                        return true;
                }
            }
            return base.CanDropBefore(node);
        }

        public override bool CanDropAfter(BaseProjectNode node)
        {
            if (ElementType == typeof(ModelMeshReference))
            {
                if (node is ProjectElementNode elementNode)
                {
                    if (elementNode.Element is ModelMeshReference)
                        return true;
                }
            }
            return base.CanDropAfter(node);
        }

        public override bool CanToggleVisibility()
        {
            return true;
        }

        public override bool GetIsVisible()
        {
            var modelExt = Element.GetExtension<ModelElementExtension>();
            if (modelExt != null)
                return modelExt.IsVisible;
            return base.GetIsVisible();
        }
    }
}