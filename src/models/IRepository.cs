using Hamfer.Kernel.Errors;
using Hamfer.Repository.Entity;

namespace Hamfer.Repository.Models;

public interface IRepository<TEntity>
  where TEntity : class, IRepositoryEntity<TEntity>
{
  /// <summary>
  /// جستجو و ارائه یک گزینه از پایگاه داده
  /// </summary>
  /// <param name="entityId">شناسه‌ی یکتای موجودیت</param>
  /// <returns><typeparamref name="TEntity"/></returns>
  IRepositoryEntity<TEntity> findOne(Guid entityId);

  /// <summary>
  /// جستجو و ارائه یک گزینه از پایگاه داده
  /// </summary>
  /// <param name="clause">عبارت جستجو</param>
  /// <returns><typeparamref name="TEntity"/></returns>
  IRepositoryEntity<TEntity> findOneBy(Func<IRepositoryEntity<TEntity>, bool> clause);

  /// <summary>
  /// جستجو و ارائه فهرست نتایج جستجو از پایگاه داده
  /// </summary>
  /// <param name="config">تنظیمات جستجو، مرتب سازی و صفحه بندی نتایج</param>
  /// <returns><typeparamref name="TEntity[]"/></returns>
  ICollection<IRepositoryEntity<TEntity>> findManyBy(IRepositoryPaginationConfiguration<TEntity> config);

  /// <summary>
  /// افزودن موجودیت به پایگاه داده
  /// </summary>
  /// <param name="entity">موجودیت جدید</param>
  void insert(IRepositoryEntity<TEntity> entity);

  /// <summary>
  /// تلاش برای افزودن موجودیت به پایگاه داده
  /// </summary>
  /// <param name="entity">موجودیت</param>
  /// <returns><typeparamref name="Boolean"/></returns>
  bool tryInsert(IRepositoryEntity<TEntity> entity, out RepositoryError error);

  /// <summary>
  /// ویرایش یک موجودیت پایگاه داده
  /// </summary>
  /// <param name="entity">موجودیت</param>
  void update(IRepositoryEntity<TEntity> entity);

  /// <summary>
  /// تلاش برای ویرایش موجودیت پایگاه داده
  /// </summary>
  /// <param name="entity">موجودیت</param>
  /// <returns><typeparamref name="Boolean"/></returns>
  bool tryUpdate(IRepositoryEntity<TEntity> entity, out RepositoryError error);

  /// <summary>
  /// افزودن موجودیت به پایگاه داده یا ویرایش آن در صورت وجود
  /// </summary>
  /// <param name="entity">موجودیت</param>
  void upsert(IRepositoryEntity<TEntity> entity);

  /// <summary>
  /// تلاش برای افزودن موجودیت به پایگاه داده یا ویرایش آن موجودیت در صورت وجود
  /// </summary>
  /// <param name="entity">موجودیت</param>
  /// <returns><typeparamref name="Boolean"/></returns>
  bool tryUpsert(IRepositoryEntity<TEntity> entity, out RepositoryError error);

  /// <summary>
  /// حذف موجودیت از پایگاه داده
  /// </summary>
  /// <param name="entityId">شناسه‌ی یکتای موجودیت</param>
  void delete(Guid entityId);

  /// <summary>
  /// تلاش برای حذف موجودیت از پایگاه داده
  /// </summary>
  /// <param name="entityId">شناسه موجودیت</param>
  /// <returns><typeparamref name="Boolean"/></returns>
  bool tryDelete(Guid entityId, out RepositoryError error);

  /// <summary>
  /// اعمال تغییرات بر روی پایگاه داده
  /// </summary>
  void commit();

  /// <summary>
  /// لغو تغییرات
  /// </summary>
  void rollBack();
}