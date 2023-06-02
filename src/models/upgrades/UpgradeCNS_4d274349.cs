namespace MgcPrxyDrftr.models { public partial class Contents { 
public long? Draft { get; set; }
public long? NondraftCommon { get; set; }
public long? NondraftRareMythic { get; set; }
public long? NondraftUncommon { get; set; }
public long? NondraftFoil { get; set; }
public long? DraftFoil { get; set; }
 } 
public partial class Sheets { 
public Sheet Draft { get; set; }
public Sheet NondraftCommon { get; set; }
public Sheet NondraftRareMythic { get; set; }
public Sheet NondraftUncommon { get; set; }
public Sheet NondraftFoil { get; set; }
public Sheet DraftFoil { get; set; }
 } }

