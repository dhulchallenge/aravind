using System;

namespace Lokad.Cqrs.AtomicStorage
{
    public static class NuclearStorageExtensions
    {
        
        public static TEntity UpdateEntityEnforcingNew<TEntity>(this NuclearStorage storage, object key, Action<TEntity> update)
            where TEntity : new()
        {
            return storage.Container.GetWriter<object, TEntity>().UpdateEnforcingNew(key, update);
        }
    }

    
}