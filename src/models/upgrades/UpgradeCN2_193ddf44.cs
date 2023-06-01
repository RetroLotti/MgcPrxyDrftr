namespace MgcPrxyDrftr.models { public partial class Contents { 
public long? Conspiracy { get; set; }
public long? NonconspiracyCommon { get; set; }
public long? NonconspiracyRareMythic { get; set; }
public long? NonconspiracyUncommon { get; set; }
public long? NonconspiracyFoil { get; set; }
public long? ConspiracyFoil { get; set; }
 } 
public partial class Sheets { 
public Sheet Conspiracy { get; set; }
public Sheet NonconspiracyCommon { get; set; }
public Sheet NonconspiracyRareMythic { get; set; }
public Sheet NonconspiracyUncommon { get; set; }
public Sheet NonconspiracyFoil { get; set; }
public Sheet ConspiracyFoil { get; set; }
 } }

