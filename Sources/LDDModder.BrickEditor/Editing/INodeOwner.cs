﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.BrickEditor.Editing
{
    public interface INodeOwner
    {
        PartNodeCollection Nodes { get; }
    }
}