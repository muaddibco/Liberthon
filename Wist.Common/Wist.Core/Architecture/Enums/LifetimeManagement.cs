﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Wist.Core.Architecture.Enums
{
    public enum LifetimeManagement
    {
        Transient,
        TransientPerResolve,
        Singleton,
        ThreadSingleton
    }
}
