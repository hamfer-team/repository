namespace Hamfer.Repository.Attributes;

public enum SqlColumnParam
{
  // TODO
  //Set_Collation_string,
  //Set_CharSet_string,
  //Use_Udt_???,
  //Set_ComputedColumn_???
  
  With_FixedLength,
  Set_StorageSize_int,
  With_MaxSize,

  Is_DateOnly,
  Is_Money,
  Is_SmallMoney,
  Set_FractionalSecondScale_int,
  Set_Precision_int,
  Set_Scale_int,
  
  Is_Nullable,
  Is_Not_Nullable,
  
  With_DefaultValue_string,
  With_SupprtsUnicode,
  
  Is_PrimaryKey,
  Is_Unique_With_string,
  Is_Ignored,

  With_AutomaticGeneration,
  Is_Identity_With_Increment_int,
  Is_Identity_With_Seed_int,

  Set_Name,
  Set_Description,
}
