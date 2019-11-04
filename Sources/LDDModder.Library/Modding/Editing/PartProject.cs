﻿using ICSharpCode.SharpZipLib.Zip;
using LDDModder.LDD.Data;
using LDDModder.LDD.Meshes;
using LDDModder.LDD.Primitives;
using LDDModder.LDD.Primitives.Connectors;
using LDDModder.Serialization;
using LDDModder.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LDDModder.Modding.Editing
{
    [XmlRoot("LDDPart")]
    public class PartProject
    {
        public const string ProjectFileName = "project.xml";

        #region Part Info

        public int PartID { get; private set; }

        public string PartDescription { get; set; }

        public string Comments { get; set; }

        public List<int> Aliases { get; set; }

        public Platform Platform { get; set; }

        public MainGroup MainGroup { get; set; }

        public PhysicsAttributes PhysicsAttributes { get; set; }

        public BoundingBox Bounding { get; set; }

        public BoundingBox GeometryBounding { get; set; }

        public ItemTransform DefaultOrientation { get; set; }

        public Camera DefaultCamera { get; set; }

        public VersionInfo PrimitiveFileVersion { get; set; }

        public int PartVersion { get; set; }
 
        public bool Decorated { get; set; }

        public bool Flexible { get; set; }

        #endregion


        public string ProjectPath { get; set; }

        public string ProjectWorkingDir { get; set; }

        [XmlArray("ModelSurfaces")]
        public ElementCollection<PartSurface> Surfaces { get; }

        [XmlArray("Connections")]
        public ElementCollection<PartConnection> Connections { get; }

        [XmlArray("Collisions")]
        public ElementCollection<PartCollision> Collisions { get; }

        [XmlArray("Bones")]
        public ElementCollection<PartBone> Bones { get; }
        
        [XmlIgnore]
        public ElementCollection<ModelMesh> UnassignedMeshes { get; }
        
        [XmlIgnore]
        public bool IsLoading { get; internal set; }

        public event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;

        public event EventHandler<CollectionChangedEventArgs> ElementCollectionChanged;

        public PartProject()
        {
            Surfaces = new ElementCollection<PartSurface>(this);
            Connections = new ElementCollection<PartConnection>(this);
            Collisions = new ElementCollection<PartCollision>(this);
            Bones = new ElementCollection<PartBone>(this);
            UnassignedMeshes = new ElementCollection<ModelMesh>(this);

            PrimitiveFileVersion = new VersionInfo(1, 0);
            Aliases = new List<int>();
            PartVersion = 1;
        }

        public void ValidatePart()
        {
            //TODO

            if (Decorated != Surfaces.Any(x => x.SurfaceID > 0))
            {
                //Marked as having decorations but does not have any decoration surfaces
            }

            foreach (var surf in Surfaces.Where(x => x.SurfaceID > 0))
            {
                var allMeshes = surf.Components.SelectMany(c => c.GetAllMeshes());
                if (!allMeshes.All(m => m.IsTextured))
                {
                    //The decoration surface has meshes that does not have UV mapping
                }
            }
        }

        #region Creation From LDD

        public static PartProject CreateFromLddPart(int partID)
        {
            return CreateFromLddPart(LDD.LDDEnvironment.Current, partID);
        }

        public static PartProject CreateFromLddPart(LDD.LDDEnvironment environment, int partID)
        {
            var lddPart = LDD.Parts.PartWrapper.LoadPart(environment, partID);

            var project = new PartProject()
            {
                IsLoading = true
            };
            project.SetBaseInfo(lddPart);

            foreach (var collision in lddPart.Primitive.Collisions)
                project.Collisions.Add(PartCollision.FromLDD(collision));

            foreach (var lddConn in lddPart.Primitive.Connectors)
            {
                var partConn = PartConnection.FromLDD(lddConn);
                //if (partConn.ConnectorType == ConnectorType.Custom2DField)
                //{
                //    int connIdx = lddPart.Primitive.Connectors.IndexOf(lddConn);
                //    partConn.ID = StringUtils.GenerateUUID($"{partID}_{connIdx}", 8);
                //}
                project.Connections.Add(partConn);
            }

            foreach (var flexBone in lddPart.Primitive.FlexBones)
                project.Bones.Add(PartBone.FromLDD(flexBone));

            foreach (var meshSurf in lddPart.Surfaces)
            {
                var partSurf = new PartSurface(
                    meshSurf.SurfaceID,
                    lddPart.Primitive.GetSurfaceMaterialIndex(meshSurf.SurfaceID)
                );

                project.Surfaces.Add(partSurf);

                foreach (var culling in meshSurf.Mesh.Cullings)
                {
                    var cullingComp = SurfaceComponent.FromLDD(meshSurf.Mesh, culling);
                    partSurf.Components.Add(cullingComp);
                }
            }

            project.GenerateElementsIDs(true);
            project.GenerateElementsNames();
            project.LinkStudReferences();

            project.IsLoading = false;
            return project;
        }


        private void SetBaseInfo(LDD.Parts.PartWrapper lddPart)
        {
            PartID = lddPart.PartID;
            PartDescription = lddPart.Primitive.Name;
            PartVersion = lddPart.Primitive.PartVersion;
            Decorated = lddPart.IsDecorated;
            Flexible = lddPart.IsFlexible;
            Aliases = lddPart.Primitive.Aliases;
            Platform = lddPart.Primitive.Platform;
            MainGroup = lddPart.Primitive.MainGroup;
            PhysicsAttributes = lddPart.Primitive.PhysicsAttributes;
            GeometryBounding = lddPart.Primitive.GeometryBounding;
            Bounding = lddPart.Primitive.Bounding;
            if (lddPart.Primitive.DefaultOrientation != null)
                DefaultOrientation = ItemTransform.FromLDD(lddPart.Primitive.DefaultOrientation);
        }

        #endregion


        public static PartProject CreateEmptyProject()
        {
            var project = new PartProject();
            //project.IsLoading = true;
            project.Surfaces.Add(new PartSurface(0, 0));
            //project.IsLoading = false;
            return project;
        }

        #region Read/Write Xml structure

        public XDocument GenerateProjectXml()
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(PartProject));
            //var doc = new XDocument();
            //var ns = new XmlSerializerNamespaces();
            //ns.Add("", "");
            //using (var docWriter = doc.CreateWriter())
            //    serializer.Serialize(docWriter, this, ns);
            //return doc;

            var doc = new XDocument(new XElement("LDDPART"));

            //Part Info
            {
                var propsElem = doc.Root.AddElement("Properties");
                propsElem.Add(new XElement("PartID", PartID));

                if (Aliases.Where(x => x != PartID).Any())
                    propsElem.Add(new XElement("Aliases", string.Join(";", Aliases.Where(x => x != PartID))));

                propsElem.Add(new XElement("Description", PartDescription));

                propsElem.Add(new XElement("PartVersion", PartVersion));

                if (PrimitiveFileVersion != null)
                    propsElem.Add(PrimitiveFileVersion.ToXmlElement("PrimitiveVersion"));

                propsElem.Add(new XElement("Flexible", Flexible));

                propsElem.Add(new XElement("Decorated", Decorated));

                if (Platform != null)
                    propsElem.AddElement("Platform", new XAttribute("ID", Platform.ID), new XAttribute("Name", Platform.Name));

                if (MainGroup != null)
                    propsElem.AddElement("MainGroup", new XAttribute("ID", MainGroup.ID), new XAttribute("Name", MainGroup.Name));

                if (PhysicsAttributes != null)
                    propsElem.Add(PhysicsAttributes.SerializeToXml());

                if (Bounding != null)
                    propsElem.Add(XmlHelper.DefaultSerialize(Bounding, "Bounding"));

                if (GeometryBounding != null)
                    propsElem.Add(XmlHelper.DefaultSerialize(GeometryBounding, "GeometryBounding"));

                if (DefaultOrientation != null)
                    propsElem.Add(DefaultOrientation.SerializeToXml("DefaultOrientation"));

                if (DefaultCamera != null)
                    propsElem.Add(XmlHelper.DefaultSerialize(DefaultCamera, "DefaultCamera"));

                if (!string.IsNullOrEmpty(Comments))
                    propsElem.Add(new XElement("Comments", Comments));
            }

            var surfacesElem = doc.Root.AddElement("ModelSurfaces");
            foreach (var surf in Surfaces)
                surfacesElem.Add(surf.SerializeToXml());

            var collisionsElem = doc.Root.AddElement("Collisions");
            foreach (var col in Collisions)
                collisionsElem.Add(col.SerializeToXml());

            var connectionsElem = doc.Root.AddElement("Connections");
            foreach (var conn in Connections)
                connectionsElem.Add(conn.SerializeToXml());

            if (Bones.Any())
            {
                var bonesElem = doc.Root.AddElement("Bones");
                foreach (var bone in Bones)
                    bonesElem.Add(bone.SerializeToXml());
            }

            return doc;
        }

        public static PartProject CreateFromXml(XDocument doc)
        {
            var project = new PartProject();
            project.LoadFromXml(doc);
            return project;
        }

        private void LoadFromXml(XDocument doc)
        {
            Surfaces.Clear();
            Connections.Clear();
            Collisions.Clear();
            Bones.Clear();
            Aliases.Clear();

            //Part info
            if (doc.Root.HasElement("Properties", out XElement propsElem))
            {
                PartID = int.Parse(propsElem.Element("PartID")?.Value);

                if (propsElem.HasElement("Aliases", out XElement aliasElem))
                {
                    foreach(string partAlias in aliasElem.Value.Split(';'))
                    {
                        if (int.TryParse(partAlias, out int aliasID))
                            Aliases.Add(aliasID);
                    }
                }

                PartDescription = propsElem.ReadElement("Description", string.Empty);
                PartVersion = propsElem.ReadElement("PartVersion", 1);
                
                if (propsElem.HasElement("PhysicsAttributes", out XElement pA))
                {
                    PhysicsAttributes = new PhysicsAttributes();
                    PhysicsAttributes.LoadFromXml(pA);
                }
                
                if (propsElem.HasElement("GeometryBounding", out XElement gb))
                    GeometryBounding = XmlHelper.DefaultDeserialize<BoundingBox>(gb);

                if (propsElem.HasElement("Bounding", out XElement bb))
                    Bounding = XmlHelper.DefaultDeserialize<BoundingBox>(bb);

                if (propsElem.HasElement("DefaultOrientation", out XElement defori))
                    DefaultOrientation = ItemTransform.FromXml(defori);

                if (propsElem.HasElement("DefaultCamera", out XElement camElem))
                    DefaultCamera = XmlHelper.DefaultDeserialize<Camera>(camElem);
            }

            var surfacesElem = doc.Root.Element("ModelSurfaces");
            if (surfacesElem != null)
            {
                foreach (var surfElem in surfacesElem.Elements(PartSurface.NODE_NAME))
                    Surfaces.Add(PartSurface.FromXml(surfElem));
            }

            var connectionsElem = doc.Root.Element("Connections");
            if (connectionsElem != null)
            {
                foreach (var connElem in connectionsElem.Elements(PartConnection.NODE_NAME))
                    Connections.Add(PartConnection.FromXml(connElem));
            }

            var collisionsElem = doc.Root.Element("Collisions");
            if (collisionsElem != null)
            {
                foreach (var connElem in collisionsElem.Elements(PartCollision.NODE_NAME))
                    Collisions.Add(PartCollision.FromXml(connElem));
            }

            LinkStudReferences();
        }

        #endregion

        #region Read/Write from Directory

        public void SaveExtracted(string directory, bool setWorkingDir = true)
        {
            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            if (setWorkingDir)
                ProjectWorkingDir = directory;
            //LinkStudReferences();
            //GenerateMeshIDs(false);
            GenerateMeshesNames();

            var projectXml = GenerateProjectXml();
            projectXml.Save(Path.Combine(directory, ProjectFileName));

            string meshDir = Path.Combine(directory, "Meshes");
            Directory.CreateDirectory(meshDir);


            var allMeshes = Surfaces.SelectMany(s => s.GetAllMeshes());

            foreach (var mesh in allMeshes)
            {
                string meshPath = Path.Combine(directory, mesh.FileName);
                mesh.Geometry.Save(meshPath);
            }
        }

        public static PartProject LoadFromDirectory(string directory)
        {
            var doc = XDocument.Load(Path.Combine(directory, ProjectFileName));
            var project = new PartProject
            {
                ProjectWorkingDir = directory
            };
            project.LoadFromXml(doc);
            return project;
        }

        #endregion

        #region Read/Write from zip

        public void Save(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Create))
            using (var zipStream = new ZipOutputStream(fs))
            {
                zipStream.SetLevel(1);
                
                zipStream.PutNextEntry(new ZipEntry(ProjectFileName));

                GenerateMeshesNames();
                var projectXml = GenerateProjectXml();

                projectXml.Save(zipStream);

                zipStream.CloseEntry();

                var allMeshes = Surfaces.SelectMany(s => s.GetAllMeshes());

                foreach (var mesh in allMeshes)
                {
                    if (!string.IsNullOrEmpty(mesh.WorkingFilePath) && 
                        File.Exists(mesh.WorkingFilePath))
                    {
                        zipStream.PutNextEntry(new ZipEntry(mesh.FileName));
                        using (var meshFs = File.OpenRead(mesh.WorkingFilePath))
                            meshFs.CopyTo(zipStream);
                    }
                    else if (mesh.IsModelLoaded)
                    {
                        using (var ms = new MemoryStream())
                        {
                            //if (!mesh.IsModelLoaded && mesh.fi)
                            mesh.Geometry.Save(ms);
                            ms.Position = 0;
                            zipStream.PutNextEntry(new ZipEntry(mesh.FileName));
                            ms.CopyTo(zipStream);
                            zipStream.CloseEntry();
                        }
                    }
                }
            }

            ProjectPath = filename;
        }

        public static PartProject ExtractAndOpen(Stream stream, string targetPath)
        {
            using (var zipFile = new ZipFile(stream))
            {
                if (zipFile.GetEntry(ProjectFileName) == null)
                    return null;

                foreach (ZipEntry entry in zipFile)
                {
                    if (!entry.IsFile)
                        continue;
                    string fullPath = Path.Combine(targetPath, entry.Name);
                    string dirName = Path.GetDirectoryName(fullPath);
                    if (dirName.Length > 0)
                        Directory.CreateDirectory(dirName);

                    var buffer = new byte[4096];

                    using (var zipStream = zipFile.GetInputStream(entry))
                    using (Stream fsOutput = File.Create(fullPath))
                    {
                        zipStream.CopyTo(fsOutput, 4096);
                    }
                }
            }

            string projectFilePath = Path.Combine(targetPath, ProjectFileName);

            var projectXml = XDocument.Load(projectFilePath);
            var project = new PartProject()
            {
                //ProjectPath = projectFilePath,
                ProjectWorkingDir = targetPath
            };
            project.LoadFromXml(projectXml);
            return project;
        }

        #endregion

        #region Elements methods

        public IEnumerable<PartElement> GetAllElements()
        {
            IEnumerable<PartElement> elems = Surfaces;
            elems = elems.Concat(Connections)
                .Concat(Collisions)
                .Concat(Bones);

            foreach (var elem in elems)
            {
                yield return elem;
                foreach (var child in elem.GetChildsHierarchy())
                    yield return child;
            }
        }

        public IEnumerable<ModelMesh> GetAllMeshes()
        {
            return GetAllElements().OfType<ModelMesh>();
            //return Surfaces.SelectMany(s => s.GetAllMeshes());
        }

        #endregion

        #region Element ID Handling

        private void GenerateElementsIDs(bool deterministicIDs)
        {
            var allElements = GetAllElements().ToList();
            int elemCount = 0;

            foreach (var elem in allElements)
            {
                string elementID = elem.ID;
                while (string.IsNullOrEmpty(elementID) ||
                    allElements.Any(x => x.ID == elementID && x != elem))
                {
                    if (deterministicIDs)
                        elementID = StringUtils.GenerateUUID($"{PartID}_{elemCount++}", 8);
                    else
                        elementID = StringUtils.GenerateUID(8);
                }
                elem.ID = elementID;
            }
        }

        private void GenerateElementID(PartElement element)
        {
            var existingIDs = GetAllElements().Where(x => x != element && !string.IsNullOrEmpty(x.ID)).Select(x => x.ID);
            string elementID = element.ID;
            while (string.IsNullOrEmpty(elementID) || existingIDs.Contains(elementID))
                elementID = StringUtils.GenerateUID(8);
            element.ID = elementID;
        }

        private void GenerateElementsIDs(IEnumerable<PartElement> elements)
        {
            var existingIDs = GetAllElements()
                .Where(x => !elements.Contains(x) && !string.IsNullOrEmpty(x.ID))
                .Select(x => x.ID)
                .ToList();

            foreach (var elem in elements)
            {
                string elementID = elem.ID;
                while (string.IsNullOrEmpty(elementID) || existingIDs.Contains(elementID))
                    elementID = StringUtils.GenerateUID(8);
                elem.ID = elementID;
                existingIDs.Add(elementID);
            }
        }

        #endregion

        #region Elements Name Handling

        private void GenerateElementsNames()
        {
            GenerateElementNames(GetAllElements().OfType<PartSurface>());
            GenerateElementNames(GetAllElements().OfType<SurfaceComponent>());
            GenerateElementNames(GetAllElements().OfType<ModelMesh>());
            GenerateElementNames(GetAllElements().OfType<PartConnection>());
            GenerateElementNames(GetAllElements().OfType<PartCollision>());
        }

        private void GenerateElementsNames(Type elementType, IEnumerable<PartElement> elements)
        {
            var allElements = GetAllElements().Where(x => x.GetType().IsSubclassOf(elementType));

            int elementIndex = allElements.Count(x => !string.IsNullOrEmpty(x.Name));

            foreach (var element in elements)
            {
                string elementID = element.Name;

                while (string.IsNullOrEmpty(elementID) ||
                            allElements.Any(x => x.Name == elementID && x != element))
                {
                    elementID = GenerateElementName(elementType, elementIndex++);
                    if (elementID == null)
                        break;
                }

                element.Name = elementID;
            }
        }

        private void GenerateElementNames<T>(IEnumerable<T> allElements) where T : PartElement
        {
            var elementType = typeof(T);

            int elementIndex = allElements.Count(x => !string.IsNullOrEmpty(x.Name));

            foreach (var element in allElements)
            {
                string elementName = element.Name;

                while (string.IsNullOrEmpty(elementName) ||
                            allElements.Any(x => x.Name == elementName && x != element))
                {
                    elementName = GenerateElementName(elementType, elementIndex++);
                    if (elementName == null)
                        break;
                }

                element.Name = elementName;
            }
        }

        private string GenerateElementName(Type elementType, int elementIndex)
        {
            if (elementType == typeof(PartSurface))
                return $"Surface{elementIndex}";
            else if (elementType == typeof(SurfaceComponent))
                return $"Component{elementIndex++}";
            else if (elementType == typeof(PartConnection))
                return $"Connection{elementIndex++}";
            else if (elementType == typeof(PartCollision))
                return $"Collision{elementIndex++}";
            else if (elementType == typeof(ModelMesh))
                return $"Mesh{elementIndex++}";
            else if (elementType == typeof(StudReference))
                return $"Mesh{elementIndex++}";
            return null;
        }

        #endregion

        #region Methods

        private void LinkStudReferences()
        {
            foreach (var surf in Surfaces)
            {
                foreach (var comp in surf.Components.OfType<PartCullingModel>())
                {
                    PartConnection linkedConnection = null;

                    if (comp.ConnectionIndex != -1 &&
                        comp.ConnectionIndex < Connections.Count &&
                        Connections[comp.ConnectionIndex].ConnectorType == ConnectorType.Custom2DField)
                    {
                        linkedConnection = Connections[comp.ConnectionIndex];
                    }

                    if (linkedConnection == null && !string.IsNullOrEmpty(comp.ConnectionID))
                    {
                        linkedConnection = Connections
                            .FirstOrDefault(x => x.ID == comp.ConnectionID);
                    }

                    comp.ConnectionID = linkedConnection?.ID;
                    comp.ConnectionIndex = linkedConnection != null ? Connections.IndexOf(linkedConnection) : -1;

                    //foreach (var stud in comp.GetStudReferences())
                    //{
                        
                    //    if (stud.Connection == null)
                    //    {
                    //        if (stud.ConnectorIndex != -1)
                    //        {
                    //            if (stud.ConnectorIndex < Connections.Count &&
                    //                Connections[stud.ConnectorIndex].ConnectorType == ConnectorType.Custom2DField)
                    //            {
                    //                stud.Connection = (PartConnection/*<Custom2DFieldConnector>*/)Connections[stud.ConnectorIndex];
                    //            }
                    //            else
                    //            {
                    //                string refID = Utilities.StringUtils.GenerateUUID($"{PartID}_{stud.ConnectorIndex}", 8);
                    //                stud.Connection = Connections.OfType<PartConnection/*<Custom2DFieldConnector>*/>()
                    //                    .FirstOrDefault(x => x.RefID == refID);
                    //            }
                    //        }
                    //        else if (!string.IsNullOrEmpty(stud.ConnectionID))
                    //        {
                    //            stud.Connection = Connections.OfType<PartConnection/*<Custom2DFieldConnector>*/>()
                    //                .FirstOrDefault(x => x.RefID == stud.ConnectionID);
                    //        }
                    //    }

                    //    if (stud.Connection != null)
                    //    {
                            
                    //        stud.ConnectionID = stud.Connection.RefID;
                    //        stud.ConnectorIndex = Connections.IndexOf(stud.Connection);
                    //    }
                    //}
                
                
                }
            }
        }

        private void GenerateMeshIDs(bool fromLDD)
        {
            //int maxCompCount = Surfaces.Max(s => s.Components.Count);
            var allMeshes = Surfaces.SelectMany(s => s.Components.SelectMany(c => c.GetAllMeshes()));

            foreach (var surface in Surfaces)
            {
                int componentIndex = 0;

                foreach (var component in surface.Components)
                {
                    int meshIndex = 0;

                    foreach (var compMesh in component.GetAllMeshes())
                    {
                        if (!string.IsNullOrEmpty(compMesh.ID))
                            continue;

                        if (fromLDD)
                        {
                            string uniqueStr = $"{PartID}_{surface.SurfaceID}_{componentIndex}_{meshIndex++}";
                            string meshID = StringUtils.GenerateUUID(uniqueStr, 8);

                            while (allMeshes.Any(x => x.ID == meshID && x != compMesh))
                            {
                                uniqueStr = $"{PartID}_{surface.SurfaceID}_{componentIndex}_{meshIndex++}";
                                meshID = StringUtils.GenerateUUID(uniqueStr, 8);
                            }

                            compMesh.ID = meshID;
                        }
                        else
                            compMesh.ID = StringUtils.GenerateUID(8);

                        //compMesh.FileName = $"{compMesh.RefID}.geom";
                    }

                    componentIndex++;
                }
            }
        }

        private void GenerateMeshesNames()
        {
            foreach (var surface in Surfaces)
            {
                foreach (var mesh in surface.GetAllMeshes())
                {

                    if (string.IsNullOrEmpty(mesh.FileName) || !mesh.FileName.Contains(mesh.ID))
                    {
                        
                        //mesh.FileName = $"Meshes\\Surface_{surface.SurfaceID}\\{mesh.RefID}.geom";
                        mesh.FileName = $"Meshes\\{mesh.ID}.geom";

                        if (mesh.Surface != null)
                            mesh.FileName = $"Meshes\\Surface{mesh.Surface.SurfaceID}_{mesh.ID}.geom";

                        if (!string.IsNullOrEmpty(ProjectWorkingDir))
                            mesh.WorkingFilePath = Path.Combine(ProjectWorkingDir, mesh.FileName);
                    }
                }
            }
        }

        public MeshGeometry ReadModelMesh(string meshFilename)
        {
            if (string.IsNullOrEmpty(ProjectWorkingDir))
                return null;

            string meshPath = Path.Combine(ProjectWorkingDir, meshFilename);
            try
            {
                if (File.Exists(meshPath))
                    return MeshGeometry.FromFile(meshPath);
            }
            catch { }

            return null;
        }

        #endregion

        #region Change tracking 

        internal void OnElementCollectionChanged(CollectionChangedEventArgs ccea)
        {
            if (ccea.Action == System.ComponentModel.CollectionChangeAction.Add)
            {
                if (!IsLoading)
                {
                    var elementHierarchy = ccea.AddedElements
                        .Concat(ccea.AddedElements.SelectMany(x => x.GetChildsHierarchy()));

                    GenerateElementsIDs(elementHierarchy);

                    GenerateElementsNames(ccea.ElementType, ccea.AddedElements);

                    //var allChilds = ccea.AddedElements.SelectMany(x => x.GetChildsHierarchy());
                    //var childMeshes = allChilds.OfType<ModelMesh>();
                    //if (childMeshes.Any())
                    //    GenerateElementsIDs(typeof(ModelMesh), childMeshes);
                }
            }

            if (!IsLoading)
                ElementCollectionChanged?.Invoke(this, ccea);
        }

        

        internal void OnElementPropertyChanged(PropertyChangedEventArgs pcea)
        {
            if (!IsLoading)
                ElementPropertyChanged?.Invoke(this, pcea);
        }

        #endregion


        #region LDD File Generation

        public LDD.Parts.PartWrapper GenerateLddPart()
        {
            return null;
        }

        #endregion
    }
}
