using Hamfer.Repository.data;
using Hamfer.Repository.utils;

namespace Hamfer.Repository.models;

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