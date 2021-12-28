using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public static class StorageExtentions
    {
        public static T Get<T>(this Hashtable table, object key)
        {
            return (T)table[key];
        }
    }
}
