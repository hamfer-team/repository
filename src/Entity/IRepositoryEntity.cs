using Hamfer.Repository.Attributes;

namespace Hamfer.Repository.Entity;

/// <summary>
/// موجودیت ریپازیتوری: موجودیتی است که برای تعامل لایه‌های بالاتر با ریپازیتوری کاربرد دارد.
/// در اینجا همواره موجودیت‌ها دارای یک شناسه یکتا هستند.
/// </summary>
public interface IRepositoryEntity
{
  Guid id { get; set; }
}

/// <summary>
/// موجودیت ریپازیتوری: موجودیتی است که برای تعامل لایه‌های بالاتر با ریپازیتوری کاربرد دارد
/// این موجودیت باید قابل مقایسه و قابل سنجش باشد
/// </summary>
/// <typeparam name="TEntity">جنس همان موجودیت</typeparam>
public interface IRepositoryEntity<TEntity> : IRepositoryEntity, IEquatable<TEntity>, IComparable<TEntity>
    where TEntity : class
{
}