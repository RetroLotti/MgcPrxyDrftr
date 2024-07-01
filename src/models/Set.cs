using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        public SetType Type { get; set; }
        public long McmIdExtras { get; set; }
        public string MtgoCode { get; set; }
        public long McmId { get; set; }
        public string McmName { get; set; }
        public string ParentCode { get; set; }
        public string TokenSetCode { get; set; }
    }

    public class ForeignData
    {
        public Language Language { get; set; }
        public int MultiverseId { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public class Booster
    {
        public DefaultBooster Arena { get; set; }
        public DefaultBooster Default { get; set; }
        public DefaultBooster Set { get; set; }
        public DefaultBooster Collector { get; set; }
        [JsonProperty(PropertyName = "collector-sample")]
        public DefaultBooster CollectorSample { get; set; }
        public DefaultBooster Jumpstart { get; set; }
        public DefaultBooster Play { get; set; }
        public DefaultBooster Draft { get; set; }
    }

    public class DefaultBooster
    {
        public List<BoosterElement> Boosters { get; set; }
        public long BoostersTotalWeight { get; set; }
        public Sheets Sheets { get; set; }
        public string Name { get; set; }
    }

    public class BoosterElement
    {
        public Contents Contents { get; set; }
        public long Weight { get; set; }
    }

    public class Sheet
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool AllowDuplicates { get; set; }
        public bool BalanceColors { get; set; }
        public Dictionary<string, long> Cards { get; set; }
        public bool Foil { get; set; }
        public bool Fixed { get; set; }
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
        public Legality Alchemy { get; set; }
        public Legality Brawl { get; set; }
        public Legality Commander { get; set; }
        public Legality Duel { get; set; }
        public Legality Explorer { get; set; }
        public Legality? Future { get; set; }
        public Legality? Gladiator { get; set; }
        public Legality? Historic { get; set; }
        public Legality? Historicbrawl { get; set; }
        public Legality Legacy { get; set; }
        public Legality? Modern { get; set; }
        public Legality? Oathbreaker { get; set; }
        public Legality Oldschool { get; set; }
        public Legality? Pauper { get; set; }
        public Legality? Paupercommander { get; set; }
        public Legality? Penny { get; set; }
        public Legality? Pioneer { get; set; }
        public Legality? Predh { get; set; }
        public Legality? Premodern { get; set; }
        public Legality? Standard { get; set; }
        public Legality? StandardBrawl { get; set; }
        public Legality? Timeless { get; set; }
        public Legality Vintage { get; set; }
    }

    public class PurchaseUrls
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
    public enum Availability { Arena, Dreamcast, Mtgo, Paper, Shandalar }

    public enum BorderColor { Black, White, Borderless, Silver, Gold }

    public enum ColorIdentity { B, G, R, U, W }

    public enum ColorIdicator { B, G, R, U, W }

    public enum Color { B, G, R, U, W }

    public enum Finish { Foil, Nonfoil, Etched, Glossy, Signed }

    public enum FrameEffect { Colorshifted, Companion, Compasslanddfc, Borderless, Spree, Upsidedowndfc, Convertdfc, Storyspotlight, Thick, Shatteredglass, Devoid, Draft, Etched, Extendedart, Fandfc, Fullart, Inverted, Legendary, Lesson, Miracle, Mooneldrazidfc, Nyxborn, Nyxtouched, Originpwdfc, Showcase, Snow, Sunmoondfc, Textless, Tombstone, Waxingandwaningmoondfc }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FrameVersion
    {
        [System.Runtime.Serialization.EnumMember(Value = "1993")]
        Year1993,
        [System.Runtime.Serialization.EnumMember(Value = "1997")]
        Year1997,
        [System.Runtime.Serialization.EnumMember(Value = "2003")]
        Year2003,
        [System.Runtime.Serialization.EnumMember(Value = "2015")]
        Year2015, 
        Future
    };

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Layout { Adventure, Aftermath, [System.Runtime.Serialization.EnumMember(Value = "art_series")] ArtSeries, Augment, Case, Class, [System.Runtime.Serialization.EnumMember(Value = "double_faced_token")] DoubleFacedToken, Mutate, Emblem, Flip, Host, Leveler, Meld, [System.Runtime.Serialization.EnumMember(Value = "modal_dfc")] ModalDfc, Normal, Planar, [System.Runtime.Serialization.EnumMember(Value = "reversible_card")] ReversibleCard, Saga, Scheme, Split, Token, Transform, Vanguard, Prototype }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PromoType 
    { 
        Alchemy, ArenaLeague, BoosterFun, Boxtopper, BrawlDeck, BringAFriend, Bundle, Buyabox, CommanderParty, Concept, ConfettiFoil, Convention, DateStamped, Dossier, DoubleRainbow, DraculaSeries, DraftWeekend, Duels, Embossed, Event,
        [System.Runtime.Serialization.EnumMember(Value = "fnm")]
        FridayNightMagic, 
        Galaxyfoil, Gameday, Giftbox, Gilded, Glossy, Godzillaseries, Halofoil, Instore, Intropack, Invisibleink, Jpwalker, Judgegift, League, Magnified, Mediainsert, Moonlitland, Neonink, Oilslick, Openhouse, Planeswalkerstamped, Plastic, Playerrewards, Playpromo, Portrait, Poster, Premiereshop, Prerelease, Promopack, Rainbowfoil, Raisedfoil, Ravnicacity, Rebalanced, Release, Ripplefoil, Schinesealtart, Scroll, Serialized, Setextension, Setpromo, Silverfoil, Stamped, Starterdeck, Stepandcompleat, Storechampionship, Surgefoil, Textured, Themepack, Thick, Tourney, Vault, Wizardsplaynetwork };

    public enum Rarity { Bonus, Common, Mythic, Rare, Special, Uncommon }

    public enum Side { A, B, C, D, E }

    public enum SecurityStamp { Acorn, Arena, Circle, Heart, Oval, Triangle }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SubType
    {
        Abian, Adventure, Advisor, Aetherborn, Ajani, Alara,
        [System.Runtime.Serialization.EnumMember(Value = "Alfava Metraxis")]
        AlfavaMetraxis,
        Alicorn, Alien, Ally, Aminatou, Amonkhet,
        [System.Runtime.Serialization.EnumMember(Value = "Androzani Minor")]
        AndrozaniMinor,
        Angel, Angrath, Antausia, Antelope, Apalapucia, Ape, Arcane, Arcavios, Archer, Archon, Arkhos, Arlinn, Armadillo,
        Army, Art, Artificer, Ashiok, Assassin,
        [System.Runtime.Serialization.EnumMember(Value = "Assembly-Worker")]
        AssemblyWorker,
        Astartes, Atog, Attraction, Aura, Aurochs, Autobot, Automaton, Avatar, Azgol, Azra,
        [System.Runtime.Serialization.EnumMember(Value = "B.O.B.")]
        Bob,
        Background,
        [System.Runtime.Serialization.EnumMember(Value = "Baddest,")]
        BaddestComma,
        Badger, Bahamut, Balloon, Barbarian, Bard, Basilisk, Basri, Bat, Bear, Beast, Beaver, Beeble, Beholder, Belenon, Berserker,
        [System.Runtime.Serialization.EnumMember(Value = "Biggest,")]
        BiggestComma,
        Bird, Blood, Boar, Bobblehead, Bolas,
        [System.Runtime.Serialization.EnumMember(Value = "Bolas's Meditation Realm")]
        BolassMeditationRealm,
        Brainiac, 
        Bringer, 
        Brushwagg, 
        Bureaucrat, 
        Byode,
        [System.Runtime.Serialization.EnumMember(Value = "C'tan")]
        Ctan,
        Calix,
        Camel,
        Capenna,
        Capybara,
        Carrier,
        Cartouche,
        Case,
        Cat,
        Cave,
        Centaur,
        Cephalid,
        Chameleon,
        Chandra,
        Chicken,
        Child,
        Chimera,
        Chorus,
        Citizen,
        Clamfolk,
        Class,
        Cleric,
        Cloud,
        Clown,
        Clue,
        Cockatrice,
        Comet,
        Construct,
        Contraption,
        Cow,
        Coward,
        Coyote,
        Crab,
        Cridhe,
        Crocodile,
        Curse,
        Custodes,
        Cyberman,
        Cyborg,
        Cyclops,
        Dack,
        Dakkon,
        Dalek,
        Daretti,
        Darillium,
        Dauthi,
        Davriel,
        Deb,
        Deer,
        Demigod,
        Demon,
        Desert,
        Designer,
        Detective,
        Devil,
        Dihada,
        Dinosaur,
        Djinn,
        Doctor,
        Dog,
        Dominaria,
        Domri,
        Donkey,
        Dovin,
        Dragon,
        Drake,
        Dreadnought,
        Drone,
        Druid,
        Dryad,
        Duck,
        Dungeon,
        Dwarf,
        Earth,
        Echoir,
        Efreet,
        Egg,
        Elder,
        Eldraine,
        Eldrazi,
        Elemental,
        [System.Runtime.Serialization.EnumMember(Value = "Elemental?")]
        ElementalQuestionmark,
        Elephant,
        Elf,
        Elk,
        Ellywick,
        Elminster,
        Elspeth,
        Elves,
        Employee,
        Equilor,
        Equipment,
        Ergamon,
        Ersta,
        Estrid,
        Etiquette,
        Eye,
        Fabacin,
        Faerie,
        Ferret,
        Fiora,
        Fire,
        Fish,
        Flagbearer,
        Food,
        Forest,
        Fortification,
        Fox,
        Fractal,
        Freyalise,
        Frog,
        Fungus,
        Gallifrey,
        Gamer,
        Gargantikar,
        Gargoyle,
        Garruk,
        Gate,
        Germ,
        Giant,
        Gideon,
        Gith,
        Gnoll,
        Gnome,
        Goat,
        Gobakhan,
        Goblin,
        God,
        Gold,
        Golem,
        Gorgon,
        Grandchild,
        Graveborn,
        Gremlin,
        Griffin,
        Grist,
        Guest,
        Guff,
        Gus,
        Hag,
        Halfling,
        Hamster,
        Harpy,
        Hatificer,
        Head,
        Hellion,
        Hero,
        Hippo,
        Hippogriff,
        Homarid,
        Homunculus,
        Horror,
        Horse,
        [System.Runtime.Serialization.EnumMember(Value = "HorseheadNebula")]
        HorseheadNebula,
        Huatli,
        Human,
        Hydra,
        Hyena,
        Igpay,
        Ikoria,
        Illusion,
        Imp,
        Incarnation,
        Incubator,
        Inkling,
        Innistrad,
        Inquisitor,
        Insect,
        Inzerva,
        Iquatana,
        Ir,
        Island,
        Ixalan,
        Jace,
        Jackal,
        Jared,
        Jaya,
        Jellyfish,
        Jeska,
        Juggernaut,
        Junk,
        Kaito,
        Kaladesh,
        Kaldheim,
        Kamigawa,
        Kandoka,
        Kangaroo,
        Karn,
        Karsus,
        Kasmina,
        Kavu,
        Kaya,
        Kephalai,
        Key,
        Killbot,
        Kinshala,
        Kiora,
        Kirin,
        Kithkin,
        Knight,
        Kobold,
        Kolbahan,
        Kor,
        Koth,
        Kraken,
        Kylem,
        Kyneth,
        Lady,
        Lair,
        Lamia,
        Lammasu,
        Leech,
        Lesson,
        Leviathan,
        Lhurgoyf,
        Licid,
        Liliana,
        Lizard,
        Lobster,
        Locus,
        Lolth,
        Lorwyn,
        Lukka,
        Luvion,
        Mammoth,
        Manticore,
        Map,
        Mars,
        Master,
        Masticore,
        Mercadia,
        Mercenary,
        Merfolk,
        Metathran,
        Mime,
        Mine,
        Minion,
        Minotaur,
        Minsc,
        Mirrodin,
        Mite,
        Moag,
        Mole,
        Monger,
        Mongoose,
        Mongseng,
        Monk,
        Monkey,
        Moon,
        Moonfolk,
        Mordenkainen,
        Mount,
        Mountain,
        Mouse,
        Mummy,
        Muraganda,
        Mutant,
        Myr,
        Mystic,
        Naga,
        Nahiri,
        Narset,
        [System.Runtime.Serialization.EnumMember(Value = "Nastiest,")]
        NastiestComma,
        Nautilus,
        Necron,
        Necros,
        Nephilim,
        [System.Runtime.Serialization.EnumMember(Value = "New Earth")]
        NewEarth,
        [System.Runtime.Serialization.EnumMember(Value = "New Phyrexia")]
        NewPhyrexia,
        Nightmare,
        Nightstalker,
        Niko,
        Ninja,
        Nissa,
        Nixilis,
        Noble,
        Noggle,
        Nomad,
        Nymph,
        Octopus,
        Ogre,
        Oko,
        Ooze,
        Orc,
        Orgg,
        Otter,
        Ouphe,
        [System.Runtime.Serialization.EnumMember(Value = "Outside Mutter's Spiral")]
        OutsideMuttersSpiral, 
        Ox,
        Oyster,
        Pangolin,
        Paratrooper, Peasant, Pegasus, Penguin, Pentavite, Performer, Pest, Phelddagrif, Phoenix, Phyrexia, Phyrexian, Pilot, Pirate, Plains, Plant,
        Pony, Porcupine, Possum,
        [System.Runtime.Serialization.EnumMember(Value = "Power-Plant")]
        PowerPlant,
        Powerstone, Praetor, Primarch, Processor, Proper, Pyrulea, Quintorius, Rabbit, Rabiah, Raccoon,
        Ral, Ranger, Rat, Rath, Ravnica, Rebel, Reflection, Regatha, Reveler, Rhino, Rigger, Robot, Rogue, Role, Rowan, Rune, Sable, Saga,
        Saheeli, Salamander, Samurai, Samut, Sand, Saproling, Sarkhan, Satyr, Scarecrow, Scientist, Scion, Scorpion, Scout, Sculpture, Segovia,
        Serf, Serpent, Serra,
        [System.Runtime.Serialization.EnumMember(Value = "Serra’s Realm")]
        SerrasRealm,
        Servo, Shade, Shadowmoor, Shaman, Shandalar, Shapeshifter, Shard, Shark, Sheep, Shenmeng, Ship, Shrine,
        Siege, Siren, Sivitri, Skaro, Skeleton, Slith, Sliver, Sloth, Slug, Snail, Snake, Soldier, Soltari, Sorin, Spacecraft, Spawn, Specter,
        Spellshaper, Sphere, Sphinx, Spider, Spike, Spirit, Sponge, Spy, Squid, Squirrel, Starfish, Surrakar, Survivor, Svega, Swamp, Synth, Szat,
        Tamiyo, Tarkir, Tasha, Teddy, Teferi, Tentacle, Teyo, Tezzeret, Thalakos, The,
        [System.Runtime.Serialization.EnumMember(Value = "The Abyss")]
        TheAbyss,
        [System.Runtime.Serialization.EnumMember(Value = "TheDalekAsylum")]
        TheDalekAsylum,
        [System.Runtime.Serialization.EnumMember(Value = "The Library")]
        TheLibrary,
        Theros, Thopter, Thrull, Tibalt, Tiefling, Time,
        [System.Runtime.Serialization.EnumMember(Value = "Time Lord")]
        TimeLord,
        Tower, Townsfolk, Trap, Treasure, Treefolk, Trenzalore,
        Trilobite,
        Triskelavite,
        Troll,
        Turtle,
        Tyranid,
        Tyvar,
        Ugin,
        Ulgrotha,
        Undercity,
        Unicorn,
        [System.Runtime.Serialization.EnumMember(Value = "Unknown Planet")]
        UnknownPlanet,
        Urza,
        [System.Runtime.Serialization.EnumMember(Value = "Urza's")]
        Urzas,
        Urzan,
        Valla,
        Vampire,
        Vampyre,
        Varmint,
        Vedalken,
        Vehicle,
        Venser,
        Viashino,
        Villain,
        Vivien,
        Volver,
        Vraska,
        Vronos,
        Vryn,
        Waiter,
        Wall,
        Walrus,
        Wanderer,
        Warlock,
        Warrior,
        Weird,
        Werewolf,
        Whale,
        Wildfire,
        Will,
        Windgrace,
        Wizard,
        Wolf,
        Wolverine,
        Wombat,
        Worm,
        Wraith,
        Wrenn,
        Wrestler,
        Wurm,
        Xenagos,
        Xerex,
        Yanggu,
        Yanling,
        Yeti,
        Zariel,
        Zendikar,
        Zhalfir,
        Zombie,
        Zubera,
        [System.Runtime.Serialization.EnumMember(Value = "and/or")]
        AndOr,
        [System.Runtime.Serialization.EnumMember(Value = "of")]
        Of
    };

    public enum Supertype { Basic, Host, Legendary, Ongoing, Snow, World }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Type 
    { 
        Artifact, Battle, Stickers, Creature, Card, Conspiracy, Dragon, Dungeon, Tolkien, Universewalker, Eaturecray, Elemental, Elite, Emblem, Enchantment, Ever, Goblin, Hero, Instant, Jaguar, Kindred, Knights, Land, Phenomenon, Plane, Planeswalker, Scariest, Scheme, See, Sorcery, Specter, Sticker, Summon, Token, Tribal, Vanguard, Wolf,
        [System.Runtime.Serialization.EnumMember(Value = "You'll")]
        Youll
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SetType
    {
        Alchemy, Archenemy, Arsenal, Box, Commander, Core,
        [System.Runtime.Serialization.EnumMember(Value = "draft_innovation")]
        DraftInnovation,
        [System.Runtime.Serialization.EnumMember(Value = "duel_deck")]
        DuelDeck,
        Expansion,
        [System.Runtime.Serialization.EnumMember(Value = "from_the_vault")]
        FromTheVault,
        Funny, Masterpiece, Masters, Memorabilia, Minigame, Planechase,
        [System.Runtime.Serialization.EnumMember(Value = "premium_deck")]
        PremiumDeck,
        Promo, Spellbook, Starter, Token,
        [System.Runtime.Serialization.EnumMember(Value = "treasure_chest")]
        TreasureChest,
        Vanguard
    }

    // TODO 
    // "abzan", "agentsofsneak", "arena", "atarka", "azorius", "boros", "brokers", "cabaretti", "colorpie", "conspiracy", "corocoro", "crossbreedlabs", "cutiemark", "d&d", "dci", "dengekimaoh", "desparked", "dimir", "dromoka", "flavor", "fnm", "foretell", "goblinexplosioneers", "golgari", "grandprix", "gruul", "herospath", "izzet", "japanjunior", "jeskai", "judgeacademy", "junior", "juniorapac", "junioreurope", "kolaghan", "leagueofdastardlydoom", "lorehold", "maestros", "magicfest", "mardu", "mirran", "mps", "mtg", "mtg10", "mtg15", "nerf", "obscura", "ojutai", "orderofthewidget", "orzhov", "phyrexian", "planeswalker", "prismari", "protour", "quandrix", "rakdos", "riveteers", "scholarship", "selesnya", "set", "set (5DN)", "set (AER)", "set (AKH)", "set (ALA)", "set (ALL)", "set (APC)", "set (ARB)", "set (ARN)", "set (ATQ)", "set (AVR)", "set (BFZ)", "set (BNG)", "set (BOK)", "set (C13)", "set (C14)", "set (C15)", "set (C16)", "set (C17)", "set (CHK)", "set (CMD)", "set (CN2)", "set (CNS)", "set (CON)", "set (CSP)", "set (DGM)", "set (DIS)", "set (DKA)", "set (DOM)", "set (DRK)", "set (DST)", "set (DTK)", "set (EMN)", "set (EVE)", "set (EXO)", "set (FEM)", "set (FRF)", "set (FUT)", "set (GPT)", "set (GRN)", "set (GTC)", "set (HML)", "set (HOU)", "set (ICE)", "set (INV)", "set (ISD)", "set (JOU)", "set (JUD)", "set (KLD)", "set (KTK)", "set (LEA)", "set (LEG)", "set (LGN)", "set (LRW)", "set (M10)", "set (M11)", "set (M12)", "set (M13)", "set (M14)", "set (M15)", "set (M19)", "set (MBS)", "set (MIR)", "set (MMQ)", "set (MOR)", "set (MRD)", "set (NEM)", "set (NPH)", "set (ODY)", "set (OGW)", "set (ONS)", "set (ORI)", "set (P02)", "set (PC2)", "set (PCY)", "set (PLC)", "set (PLS)", "set (POR)", "set (PTK)", "set (RAV)", "set (RIX)", "set (RNA)", "set (ROE)", "set (RTR)", "set (S99)", "set (SCG)", "set (SHM)", "set (SOI)", "set (SOK)", "set (SOM)", "set (STH)", "set (THS)", "set (TMP)", "set (TOR)", "set (TSP)", "set (UDS)", "set (ULG)", "set (USG)", "set (VIS)", "set (WTH)", "set (WWK)", "set (XLN)", "set (ZEN)", "silumgar", "silverquill", "simic", "sultai", "tarkir", "temur", "transformers", "trumpkatsumai", "witherbloom", "wotc", "wpn"
    //public enum Watermark { Abzan };

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Language
    {
        [System.Runtime.Serialization.EnumMember(Value = "Ancient Greek")]
        AcientGreek,
        Arabic,
        [System.Runtime.Serialization.EnumMember(Value = "Chinese Simplified")]
        ChineseSimplified,
        [System.Runtime.Serialization.EnumMember(Value = "Chinese Traditional")]
        ChineseTraditional,
        English,
        French,
        German,
        Hebrew,
        Italian,
        Japanese,
        Korean,
        Latin,
        Phyrexian,
        [System.Runtime.Serialization.EnumMember(Value = "Portuguese (Brazil)")]
        PortugueseBrazil,
        Russian,
        Sanskrit,
        Spanish
    };

    public enum Legality { Banned, Legal, Restricted }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BoosterType { Default, Draft, Collector, [System.Runtime.Serialization.EnumMember(Value = "collector-sample")] CollectorSample, Set, Jumpstart, Arena, Tournament, Play, [System.Runtime.Serialization.EnumMember(Value = "box-topper")] BoxTopper }
    #endregion
}
