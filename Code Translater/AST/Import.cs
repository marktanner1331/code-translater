﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Import : Node
    { 
        public string Package;
        public string Component;
    }
}
