﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Commands
{
    public interface IDependencyMap
    {
        T Get<T>() where T : class;
        void Add<T>(T obj);
    }
}
