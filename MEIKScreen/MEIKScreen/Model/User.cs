﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEIKScreen.Model
{
    public class User
    {
        private string name="";
        private string license="";

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        
        public string License
        {
            get { return license; }
            set { license = value; }
        }
        
    }
}
