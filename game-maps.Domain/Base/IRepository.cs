using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace game_maps.Domain.Base
{
    public interface IRepository<TEntity, TIdentity> where TEntity : Entity<TIdentity>
    {
        IQueryable<TEntity> AsQueryable();
        Task<TEntity?> GetByIdAsync(int id);
        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> exp);
        Task<ICollection<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> exp);
        Task<ICollection<TEntity>> ToListAsync();
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<TEntity> AddOrUpdateAsync(Expression<Func<TEntity, bool>> exp, TEntity entity);
        Task<TEntity> AddOrUpdateAsync(TEntity entity);
        Task DeleteAsync(int id);
    }
    public interface IRepository<TEntity> where TEntity : Entity, new()
    {
        IQueryable<TEntity> AsQueryable();
        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> exp);
        Task<ICollection<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> exp);
        Task<ICollection<TEntity>> ToListAsync();
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<TEntity> AddOrUpdateAsync(Expression<Func<TEntity, bool>> exp, Action<TEntity> updateEntity);
        Task DeleteAsync(Expression<Func<TEntity, bool>> exp);
    }
}
