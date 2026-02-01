using Hamfer.Repository.Attributes;

namespace Hamfer.Repository.Entity;

/// <summary>
/// موجودیت ریپازیتوری: موجودیتی است که برای تعامل لایه‌های بالاتر با ریپازیتوری کاربرد دارد
/// این موجودیت باید قابل مقایسه و قابل سنجش باشد
/// </summary>
/// <typeparam name="TEntity">جنس همان موجودیت</typeparam>
[RepositoryTable(SqlTableParam.Set_Schema, "dbo")]
public interface IRepositoryDboEntity<TEntity> : IRepositoryEntity<TEntity>
    where TEntity : class
{
}