﻿using LDDModder.BrickEditor.Rendering;
using LDDModder.BrickEditor.Resources;
using LDDModder.Modding.Editing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.BrickEditor.ProjectHandling
{
    public class ProjectManager
    {
        private List<PartElement> _SelectedElements;
        private List<ValidationMessage> _ValidationMessages;

        private long LastValidation;
        private long LastSavedChange;

        public PartProject CurrentProject { get; private set; }

        public UndoRedoManager UndoRedoManager { get; }

        public bool IsProjectOpen => CurrentProject != null;

        public bool IsNewProject => IsProjectOpen && string.IsNullOrEmpty(CurrentProject.ProjectPath);

        public bool IsModified => LastSavedChange != UndoRedoManager.CurrentChangeID;

        public event EventHandler UndoHistoryChanged
        {
            add => UndoRedoManager.UndoHistoryChanged += value;
            remove => UndoRedoManager.UndoHistoryChanged -= value;
        }

        public PartElement SelectedElement
        {
            get => _SelectedElements.FirstOrDefault();
            set => SelectElement(value);
        }

        public IList<PartElement> SelectedElements => _SelectedElements.AsReadOnly();


        #region Events

        public event EventHandler SelectionChanged;

        public event EventHandler ProjectClosed;

        public event EventHandler ProjectChanged;

        public event EventHandler ProjectModified;

        public event EventHandler<ElementCollectionChangedEventArgs> ElementCollectionChanged;

        public event EventHandler<ElementValueChangedEventArgs> ElementPropertyChanged;

        public event EventHandler ProjectElementsChanged;

        #endregion

        private bool ElementsChanged;
        private bool PreventProjectChange;
        private bool IsProjectAttached;

        static ProjectManager()
        {
            ElementExtenderFactory.RegisterExtension(typeof(PartSurface), typeof(ModelElementExtension));
            ElementExtenderFactory.RegisterExtension(typeof(SurfaceComponent), typeof(ModelElementExtension));
            ElementExtenderFactory.RegisterExtension(typeof(FemaleStudModel), typeof(FemaleStudModelExtension));
            ElementExtenderFactory.RegisterExtension(typeof(ModelMeshReference), typeof(ModelElementExtension));

            ElementExtenderFactory.RegisterExtension(typeof(PartCollision), typeof(ModelElementExtension));
            ElementExtenderFactory.RegisterExtension(typeof(PartConnection), typeof(ModelElementExtension));
        }

        public ProjectManager()
        {
            _SelectedElements = new List<PartElement>();
            _ValidationMessages = new List<ValidationMessage>();

            UndoRedoManager = new UndoRedoManager(this);
            UndoRedoManager.BeginUndoRedo += UndoRedoManager_BeginUndoRedo;
            UndoRedoManager.EndUndoRedo += UndoRedoManager_EndUndoRedo;
            UndoRedoManager.UndoHistoryChanged += UndoRedoManager_UndoHistoryChanged;

            _ShowPartModels = true;
        }

        #region Project Loading/Closing

        public void SetCurrentProject(PartProject project)
        {
            if (CurrentProject != project)
            {
                PreventProjectChange = true;
                CloseCurrentProject();
                PreventProjectChange = false;

                CurrentProject = project;

                if (project != null)
                    AttachPartProject(project);

                ProjectChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CloseCurrentProject()
        {
            if (CurrentProject != null)
            {
                DettachPartProject(CurrentProject);

                ProjectClosed?.Invoke(this, EventArgs.Empty);

                CurrentProject = null;

                if (!PreventProjectChange)
                    ProjectChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AttachPartProject(PartProject project)
        {
            project.ElementCollectionChanged += Project_ElementCollectionChanged;
            project.ElementPropertyChanged += Project_ElementPropertyChanged;
            project.UpdateModelStatistics();
            LastValidation = -1;
            IsProjectAttached = true;
            InitializeElementExtensions();
        }

        private void DettachPartProject(PartProject project)
        {
            project.ElementCollectionChanged -= Project_ElementCollectionChanged;
            project.ElementPropertyChanged -= Project_ElementPropertyChanged;

            _SelectedElements.Clear();
            _ValidationMessages.Clear();
            LastSavedChange = 0;
            LastValidation = -1;
            IsProjectAttached = false;
        }

        #endregion

        public void SaveProject(string targetPath)
        {
            if (IsProjectOpen)
            {
                CurrentProject.Save(targetPath);
                CurrentProject.ProjectPath = targetPath;
                LastSavedChange = UndoRedoManager.CurrentChangeID;
                ProjectModified?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SaveWorkingProject()
        {
            if (IsProjectOpen)
            {
                var projectXml = CurrentProject.GenerateProjectXml();
                projectXml.Save(Path.Combine(CurrentProject.ProjectWorkingDir, PartProject.ProjectFileName));
            }
        }

        #region Project Events

        private void Project_ElementCollectionChanged(object sender, ElementCollectionChangedEventArgs e)
        {
            if (e.Action == System.ComponentModel.CollectionChangeAction.Remove)
            {
                int count = _SelectedElements.RemoveAll(x => e.RemovedElements.Contains(x));
                if (count > 0)
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (IsProjectAttached)
                    InitializeElementExtensions();
            }

            ElementCollectionChanged?.Invoke(this, e);

            if (IsExecutingUndoRedo || IsExecutingBatchChanges)
                ElementsChanged = true;
            else
                ProjectElementsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Project_ElementPropertyChanged(object sender, ElementValueChangedEventArgs e)
        {
            ElementPropertyChanged?.Invoke(this, e);
        }

        #endregion

        public string GetProjectDisplayName()
        {
            if (CurrentProject != null)
            {
                if (CurrentProject.PartID > 0 && !string.IsNullOrEmpty(CurrentProject.PartDescription))
                    return $"{CurrentProject.PartID} - {CurrentProject.PartDescription}";
                else if (CurrentProject.PartID > 0)
                    return $"{ModelLocalizations.Label_Part} {CurrentProject.PartID}";
                else if (!string.IsNullOrEmpty(CurrentProject.PartDescription))
                    return $"{CurrentProject.PartDescription}";
                else
                    return ModelLocalizations.Label_NewPartProject;
            }

            return ModelLocalizations.Label_NoActiveProject;
        }

        #region Viewport Display Handling

        private bool _ShowPartModels;
        private bool _ShowCollisions;
        private bool _ShowConnections;

        public bool ShowPartModels
        {
            get => _ShowPartModels;
            set => SetPartModelsVisibility(value);
        }

        public bool ShowCollisions
        {
            get => _ShowCollisions;
            set => SetCollisionsVisibility(value);
        }

        public bool ShowConnections
        {
            get => _ShowConnections;
            set => SetConnectionsVisibility(value);
        }

        public event EventHandler PartModelsVisibilityChanged;

        public event EventHandler CollisionsVisibilityChanged;

        public event EventHandler ConnectionsVisibilityChanged;

        private void SetPartModelsVisibility(bool visible)
        {
            if (visible != ShowPartModels)
            {
                _ShowPartModels = visible;

                if (IsProjectOpen)
                    InvalidateElementsVisibility(CurrentProject.Surfaces);

                PartModelsVisibilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetCollisionsVisibility(bool visible)
        {
            if (visible != ShowCollisions)
            {
                _ShowCollisions = visible;

                if (IsProjectOpen)
                {
                    InvalidateElementsVisibility(CurrentProject.Collisions);
                    InvalidateElementsVisibility(CurrentProject.Bones);
                }

                CollisionsVisibilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetConnectionsVisibility(bool visible)
        {
            if (visible != ShowConnections)
            {
                _ShowConnections = visible;

                if (IsProjectOpen)
                {
                    InvalidateElementsVisibility(CurrentProject.Connections);
                    InvalidateElementsVisibility(CurrentProject.Bones);
                }

                ConnectionsVisibilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void InvalidateElementsVisibility(IEnumerable<PartElement> elements)
        {
            foreach (var elem in elements)
                elem.GetExtension<ModelElementExtension>()?.FlagVisibilityDirty();
        }

        #endregion

        public void InitializeElementExtensions()
        {
            if (IsProjectOpen)
            {
                foreach (var elem in CurrentProject.GetAllElements())
                {
                    var modelExt = elem.GetExtension<ModelElementExtension>();
                    if (modelExt != null && modelExt.Manager == null)
                        modelExt.AssignManager(this);
                }
            }
        }

        #region Selection Management

        public void ClearSelection()
        {
            if (_SelectedElements.Any())
            {
                _SelectedElements.Clear();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SelectElement(PartElement element)
        {
            if (element == null)
            {
                ClearSelection();
            }
            else if (!(SelectedElement == element && _SelectedElements.Count == 1))
            {
                _SelectedElements.Clear();
                _SelectedElements.Add(element);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetSelected(PartElement element, bool selected)
        {
            bool isSelected = SelectedElements.Contains(element);
            if (selected != isSelected)
            {
                if (selected)
                    _SelectedElements.Add(element);
                else
                    _SelectedElements.Remove(element);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SelectElements(IEnumerable<PartElement> elements)
        {
            _SelectedElements.Clear();
            _SelectedElements.AddRange(elements);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsSelected(PartElement element)
        {
            return SelectedElements.Contains(element);
        }

        public bool IsContainedInSelection(PartElement element)
        {
            var allChilds = SelectedElements.SelectMany(x => x.GetChildsHierarchy(true));
            return allChilds.Contains(element);
        }

        public int GetSelectionIndex(PartElement element)
        {
            for (int i = 0; i < _SelectedElements.Count; i++)
            {
                var allChilds = _SelectedElements[i].GetChildsHierarchy(true);
                if (allChilds.Contains(element))
                    return i;
            }
            return -1;
        }

        #endregion

        #region Undo/Redo

        public bool IsExecutingUndoRedo => UndoRedoManager.ExecutingUndoRedo;

        public bool IsExecutingBatchChanges => UndoRedoManager.IsInBatch;

        public bool CanUndo => UndoRedoManager.CanUndo;

        public bool CanRedo => UndoRedoManager.CanRedo;

        public void Undo()
        {
            UndoRedoManager.Undo();
        }

        public void Redo()
        {
            UndoRedoManager.Redo();
        }

        public void StartBatchChanges()
        {
            UndoRedoManager.StartBatchChanges();
        }

        public void EndBatchChanges()
        {
            UndoRedoManager.EndBatchChanges();
            if (ElementsChanged)
            {
                ElementsChanged = false;
                ProjectElementsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UndoRedoManager_UndoHistoryChanged(object sender, EventArgs e)
        {
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }

        private void UndoRedoManager_BeginUndoRedo(object sender, EventArgs e)
        {

        }

        private void UndoRedoManager_EndUndoRedo(object sender, EventArgs e)
        {
            if (ElementsChanged)
            {
                ElementsChanged = false;
                ProjectElementsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Validation Handling

        public bool IsValidatingProject { get; private set; }

        public IList<ValidationMessage> ValidationMessages => _ValidationMessages.AsReadOnly();

        public bool IsPartValidated => IsProjectOpen && LastValidation == UndoRedoManager.CurrentChangeID;

        public bool IsPartValid => IsPartValidated &&
            !ValidationMessages.Any(x => x.Level == ValidationLevel.Error);

        public event EventHandler ValidationStarted;

        public event EventHandler ValidationFinished;

        public void ValidateProject()
        {
            if (IsProjectOpen)
            {
                IsValidatingProject = true;
                ValidationStarted?.Invoke(this, EventArgs.Empty);

                _ValidationMessages.Clear();

                try
                {
                    _ValidationMessages.AddRange(CurrentProject.ValidatePart());
                }
                catch (Exception ex)
                {
                    _ValidationMessages.Add(new ValidationMessage("PROJECT", "UNHANDLED_EXCEPTION", ValidationLevel.Error)
                    {
                        Message = ex.ToString()
                    });
                }

                IsValidatingProject = false;
                LastValidation = UndoRedoManager.CurrentChangeID;

                ValidationFinished?.Invoke(this, EventArgs.Empty);

            }
        }

        #endregion

        #region LDD File Generation Handling

        public bool IsGeneratingFiles { get; private set; }

        public event EventHandler GenerationStarted;

        public event EventHandler GenerationFinished;

        //TODO: implement destination folder parameter to allow saving somewhere without overwritting LDD files
        public void GenerateLddFiles()
        {
            if (IsProjectOpen)
            {
                IsGeneratingFiles = true;
                GenerationStarted?.Invoke(this, EventArgs.Empty);

                try
                {
                    var lddPart = CurrentProject.GenerateLddPart();
                    lddPart.ComputeEdgeOutlines();
                    var primitives = LDD.LDDEnvironment.Current.GetAppDataSubDir("db\\Primitives\\");

                    lddPart.Primitive.Save(Path.Combine(primitives, $"{lddPart.PartID}.xml"));

                    foreach (var surface in lddPart.Surfaces)
                        surface.Mesh.Save(Path.Combine(primitives, "LOD0", surface.GetFileName()));
                }
                catch (Exception ex)
                {

                }
                IsGeneratingFiles = false;
                GenerationFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}