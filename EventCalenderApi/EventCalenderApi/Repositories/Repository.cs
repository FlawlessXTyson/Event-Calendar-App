
using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        private readonly EventCalendarDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(EventCalendarDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(K key)
        {
            return await _dbSet.FindAsync(key);
        }

        public async Task<T> AddAsync(T item)
        {
            await _dbSet.AddAsync(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<T?> UpdateAsync(K key, T item)
        {
            var existing = await _dbSet.FindAsync(key);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(item);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<T?> DeleteAsync(K key)
        {
            var item = await _dbSet.FindAsync(key);
            if (item == null)
                return null;

            _dbSet.Remove(item);
            await _context.SaveChangesAsync();
            return item;
        }
    }
}