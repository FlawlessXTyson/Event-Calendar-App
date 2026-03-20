using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        private readonly EventCalendarDbContext _context;
        private readonly DbSet<T> _dbSet;

        //constructor injection
        public Repository(EventCalendarDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        //get queryable (used for filtering, searching, pagination)
        public IQueryable<T> GetQueryable()
        {
            return _dbSet;
        }

        //get entity by primary key
        public async Task<T?> GetByIdAsync(K key)
        {
            return await _dbSet.FindAsync(key);
        }

        //add new entity
        public async Task<T> AddAsync(T item)
        {
            //add to db
            await _dbSet.AddAsync(item);

            //save changes
            await _context.SaveChangesAsync();

            return item;
        }

        //update existing entity
        public async Task<T?> UpdateAsync(K key, T item)
        {
            //fetch existing entity
            var existing = await _dbSet.FindAsync(key);

            if (existing == null)
                return null;

            //update values
            _context.Entry(existing).CurrentValues.SetValues(item);

            //save changes
            await _context.SaveChangesAsync();

            return existing;
        }

        //delete entity
        public async Task<T?> DeleteAsync(K key)
        {
            //fetch entity
            var item = await _dbSet.FindAsync(key);

            if (item == null)
                return null;

            //remove from db
            _dbSet.Remove(item);

            //save changes
            await _context.SaveChangesAsync();

            return item;
        }
    }
}