﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class MultipleAssignment : Node
    {
        public List<Assignment> LValues;
        public Node RValue;
    }
}
