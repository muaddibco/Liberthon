using System;
using System.Collections.Generic;
using System.Text;
using Wist.BlockLattice.Core.DAL.Keys;
using Wist.BlockLattice.Core.DataModel;

namespace Wist.BlockLattice.Core.Interfaces
{
    public interface IDataService<T, TKey> where T : Entity where TKey : IDataKey
    {
        void Initialize();

        void Add(T item);

        T Get(TKey key);

        void Update(TKey key, T item);

        IEnumerable<T> GetAll();

        IEnumerable<T> GetAll(TKey key);
    }
}
