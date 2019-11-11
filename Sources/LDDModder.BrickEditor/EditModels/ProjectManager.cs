﻿using LDDModder.Modding.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.BrickEditor.EditModels
{
    public class ProjectManager
    {
        public PartProject CurrentProject { get; private set; }

        public bool IsProjectOpen => CurrentProject != null;

        private PartElement _SelectedElement;

        public PartElement SelectedElement
        {
            get => _SelectedElement;
            set
            {
                if (value != _SelectedElement)
                {
                    _SelectedElement = value;
                    SelectedElementChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler SelectedElementChanged;

        public event EventHandler ProjectClosed;

        public event EventHandler ProjectChanged;

        public event EventHandler<CollectionChangedEventArgs> ProjectElementsChanged;

        private bool PreventProjectChange;

        public ProjectManager()
        {
            
        }

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
        }

        private void DettachPartProject(PartProject project)
        {
            project.ElementCollectionChanged -= Project_ElementCollectionChanged;
        }
                private void Project_ElementCollectionChanged(object sender, CollectionChangedEventArgs e)
        {
            _SelectedElement = null;
            ProjectElementsChanged?.Invoke(this, e);
        }
    }
}