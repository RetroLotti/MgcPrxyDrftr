using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.models
{
    public class SetRoot
    {
        public Meta Meta { get; set; }
        public Set Data { get; set; }
    }

    public class Set
    {
        public long BaseSetSize { get; set; }
        public string Block { get; set; }
        public Booster Booster { get; set; }
        public List<Card> Cards { get; set; }
        public string Code { get; set; }
        public bool IsFoilOnly { get; set; }
        public bool IsNonFoilOnly { get; set; }
        public bool IsPartialPreview { get; set; }
        public bool IsOnlineOnly { get; set; }
        public string KeyruneCode { get; set; }
        public string Name { get; set; }
        public DateTimeOffset ReleaseDate { get; set; }
        public List<SealedProduct> SealedProduct { get; set; }
        public long TcgplayerGroupId { get; set; }
        public List<Card> Tokens { get; set; }
        public long TotalSetSize { get; set; }
        public Translations Translations { get; set; }
        public string Type { get; set; }
        public long McmIdExtras { get; set; }
        public string MtgoCode { get; set; }
        public long McmId { get; set; }
        public string McmName { get; set; }
    }

    public class ForeignData
    {
        //public Language Language { get; set; }
        public string Language { get; set; }
        public int MultiverseId { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public class Booster
    {
        public ArenaBooster Arena { get; set; }
        public DefaultBooster Default { get; set; }
        public ArenaBooster Set { get; set; }
        public ArenaBooster Collector { get; set; }
        public ArenaBooster Jumpstart { get; set; }
    }

    public class ArenaBooster : DefaultBooster
    {
        public string Name { get; set; }
    }

    public class DefaultBooster
    {
        public List<BoosterElement> Boosters { get; set; }
        public long BoostersTotalWeight { get; set; }
        public Sheets Sheets { get; set; }
    }

    public class BoosterElement
    {
        public Contents Contents { get; set; }
        public long Weight { get; set; }
    }

    public class Sheet
    {
        public bool BalanceColors { get; set; }
        public Dictionary<string, long> Cards { get; set; }
        public bool Foil { get; set; }
        public long TotalWeight { get; set; }

        public static explicit operator Sheet(PropertyInfo v)
        {
            throw new NotImplementedException();
        }
    }

    public class LeadershipSkills
    {
        public bool Brawl { get; set; }
        public bool Commander { get; set; }
        public bool Oathbreaker { get; set; }
    }

    public class Ruling
    {
        public DateTimeOffset Date { get; set; }
        public string Text { get; set; }
    }

    public class CardIdentifiers
    {
        public long CardKingdomFoilId { get; set; }
        public long CardKingdomId { get; set; }
        public long? McmId { get; set; }
        public long McmMetaId { get; set; }
        public long? MtgArenaId { get; set; }
        public Guid MtgjsonV4Id { get; set; }
        public long? MtgoId { get; set; }
        public long MultiverseId { get; set; }
        public Guid ScryfallId { get; set; }
        public Guid ScryfallIllustrationId { get; set; }
        public Guid ScryfallOracleId { get; set; }
        public long? TcgplayerProductId { get; set; }
    }

    public class Legalities
    {
        public Legality Commander { get; set; }
        public Legality Duel { get; set; }
        public Legality Legacy { get; set; }
        public Legality Oldschool { get; set; }
        public Legality? Penny { get; set; }
        public Legality? Premodern { get; set; }
        public Legality Vintage { get; set; }
        public Legality? Pauper { get; set; }
        public Legality? Paupercommander { get; set; }
        public Legality? Modern { get; set; }
        public Legality? Pioneer { get; set; }
        public Legality? Brawl { get; set; }
        public Legality? Future { get; set; }
        public Legality? Gladiator { get; set; }
        public Legality? Historic { get; set; }
        public Legality? Historicbrawl { get; set; }
        public Legality? Standard { get; set; }
    }

    public partial class PurchaseUrls
    {
        public Uri CardKingdom { get; set; }
        public Uri CardKingdomFoil { get; set; }
        public Uri CardMarket { get; set; }
        public Uri Tcgplayer { get; set; }
    }

    public class SealedProduct
    {
        public SealedProductIdentifiers Identifiers { get; set; }
        public string Name { get; set; }
        public SealedProductPurchaseUrls PurchaseUrls { get; set; }
        public object ReleaseDate { get; set; }
        public Guid Uuid { get; set; }
    }

    public class SealedProductIdentifiers
    {
        public long TcgplayerProductId { get; set; }
    }

    public class SealedProductPurchaseUrls
    {
        public Uri Tcgplayer { get; set; }
    }

    public class Translations
    {
        public object ChineseSimplified { get; set; }
        public object ChineseTraditional { get; set; }
        public string French { get; set; }
        public object German { get; set; }
        public object Italian { get; set; }
        public object Japanese { get; set; }
        public object Korean { get; set; }
        public object PortugueseBrazil { get; set; }
        public object Russian { get; set; }
        public object Spanish { get; set; }
    }

    #region Enums
    public enum Availability { Arena, Dreamcast, Mtgo, Paper, Shandalar };

    public enum BorderColor { Black, White, Borderless, Silver, Gold };

    public enum ColorIdentity { B, G, R, U, W };

    public enum ColorIdicator { B, G, R, U, W };

    public enum Color { B, G, R, U, W };

    public enum Finish { Foil, Nonfoil, Etched, Glossy, Signed };

    public enum FrameEffect { Colorshifted, Companion, Compasslanddfc, storyspotlight, Devoid, Draft, Etched, Extendedart, Fandfc, Fullart, Inverted, Legendary, Lesson, Miracle, Mooneldrazidfc, Nyxborn, Nyxtouched, Originpwdfc, Showcase, Snow, Sunmoondfc, Textless, Tombstone, Waxingandwaningmoondfc };

    //public enum FrameVersion { x1993, x1997, x2003, x2015, Future };

    public enum Layout { Adventure, Aftermath, Art_Series, Augment, Class, Double_Faced_Token, Emblem, Flip, Host, Leveler, Meld, Modal_Dfc, Normal, Planar, Reversible_Card, Saga, Scheme, Split, Token, Transform, Vanguard };

    // TODO
    //"arenaleague", "boosterfun", "boxtopper", "brawldeck", "bundle", "buyabox", "convention", "datestamped", "draculaseries", "draftweekend", "duels", "event", "fnm", "gameday", "gateway", "giftbox", "godzillaseries", "instore", "intropack", "jpwalker", "judgegift", "league", "mediainsert", "openhouse", "planeswalkerstamped", "playerrewards", "playpromo", "premiereshop", "prerelease", "promopack", "release", "setpromo", "stamped", "themepack", "tourney", "wizardsplaynetwork"
    //public enum PromoType { Boosterfun, Bundle, Buyabox, Promopack, Themepack, Intropack, Setpromo, brawldeck, godzillaseries, boxtopper, release };

    public enum Rarity { Bonus, Common, Mythic, Rare, Special, Uncommon };

    public enum Side { A, B, C, D, E };

    public enum SecurityStamp { Acorn, Arena, Oval, Triangle };

    // TODO
    // "Abian", "Adventure", "Advisor", "Aetherborn", "Ajani", "Alara", "Alicorn", "Alien", "Ally", "Aminatou", "Angel", "Angrath", "Antelope", "Ape", "Arcane", "Archer", "Archon", "Arkhos", "Arlinn", "Army", "Art", "Artificer", "Ashiok", "Assassin", "Assembly-Worker", "Atog", "Aura", "Aurochs", "Autobot", "Avatar", "Azgol", "Azra", "B.O.B.", "Baddest,", "Badger", "Bahamut", "Barbarian", "Bard", "Basilisk", "Basri", "Bat", "Bear", "Beast", "Beaver", "Beeble", "Beholder", "Belenon", "Berserker", "Biggest,", "Bird", "Blood", "Boar", "Bolas", "Bolas’s Meditation Realm", "Brainiac", "Bringer", "Brushwagg", "Bureaucrat", "Calix", "Camel", "Carrier", "Cartouche", "Cat", "Centaur", "Cephalid", "Chameleon", "Chandra", "Chicken", "Child", "Chimera", "Citizen", "Clamfolk", "Class", "Cleric", "Cloud", "Clown", "Clue", "Cockatrice", "Construct", "Contraption", "Cow", "Coward", "Crab", "Crocodile", "Curse", "Cyborg", "Cyclops", "Dack", "Dakkon", "Daretti", "Dauthi", "Davriel", "Deer", "Demigod", "Demon", "Desert", "Designer", "Devil", "Dihada", "Dinosaur", "Djinn", "Dog", "Dominaria", "Domri", "Donkey", "Dovin", "Dragon", "Drake", "Dreadnought", "Drone", "Druid", "Dryad", "Duck", "Dungeon", "Dwarf", "Efreet", "Egg", "Elder", "Eldrazi", "Elemental", "Elemental?", "Elephant", "Elf", "Elk", "Ellywick", "Elspeth", "Elves", "Equilor", "Equipment", "Ergamon", "Estrid", "Etiquette", "Eye", "Fabacin", "Faerie", "Ferret", "Fire", "Fish", "Flagbearer", "Food", "Forest", "Fortification", "Fox", "Fractal", "Freyalise", "Frog", "Fungus", "Gamer", "Gargoyle", "Garruk", "Gate", "Germ", "Giant", "Gideon", "Gnoll", "Gnome", "Goat", "Goblin", "God", "Gold", "Golem", "Gorgon", "Grandchild", "Gremlin", "Griffin", "Grist", "Guest", "Gus", "Hag", "Halfling", "Hamster", "Harpy", "Hatificer", "Head", "Hellion", "Hero", "Hippo", "Hippogriff", "Homarid", "Homunculus", "Horror", "Horse", "Huatli", "Human", "Hydra", "Hyena", "Igpay", "Illusion", "Imp", "Incarnation", "Inkling", "Innistrad", "Insect", "Inzerva", "Iquatana", "Ir", "Island", "Jace", "Jackal", "Jaya", "Jellyfish", "Jeska", "Juggernaut", "Kaito", "Kaldheim", "Kamigawa", "Kangaroo", "Karn", "Karsus", "Kasmina", "Kavu", "Kaya", "Kephalai", "Key", "Killbot", "Kinshala", "Kiora", "Kirin", "Kithkin", "Knight", "Kobold", "Kolbahan", "Kor", "Koth", "Kraken", "Kyneth", "Lady", "Lair", "Lamia", "Lammasu", "Leech", "Legend", "Lesson", "Leviathan", "Lhurgoyf", "Licid", "Liliana", "Lizard", "Lobster", "Locus", "Lolth", "Lorwyn", "Lukka", "Luvion", "Mammoth", "Manticore", "Master", "Masticore", "Mercadia", "Mercenary", "Merfolk", "Metathran", "Mime", "Mine", "Minion", "Minotaur", "Mirrodin", "Moag", "Mole", "Monger", "Mongoose", "Mongseng", "Monk", "Monkey", "Moonfolk", "Mordenkainen", "Mountain", "Mouse", "Mummy", "Muraganda", "Mutant", "Myr", "Mystic", "Naga", "Nahiri", "Narset", "Nastiest,", "Nautilus", "Nephilim", "New Phyrexia", "Nightmare", "Nightstalker", "Niko", "Ninja", "Nissa", "Nixilis", "Noble", "Noggle", "Nomad", "Nymph", "Octopus", "Ogre", "Oko", "Ooze", "Orc", "Orgg", "Otter", "Ouphe", "Ox", "Oyster", "Pangolin", "Paratrooper", "Peasant", "Pegasus", "Penguin", "Pentavite", "Pest", "Phelddagrif", "Phoenix", "Phyrexia", "Phyrexian", "Pilot", "Pirate", "Plains", "Plant", "Power-Plant", "Praetor", "Processor", "Proper", "Pyrulea", "Rabbit", "Rabiah", "Raccoon", "Ral", "Ranger", "Rat", "Rath", "Ravnica", "Rebel", "Reflection", "Regatha", "Reveler", "Rhino", "Rigger", "Robot", "Rogue", "Rowan", "Rune", "Sable", "Saga", "Saheeli", "Salamander", "Samurai", "Samut", "Saproling", "Sarkhan", "Satyr", "Scarecrow", "Scientist", "Scion", "Scorpion", "Scout", "Sculpture", "Segovia", "Serf", "Serpent", "Serra", "Serra’s Realm", "Servo", "Shade", "Shadowmoor", "Shaman", "Shandalar", "Shapeshifter", "Shard", "Shark", "Sheep", "Ship", "Shrine", "Siren", "Skeleton", "Slith", "Sliver", "Slug", "Snake", "Soldier", "Soltari", "Sorin", "Spawn", "Specter", "Spellshaper", "Sphinx", "Spider", "Spike", "Spirit", "Sponge", "Spy", "Squid", "Squirrel", "Starfish", "Surrakar", "Survivor", "Swamp", "Szat", "Tamiyo", "Teddy", "Teferi", "Tentacle", "Teyo", "Tezzeret", "Thalakos", "The", "Thopter", "Thrull", "Tibalt", "Tiefling", "Tower", "Townsfolk", "Trap", "Treasure", "Treefolk", "Trilobite", "Triskelavite", "Troll", "Turtle", "Tyvar", "Ugin", "Ulgrotha", "Unicorn", "Urza", "Urza’s", "Valla", "Vampire", "Vampyre", "Vedalken", "Vehicle", "Venser", "Viashino", "Villain", "Vivien", "Volver", "Vraska", "Vryn", "Waiter", "Wall", "Warlock", "Warrior", "Weird", "Werewolf", "Whale", "Wildfire", "Will", "Windgrace", "Wizard", "Wolf", "Wolverine", "Wombat", "Worm", "Wraith", "Wrenn", "Wrestler", "Wurm", "Xenagos", "Xerex", "Yanggu", "Yanling", "Yeti", "Zariel", "Zendikar", "Zombie", "Zubera", "and/or", "of"
    //public enum SubType { Abian };

    public enum Supertype { Basic, Host, Legendary, Ongoing, Snow, World };

    // TODO
    public enum Type { Artifact, Battle, Creature, Card, Conspiracy, Dragon, Dungeon, Eaturecray, Elemental, Elite, Emblem, Enchantment, Ever, Goblin, Hero, Instant, Jaguar, Knights, Land, Phenomenon, Plane, Planeswalker, Scariest, Scheme, See, Sorcery, Specter, Sticker, Summon, Token, Tribal, Vanguard, Wolf, Youll }

    // TODO 
    //public enum Watermark { Abzan };



    //public enum Language { ChineseSimplified, ChineseTraditional, French, German, Italian, Japanese, Korean, PortugueseBrazil, Russian, Spanish };

    //public enum CardLayout { Normal, Class, Transform };

    public enum Legality { Banned, Legal, Restricted };

    public enum BoosterType { Default, Collector, Set, Jumpstart, Arena }
    #endregion
}
