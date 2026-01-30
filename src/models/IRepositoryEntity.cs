using Hamfer.Repository.data;
using Hamfer.Repository.utils;

namespace Hamfer.Repository.models;

/// <summary>
/// موجودیت ریپازیتوری: موجودیتی است که برای تعامل لایه‌های بالاتر با ریپازیتوری کاربرد دارد
/// این موجودیت باید قابل مقایسه و قابل سنجش باشد
/// </summary>
/// <typeparam name="TEntity">جنس همان موجودیت</typeparam>
public interface IRepositoryEntity<TEntity> : IEquatable<TEntity>, IComparable<TEntity>
    where TEntity : class
{
  [RepositoryColumn(SqlColumnParam.Is_Not_Nullable)]
  Guid id { get; set; }
}