using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.models
{
    public class Card
    {
        public string Artist { get; set; }
        public List<Availability> Availability { get; set; }
        public BorderColor BorderColor { get; set; }
        public List<Color> ColorIdentity { get; set; }
        public List<Color> Colors { get; set; }
        public int? Count { get; set; }
        public long ConvertedManaCost { get; set; }
        public long? EdhrecRank { get; set; }
        public List<Finish> Finishes { get; set; }
        public List<ForeignData> ForeignData { get; set; }
        public List<FrameEffect> FrameEffects { get; set; }
        public string FrameVersion { get; set; }
        public bool HasFoil { get; set; }
        public bool HasNonFoil { get; set; }
        public CardIdentifiers Identifiers { get; set; }
        public List<string> Keywords { get; set; }
        public Layout Layout { get; set; }
        public Legalities Legalities { get; set; }
        public string ManaCost { get; set; }
        public long ManaValue { get; set; }
        public string Name { get; set; }
        //public long Number { get; set; }
        public string Number { get; set; }
        public string OriginalText { get; set; }
        public string OriginalType { get; set; }
        public List<string> Printings { get; set; }
        public PurchaseUrls PurchaseUrls { get; set; }
        public Rarity Rarity { get; set; }
        public List<Ruling> Rulings { get; set; }
        //public Code SetCode { get; set; }
        public string SetCode { get; set; }
        //public List<SubType> Subtypes { get; set; }
        public List<string> Subtypes { get; set; }
        public List<Supertype> Supertypes { get; set; }
        public string Text { get; set; }
        public string Toughness { get; set; }
        public string Type { get; set; }
        public List<Type> Types { get; set; }
        public Guid Uuid { get; set; }
        public List<Guid> Variations { get; set; }

        public string FlavorText { get; set; }
        public string Power { get; set; }
        public bool? IsReserved { get; set; }
        public bool? HasContentWarning { get; set; }
        public LeadershipSkills LeadershipSkills { get; set; }
        //public long? Loyalty { get; set; }
#nullable enable
        public string? Loyalty { get; set; }
#nullable disable
        public bool? IsStarter { get; set; }
        //public List<PromoType> PromoTypes { get; set; }
        public List<string> PromoTypes { get; set; }
        public bool? IsPromo { get; set; }
        public string Watermark { get; set; }
        public long? FaceConvertedManaCost { get; set; }
        public long? FaceManaValue { get; set; }
        public string FaceName { get; set; }
        public List<Guid> OtherFaceIds { get; set; }
        public Side? Side { get; set; }
        public List<Color> ColorIndicator { get; set; }
        public bool? IsStorySpotlight { get; set; }
        public bool? IsFullArt { get; set; }
        public bool? IsReprint { get; set; }
        public string AsciiName { get; set; }
    }
}
