namespace EventCalenderApi.Interfaces
{
    public interface IRepository<K, T> where T : class
    {

        

        IQueryable<T> GetQueryable();

        Task<T?> GetByIdAsync(K key);


        Task<T> AddAsync(T item);

        Task<T?> UpdateAsync(K key, T item);

        Task<T?> DeleteAsync(K key);




    }
}